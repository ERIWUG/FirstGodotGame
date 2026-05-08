using Godot;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
public partial class SquadCommander : Node
{
    [Export] public float CommandRadius = 500.0f;
    [Export] public float OrderInterval = 2.0f; // как часто обновляет приказы

    private EnemyBehavior _myBehavior;
    private List<EnemyBehavior> _squadMembers = new();
    private float _orderTimer;

	public string SquadName { get; set; } = "Отряд";

	private int _initialTotal = 0;

	public List<EnemyBehavior> GetMembers()
	{
		CleanupSquad();
		return _squadMembers;
	}

    public override void _Ready()
    {
       _myBehavior = GetParent().GetNode<EnemyBehavior>("EnemyBehavior");

		// Включаем иконку короны, если она есть
		var crown = GetParent().GetNodeOrNull<Sprite2D>("CommanderCrown");
		if (crown != null)
			crown.Visible = true;
    }

	public void NotifyRetreat()
	{
		if (_myBehavior == null) return;
		foreach (var member in _squadMembers)
		{
			if (member != null && IsInstanceValid(member))
				member.ExecuteOrder(new OrderData { Type = OrderType.Retreat });
		}
		_myBehavior.EnterState(AIState.Fleeing);
	}

    public void GatherSquad()
	{
		_squadMembers.Clear();
		var allEnemies = GetTree().GetNodesInGroup("Enemies");
		var myBody = GetParent<CharacterBody2D>();

		foreach (var node in allEnemies)
		{
			// Пропускаем себя
			if (node == myBody) continue;

			// Приводим к Node2D, потому что все враги — CharacterBody2D
			var enemyNode = node as Node2D;
			if (enemyNode == null) continue;

			// Проверяем дистанцию
			if (myBody.GlobalPosition.DistanceTo(enemyNode.GlobalPosition) > CommandRadius)
				continue;

			var behavior = enemyNode.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
			if (behavior != null && behavior.FactionId == _myBehavior.FactionId)
			{
				_squadMembers.Add(behavior);
				behavior.SquadName = this.SquadName;   // ← даём имя отряда
			}
			
			 _initialTotal = _squadMembers.Count + 1;
		}
	}

    private AIState _prevState = AIState.Idle;
	public override void _Process(double delta)
	{
		if (GetTree().Paused) return;
		if (_myBehavior.CurrentState != AIState.Attacking && _myBehavior.CurrentState != AIState.Commanding) return;

		// Если только что перешли в атакующее/командное состояние – немедленно обновляем тактику
		if ((_myBehavior.CurrentState == AIState.Attacking || _myBehavior.CurrentState == AIState.Commanding) && 
			(_prevState != AIState.Attacking && _prevState != AIState.Commanding))
		{
			_orderTimer = OrderInterval; // заставляем сработать немедленно
		}
		_prevState = _myBehavior.CurrentState;

		_orderTimer += (float)delta;
		if (_orderTimer >= OrderInterval)
		{
			_orderTimer = 0;
			IssueOrders();
		}
	}

	public void CleanupSquad()
	{
		_squadMembers.RemoveAll(member => !IsInstanceValid(member) || !IsInstanceValid(member.GetParent()) || member.CurrentState == AIState.Dead);
	}

