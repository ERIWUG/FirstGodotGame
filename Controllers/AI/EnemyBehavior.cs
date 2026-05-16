using Godot;
using System.Linq;
using System.Collections.Generic;



public enum AIState { Idle, Moving, Attacking, Fleeing, Hesitating, SlowApproach, Dead, Casting, MovingToPosition, Commanding }

[GlobalClass]
public partial class EnemyBehavior : Node
{
    [Export] public string FactionId = "neutral";       // Идентификатор фракции
    [Export] public float Courage = 20.0f;              // Личная храбрость
    [Export] public float FearResistance = 30.0f;       // Порог, выше которого начинается паника
    [Export] public float Speed = 100.0f;
    [Export] public float PreferredCastDistance = 300.0f;

    [Export] public float MinCastDistance = 80.0f;   // ближе этого маг не колдует
    [Export] public bool CanMelee = true;   

    [Export] public float AvoidanceRadius = 50.0f; // радиус проверки препятствий
    [Export] public float AvoidanceStrength = 200.0f; // сила отклонения
    [Export] public Vector2 StatusIconScale { get; set; } = new Vector2(0.5f, 0.5f);

    [Signal] public delegate void CastingStartedEventHandler();
    [Signal] public delegate void CastingFinishedEventHandler();

    public AIState CurrentState { get; private set; } = AIState.Idle;
    private bool _permanentRetreat = false;

    public Node2D GetCurrentTarget() => _currentTarget;

    private PackedScene _statusIconScene;

    public string SquadName { get; set; }
    private bool _isMage;

    private bool _followingOrder = false;
    
    private CharacterBody2D _body;
    private HealthComponent _health;
    private Node2D _player;
    private float _currentFear = 0f; // Накопленный страх

    private int _hitCounter = 0;        // сколько раз ударили
    private const int HitsToFlee = 3;

    private Vector2 _targetPosition;

	private float _hesitateTimer = 0f;
	private Vector2 _originalPosition;
    private SkillComponent _skill;
    private ResourceComponent _resources;
    public string DisplayName => UnitNameOverride ?? _body?.Name ?? (GetParent()?.Name ?? Name);
    
    private Node2D _currentTarget;

    private SquadCommander _mySquad;

    private BaseArchetype _archetype;
    private MovementController _movement;
    private CombatController _combat;
    private SpellController _spell;

    private Sprite2D _bodySprite;
    public string UnitNameOverride { get; set; }
    private string UnitName => UnitNameOverride ?? _body?.Name ?? (GetParent()?.Name ?? Name);

    public SquadCommander Commander { get; set; }

   public override void _Ready()
    {
        _body = (CharacterBody2D)GetParent();

        _mySquad = GetParent().GetNodeOrNull<SquadCommander>("SquadCommander");
        _archetype = _body?.GetNodeOrNull<BaseArchetype>("Archetype");
        _health = GetNode<HealthComponent>("../HealthComponent");
        _skill = GetNodeOrNull<SkillComponent>("../SkillComponent");
        _resources = GetNodeOrNull<ResourceComponent>("../ResourceComponent");
        _player = GetTree().GetFirstNodeInGroup("Player") as Node2D;
        _movement = _body?.GetNodeOrNull<MovementController>("MovementController");
        _combat = _body?.GetNodeOrNull<CombatController>("CombatController");
        _spell = _body?.GetNodeOrNull<SpellController>("SpellController");

        _statusIconScene = GD.Load<PackedScene>("res://Scenes/UIBricks/StatusIcon.tscn");
        _health.HealthDepleted += () =>
        {
            CurrentState = AIState.Dead;
            string squadInfo = !string.IsNullOrEmpty(SquadName) ? $" (Отряд {SquadName})" : "";
            
            SquadJournal.Instance?.AddEntry($"{UnitName}{squadInfo} пал в бою.");
            _body.QueueFree();
        };

        if (_health != null)
            _health.Damaged += OnDamaged;

        _bodySprite = _body?.GetNodeOrNull<Sprite2D>("Sprite2D");

        

        _isMage = _skill != null && _resources != null && _skill.GetKnownSpells().Count > 0;
    }

