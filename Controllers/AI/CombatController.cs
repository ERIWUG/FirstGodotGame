using Godot;

[GlobalClass]
public partial class CombatController : Node
{
    [Export] public float AttackCooldown = 1.0f;
    [Export] public float AttackRange = 40.0f;
    [Export] public float Damage = 10.0f;
    [Export] public float MeleeAttackScale = 2.5f;

    private CharacterBody2D _body;
    private float _attackTimer = 0f;
    private PackedScene _meleeAttackScene;

    public override void _Ready()
    {
        _body = GetParent<CharacterBody2D>();
        _meleeAttackScene = GD.Load<PackedScene>("res://Entitys/Specials/MeleeAttack.tscn");
    }

    /// <summary>
    /// Пытается атаковать цель. Возвращает true, если атака произошла.
    /// </summary>
    public bool TryAttack(Node2D target, double delta)
    {
        if (target == null || !IsInstanceValid(target)) return false;
        
        float dist = _body.GlobalPosition.DistanceTo(target.GlobalPosition);
        if (dist > AttackRange) return false;

        _attackTimer += (float)delta;
        if (_attackTimer >= AttackCooldown)
        {
            _attackTimer = 0f;
            PerformAttack(target);
            return true;
        }
        return false;
    }

    private void PerformAttack(Node2D target)
    {
        // Визуал удара
        if (_meleeAttackScene != null)
        {
            var attack = _meleeAttackScene.Instantiate<MeleeAttack>();
            attack.Scale = new Vector2(MeleeAttackScale, MeleeAttackScale);
            attack.GlobalPosition = _body.GlobalPosition.Lerp(target.GlobalPosition, 0.5f);
            attack.LookAt(target.GlobalPosition);
            attack.Damage = Damage;
            _body.GetTree().CurrentScene.AddChild(attack);
        }

        // Наносим урон
        var targetHealth = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        targetHealth?.Damage(Damage);
    }
}