 	public void IssueOrders()
	{
		if (_myBehavior == null) return;

		var myBody = GetParent<CharacterBody2D>();
		var myBehavior = _myBehavior;
		var myHealth = myBody.GetNode<HealthComponent>("HealthComponent");

		// Подсчитываем потери ДО очистки списка
		int totalMembers = _initialTotal > 0 ? _initialTotal : _squadMembers.Count + 1; // +1 — сам командир
		int aliveMembers = 1; // командир жив, если не в Dead
		if (myBehavior.CurrentState == AIState.Dead) aliveMembers = 0;

		foreach (var member in _squadMembers)
		{
			if (IsInstanceValid(member) && member.CurrentState != AIState.Dead)
				aliveMembers++;
		}

		// Удаляем мёртвых
		CleanupSquad();

		// Отступление, если потеряли >= половину отряда
		if (aliveMembers <= totalMembers / 2)
		{
			SquadJournal.Instance?.AddEntry($"Отряд {SquadName} отступает (потеряно больше половины бойцов).");
			NotifyRetreat();
			return;
		}

		// 1. Тактика «Защита командира» – если лидер тяжело ранен
		if (myHealth != null && myHealth.CurrentHealth / myHealth.MaxHealth < 0.3f)
		{
			SquadJournal.Instance?.AddEntry($"{SquadName} тяжело ранен! Отряд строит защиту.");
			
			myBehavior.ExecuteOrder(new OrderData { Type = OrderType.Retreat });
			
			var bodyguards = _squadMembers.Where(m => m.GetParent().Name.ToString().Contains("Knight") || m.GetParent().Name.ToString().Contains("Ogre")).ToList();
			foreach (var guard in bodyguards)
			{
				Node2D closestEnemy = null;
				float closestDist = 100f;
				foreach (var node in GetTree().GetNodesInGroup("Enemies"))
				{
					if (node == myBody || node == guard.GetParent()) continue;
					var body = node as CharacterBody2D;
					if (body == null) continue;
					var behavior = body.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
					if (behavior == null || !FactionManager.AreHostile(myBehavior.FactionId, behavior.FactionId)) continue;
					
					float dist = guard.GetParent<CharacterBody2D>().GlobalPosition.DistanceTo(body.GlobalPosition);
					if (dist < closestDist)
					{
						closestDist = dist;
						closestEnemy = body;
					}
				}
				if (closestEnemy != null)
					guard.ExecuteOrder(new OrderData { Type = OrderType.Assault, Target = closestEnemy });
				else
					guard.ExecuteOrder(new OrderData { Type = OrderType.HoldPosition });
			}
			foreach (var member in _squadMembers)
			{
				if (!bodyguards.Contains(member) && IsInstanceValid(member))
					member.ExecuteOrder(new OrderData { Type = OrderType.HoldPosition });
			}
			return;
		}

		// 3. Поиск вражеского командира
		var target = myBehavior.GetCurrentTarget();
		EnemyBehavior enemyCommander = null;
		CharacterBody2D enemyCommanderBody = null;
		foreach (var node in GetTree().GetNodesInGroup("Enemies"))
		{
			if (node == myBody) continue;
			var body = node as CharacterBody2D;
			if (body == null) continue;
			var behavior = body.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
			if (behavior == null || !FactionManager.AreHostile(myBehavior.FactionId, behavior.FactionId)) continue;
			if (body.GetNodeOrNull<SquadCommander>("SquadCommander") != null)
			{
				enemyCommander = behavior;
				enemyCommanderBody = body;
				break;
			}
		}

		bool commanderHasMagesNearby = false;
		if (enemyCommander != null && enemyCommanderBody != null)
		{
			foreach (var node in GetTree().GetNodesInGroup("Enemies"))
			{
				if (node == enemyCommanderBody) continue;
				var body = node as CharacterBody2D;
				if (body == null) continue;
				var behavior = body.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
				if (behavior == null || behavior.FactionId != enemyCommander.FactionId) continue;
				if (body.GetNodeOrNull<SkillComponent>("SkillComponent") != null &&
					body.GlobalPosition.DistanceTo(enemyCommanderBody.GlobalPosition) < 200f)
				{
					commanderHasMagesNearby = true;
					break;
				}
			}
		}

		if (enemyCommander != null && !commanderHasMagesNearby)
		{
			SquadJournal.Instance?.AddEntry($"Отряд {SquadName} начинает охоту на вражеского командира {enemyCommander.UnitNameOverride}!");
			foreach (var member in _squadMembers)
				member.ExecuteOrder(new OrderData { Type = OrderType.Assault, Target = enemyCommander.GetParent() as Node2D });
			return;
		}

		// 4. Поиск вражеских магов
		var enemyMages = new System.Collections.Generic.List<EnemyBehavior>();
		foreach (var node in GetTree().GetNodesInGroup("Enemies"))
		{
			if (node == myBody) continue;
			var body = node as CharacterBody2D;
			if (body == null) continue;
			var behavior = body.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
			if (behavior == null || !FactionManager.AreHostile(myBehavior.FactionId, behavior.FactionId)) continue;
			if (body.GetNodeOrNull<SkillComponent>("SkillComponent") != null)
				enemyMages.Add(behavior);
		}

		if (enemyMages.Count > 0)
		{
			SquadJournal.Instance?.AddEntry($"Отряд {SquadName} отправляет смельчаков против вражеских магов.");
			var braveMembers = _squadMembers.Where(m => m.Courage >= 60).ToList();
			foreach (var mage in enemyMages)
				foreach (var brave in braveMembers)
					brave.ExecuteOrder(new OrderData { Type = OrderType.Assault, Target = mage.GetParent() as Node2D });

			foreach (var member in _squadMembers)
			{
				if (braveMembers.Contains(member) || !IsInstanceValid(member)) continue;
				if (target != null)
					member.ExecuteOrder(new OrderData { Type = OrderType.Assault, Target = target, CommanderBonus = 25 });
				else
					member.ExecuteOrder(new OrderData { Type = OrderType.HoldPosition });
			}
			return;
		}

		
		// 5. Стандартная тактика: охрана командира + атака
		var mages = _squadMembers.Where(m => m.GetParent().GetNodeOrNull<SkillComponent>("SkillComponent") != null).ToList();
		var guards = _squadMembers.Where(m => m.GetParent().Name.ToString().Contains("Knight") || m.GetParent().Name.ToString().Contains("Ogre")).ToList();
		var attackers = _squadMembers.Except(guards).ToList();
		if (target == null) return;

		// Телохранители занимают позиции вокруг командира
		Vector2 commanderPos = myBody.GlobalPosition;
		foreach (var guard in guards)
		{
			// Ищем ближайшего врага к телохранителю, но остаёмся между командиром и врагом
			Node2D closestEnemy = null;
			float closestDist = 150f;
			foreach (var node in GetTree().GetNodesInGroup("Enemies"))
			{
				if (node == myBody || node == guard.GetParent()) continue;
				var body = node as CharacterBody2D;
				if (body == null) continue;
				var behavior = body.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
				if (behavior == null || !FactionManager.AreHostile(myBehavior.FactionId, behavior.FactionId)) continue;
				
				float dist = guard.GetParent<CharacterBody2D>().GlobalPosition.DistanceTo(body.GlobalPosition);
				if (dist < closestDist)
				{
					closestDist = dist;
					closestEnemy = body;
				}
			}
			if (closestEnemy != null)
			{
				Vector2 dir = (commanderPos - closestEnemy.GlobalPosition).Normalized();
				Vector2 guardPos = commanderPos + dir * 60f; // 60 пикселей перед командиром
				guard.ExecuteOrder(new OrderData { Type = OrderType.MoveTo, TargetPosition = guardPos });
			}
			else
			{
				guard.ExecuteOrder(new OrderData { Type = OrderType.HoldPosition });
			}
		}

		// Все остальные (маги, наёмники, эльфы) атакуют цель
		foreach (var member in attackers)
		{
			if (!IsInstanceValid(member)) continue;
			member.ExecuteOrder(new OrderData { Type = OrderType.Assault, Target = target, CommanderBonus = 25 });
		}
	}

	private void ShowOrderLine(Vector2 from, Vector2 to)
	{
		var line = new Line2D();
		line.Points = new[] { Vector2.Zero, to - from }; // линия рисуется в локальных координатах родителя
		line.DefaultColor = new Color(1, 1, 1, 0.7f);
		line.Width = 2;
		GetParent().AddChild(line);
		line.GlobalPosition = from;
		
		// Удаляем через 0.5 секунды
		GetTree().CreateTimer(0.5f).Timeout += line.QueueFree;
	}
}