    private void ValidateTarget()
    {
        if (_currentTarget != null && !IsInstanceValid(_currentTarget))
            _currentTarget = null;
    }

   
   private void ShowStatusIcon(Texture2D icon, Vector2 offset = default)
    {
        if (_statusIconScene == null || _body == null) return;

        var anchor = _body.GetNodeOrNull<Marker2D>("StatusIconAnchor");
        Vector2 spawnPos = anchor != null ? anchor.Position : new Vector2(0, -40);

        var instance = _statusIconScene.Instantiate<Sprite2D>();
        instance.Texture = icon;
        instance.Position = spawnPos + offset;
        instance.Scale = StatusIconScale;   // <-- применяем настраиваемый масштаб
        _body.AddChild(instance);
    }

    public void ForceTarget(Node2D target)
    {
        _followingOrder = true;
        _currentTarget = target;
    }

    
    private void AcquireTarget()
    {

        
        _currentTarget = null;
        float closestDist = float.MaxValue;

        foreach (var node in GetTree().GetNodesInGroup("Enemies"))
        {
            if (node == _body) continue;

            // Приводим к Node2D, потому что все враги у нас CharacterBody2D
            var enemyNode = node as Node2D;
            if (enemyNode == null) continue;

            var behavior = enemyNode.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
            if (behavior == null) continue;

            if (FactionManager.AreHostile(FactionId, behavior.FactionId))
            {
                float dist = _body.GlobalPosition.DistanceTo(enemyNode.GlobalPosition);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    _currentTarget = enemyNode;
                }
            }
        }

        
    }

    public bool IsPermanentlyFleeing()
    {
        return _permanentRetreat && CurrentState == AIState.Fleeing;
    }

    public void SetPermanentRetreat()
    {
        _permanentRetreat = true;
    }

    public override void _Process(double delta)
    {
        if (GetTree().Paused) return;
        if (!IsInstanceValid(this)) return;
        ValidateTarget();

      if (_body == null || !IsInstanceValid(_body)) return;
        
    if (CurrentState == AIState.Attacking || CurrentState == AIState.SlowApproach ||
    CurrentState == AIState.Moving || CurrentState == AIState.Fleeing ||
    CurrentState == AIState.Casting)
    {
        if (!_followingOrder)
        {
            AcquireTarget();
        }
        else if (_currentTarget == null || !IsInstanceValid(_currentTarget))
        {
            // Цель приказа исчезла – сбрасываем подчинение и ищем новую цель
            _followingOrder = false;
            AcquireTarget();
            GD.Print($"[{UnitName}] Цель приказа потеряна, переключаюсь на новую.");
            // Если есть новая цель, немедленно переходим в атаку
            if (_currentTarget != null)
            {
                EnterState(AIState.Attacking);
            }
        }
    }

        // Обновляем состояние
        switch (CurrentState)
        {
            case AIState.Moving:
                _movement.MoveToward(_currentTarget);
                break;
            case AIState.Attacking:
                if (_archetype != null)
                {
                    var overriddenTarget = _archetype.OverrideTarget(_currentTarget);
                    if (overriddenTarget != _currentTarget && overriddenTarget != null)
                    {
                        _currentTarget = overriddenTarget;
                    }
                }
                AttackTick(delta);
                break;
            case AIState.Fleeing:
            if (_currentTarget == null || !IsInstanceValid(_currentTarget))
            {
                // Цель исчезла – прекращаем бежать, переходим в Idle
                _body.Velocity = Vector2.Zero;
                EnterState(AIState.Idle);
                break;
            }
            _movement.FleeFrom(_currentTarget);
            if (_currentTarget != null && IsInstanceValid(_currentTarget) && 
                _body.GlobalPosition.DistanceTo(_currentTarget.GlobalPosition) > PreferredCastDistance &&
                CanAct()&& !_permanentRetreat)
            {
                EnterState(AIState.Attacking);
            }
            break;
            case AIState.SlowApproach:
    			_movement.SlowApproach(_currentTarget);
    			break;
			case AIState.Hesitating:
    			if (IsEnemyInRange(50.0f))
                    EnterState(AIState.Fleeing); // или Moving, если хочешь просто отойти
                else
                    Hesitate();
                break;
            case AIState.Casting:
                if (_spell != null && _spell.IsCasting())
                {
                    _spell.TryCast(_currentTarget, delta);
                    if (!_spell.IsCasting()) // каст завершился
                    {
                        if (_bodySprite != null)
                            _bodySprite.Modulate = Colors.White;
                        EnterState(AIState.Attacking);
                    }
                }
                else
                {
                    // Если контроллер не кастует, выходим из состояния
                    EnterState(AIState.Attacking);
                }
                break;
            case AIState.MovingToPosition:
                // Если враг рядом, немедленно вступаем в бой, забывая о точке назначения
                AcquireTarget();
                if (_currentTarget != null && _body.GlobalPosition.DistanceTo(_currentTarget.GlobalPosition) <= 40.0f)
                {
                    EnterState(AIState.Attacking);
                    break;
                }
                _movement.MoveToPosition(_targetPosition, delta);
                break;  
            case AIState.Idle:
                // Если враг подошёл близко, контратакуем
                AcquireTarget();
                if (_currentTarget != null && _body.GlobalPosition.DistanceTo(_currentTarget.GlobalPosition) <= 40.0f)
                    EnterState(AIState.Attacking);
                else
                    _body.Velocity = Vector2.Zero;
                break;
            case AIState.Commanding:
                _body.Velocity = Vector2.Zero;
                AcquireTarget();
                // Если рядом враг – всё-таки атакуем
                if (_currentTarget != null && _body.GlobalPosition.DistanceTo(_currentTarget.GlobalPosition) <= 30.0f)
                {
                    EnterState(AIState.Attacking);
                    break;
                }
                // Если рядом нет союзников, идём к ближайшему союзнику (или к центру отряда)
                var squad = _body?.GetNodeOrNull<SquadCommander>("SquadCommander");
                if (squad != null)
                {
                    Node2D closestAlly = null;
                    float closestDist = float.MaxValue;
                    foreach (var member in squad.GetMembers()) // нужно добавить публичный метод
                    {
                         if (member == null || !IsInstanceValid(member)) continue;
                        var allyBody = member.GetParent<CharacterBody2D>();
                        if (allyBody == _body) continue;
                        float dist = _body.GlobalPosition.DistanceTo(allyBody.GlobalPosition);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestAlly = allyBody;
                        }
                    }
                    if (closestAlly != null && closestDist > 80f)
                    {
                        Vector2 dir = (closestAlly.GlobalPosition - _body.GlobalPosition).Normalized();
                        _body.Velocity = dir * Speed * 0.5f;
                    }
                }
                break;
            default:
                _body.Velocity = Vector2.Zero;
                break;
        }

        _body.MoveAndSlide();
    }

    

    // Главный метод, вызываемый командиром или внешним сигналом
    public void ExecuteOrder(OrderData order)
	{

        if (_archetype != null && _archetype.HandleOrder(order))
            return;
        _followingOrder = (order.Target != null);

        if (order.Target != null)
            _currentTarget = order.Target;
        else
            AcquireTarget();

		int weight = CalculateOrderWeight(order);
		

		if (order.Type == OrderType.Assault)
        {
            if (weight > 80) EnterState(AIState.Attacking);
            else if (weight > 40)
            {
                // Маги не умеют медленно подходить – они либо атакуют (кастуют), либо колеблются
                if (_skill != null)
                    EnterState(AIState.Attacking);
                else
                    EnterState(AIState.SlowApproach);
            }
            else if (weight > 20) EnterState(AIState.Hesitating);
            else EnterState(AIState.Fleeing);
        }
		else if (order.Type == OrderType.Retreat)
        {
            _permanentRetreat = true;
            EnterState(AIState.Fleeing);
            var squad = _body?.GetNodeOrNull<SquadCommander>("SquadCommander");
            if (squad != null)
                squad.NotifyRetreat();
        }
        else if (order.Type == OrderType.MoveTo)
{
            if (_targetPosition.DistanceTo(order.TargetPosition) > 20f || CurrentState != AIState.MovingToPosition)
            {
                _targetPosition = order.TargetPosition;
                EnterState(AIState.MovingToPosition);
            }
            return;
        }
	}

    private int CalculateOrderWeight(OrderData order)
    {
        // Базовый вес – личная храбрость
        int weight = (int)Courage;

        // Модификатор фракции (заглушка, позже загрузим из FactionData)
        weight += GetFactionModifier(order.Type);
        
        // Бонус от командира (если есть в приказе)
        weight += order.CommanderBonus;

        // Штраф от страха
        weight -= (int)_currentFear;
        
        // Штраф от низкого здоровья (чем меньше HP, тем сильнее паника)
        if (_health != null)
        {
            float hpPercent = _health.CurrentHealth / _health.MaxHealth;
            if (hpPercent < 0.3f) weight -= 40;
            else if (hpPercent < 0.6f) weight -= 15;
        }

        return weight;
    }

    private int GetFactionModifier(OrderType type)
    {
        // Временная реализация. В будущем будет загружаться из ресурса фракции.
        if (FactionId == "shadow_cult") return 25;  // Фанатики любят атаковать
        if (FactionId == "mercenary") return -10;   // Наёмники осторожничают
        return 0;
    }

    // Личные триггеры
    private void OnDamaged(float amount, float currentHealth)
    {
        _currentFear += amount * 0.5f;
        _hitCounter++;

        if (CurrentState == AIState.Casting)
        {
            // Если рядом враг — не Hesitate, а сразу в ближний бой (если маг) или в атаку
            bool enemyNear = IsEnemyInRange(40.0f);
            if (enemyNear && _skill != null) // маг -> ближний бой
                EnterState(AIState.Attacking);
            else if (enemyNear && _skill == null) // не маг -> просто атака (уже по логике)
                EnterState(AIState.Attacking);
            else
                EnterState(AIState.Hesitating); // никого рядом — колебаться
            GD.Print($"[{UnitName}] Каст прерван! Переход в {(enemyNear ? "ближний бой" : "hesitate")}");
        }
        else if (_hitCounter >= HitsToFlee && CurrentState != AIState.Fleeing)
        {
            EnterState(AIState.Fleeing);
            GD.Print($"[{UnitName}] Слишком много ударов – бежит!");
        }

        // Если страх зашкаливает, тоже бежать
        if (_currentFear > FearResistance && CurrentState != AIState.Fleeing)
            EnterState(AIState.Fleeing);
    }

    // Вспомогательный метод
    private bool IsEnemyInRange(float range)
    {
        if (_currentTarget == null || !IsInstanceValid(_currentTarget)) return false;
        return _body.GlobalPosition.DistanceTo(_currentTarget.GlobalPosition) <= range;
    }

	

    // Смена состояния
    public void EnterState(AIState newState)
    {

        if (newState != AIState.Hesitating && newState != AIState.Fleeing)
            _hitCounter = 0;
        // При выходе из каста (неважно, успешном или прерванном) сообщаем об окончании
        if (CurrentState == AIState.Casting && newState != AIState.Casting)
            EmitSignal(SignalName.CastingFinished);

        // При входе в каст сообщаем о начале
        if (newState == AIState.Casting)
            EmitSignal(SignalName.CastingStarted);

        if (newState == AIState.Hesitating)
            _originalPosition = _body.GlobalPosition;

        CurrentState = newState;
        if (_statusIconScene == null || _body == null) return;

        Texture2D iconTex = null;
        if (newState == AIState.Attacking)
            iconTex = GD.Load<Texture2D>("res://Sprites/swords.png"); // ⚔️
        else if (newState == AIState.Fleeing)
            iconTex = GD.Load<Texture2D>("res://Sprites/skull.png"); // 💀
        else if (newState == AIState.Hesitating)
            iconTex = GD.Load<Texture2D>("res://Sprites/dots.png"); // ...

        if (iconTex != null)
            ShowStatusIcon(iconTex);
    }

	private void Hesitate()
	{
		_body.Velocity = Vector2.Zero;
		_hesitateTimer += (float)GetProcessDeltaTime();
		if (_hesitateTimer >= 0.5f)
		{
			_hesitateTimer = 0f;
			float offsetX = GD.Randf() * 6 - 3; // случайное смещение от -3 до +3 пикселей
			float offsetY = GD.Randf() * 6 - 3;
			_body.GlobalPosition = _originalPosition + new Vector2(offsetX, offsetY);
		}
	}

    private void AttackTick(double delta)
    {
          if (_currentTarget == null || !IsInstanceValid(_currentTarget)) return;

        // Если есть архетип и он полностью обработал тик – выходим
        if (_archetype != null && _archetype.ProcessAttack(this, delta))
            return;

        // Стандартная логика для обычных юнитов (маги, милишники)
        float dist = _body.GlobalPosition.DistanceTo(_currentTarget.GlobalPosition);
        bool canCast = (_skill != null && _resources != null && _skill.GetKnownSpells().Count > 0);

        // ========== ВЕТКА ДЛЯ МАГОВ ==========
       
        if (canCast)
        {
        
           // ========== ВЕТКА ДЛЯ МАГОВ ==========
            if (_spell != null && _spell.CanCast())
            {
                if (!CanAct())
                {
                    EnterState(AIState.Fleeing);
                    return;
                }
                if (dist > PreferredCastDistance)
                {
                    _movement.MoveToward(_currentTarget);
                    return;
                }
                if (dist < MinCastDistance)
                {
                    // Слишком близко — либо бьём рукой, либо отступаем
                    if (CanMelee)
                    {
                        _body.Velocity = Vector2.Zero;
                        _combat.TryAttack(_currentTarget, delta);
                    }
                    else
                    {
                        EnterState(AIState.Fleeing);
                    }
                    return;
                }

                // Идеальная дистанция — кастуем
                _body.Velocity = Vector2.Zero;
                if (_spell.TryCast(_currentTarget, delta))
                {
                    // Если каст начался или идёт, переходим в состояние Casting
                    if (_spell.IsCasting())
                        EnterState(AIState.Casting);
                    return;
                }
            }
        }

       // ========== ВЕТКА ДЛЯ ОБЫЧНЫХ БОЙЦОВ (без магии) ==========

       // Ветка для обычных бойцов (без магии)
        if (!CanAct())
        {
            EnterState(AIState.Fleeing);
            return;
        }

        // Командир переходит в Commanding (оставляем без изменений)
        bool isCommander = _body?.GetNodeOrNull<SquadCommander>("SquadCommander") != null;
        if (isCommander && dist > MinCastDistance)
        {
            EnterState(AIState.Commanding);
            return;
        }

        // Пытаемся атаковать
        _body.Velocity = Vector2.Zero;
        if (_combat.TryAttack(_currentTarget, delta))
            return;

        // Если не можем атаковать (далеко) – идём на сближение
        if (dist > 40.0f)
            _movement.MoveToward(_currentTarget);
    }
	

	public void ResetFear()
	{
		_currentFear = 0f;
	}

    private bool CanAct()
    {
        var status = _body?.GetNodeOrNull<StatusEffectsComponent>("StatusEffectsComponent");
        return status?.CanAct() ?? true; // если нет компонента, то действовать можно
    }

}



// Вспомогательный класс приказа
public class OrderData
{
    public OrderType Type { get; set; }
    public int CommanderBonus { get; set; }
    public Vector2 TargetPosition { get; set; }
    public Node2D Target { get; set; } 
}

public enum OrderType { Assault, Retreat, Defend, Investigate, HoldPosition, MoveTo }