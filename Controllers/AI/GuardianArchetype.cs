using Godot;
using System.Linq;
using System.Collections.Generic;

[GlobalClass]
public partial class GuardianArchetype : BaseArchetype
{
    [Export] public float GuardRadius = 120.0f; // дистанция, на которой рыцарь начинает беспокоиться
    [Export] public float AttackRange = 40.0f;
    [Export] public float Damage = 15.0f;
    [Export] public float AttackCooldown = 1.0f;
    [Export] public float Speed = 80.0f;

    private float _attackTimer = 0f;
    private Node2D _guardTarget;

    public override bool HandleOrder(OrderData order)
    {
        if (order.Type == OrderType.Assault)
        {
            _behavior.EnterState(AIState.Attacking);
            ProcessAttack(_behavior, 0f);
            return true;
        }
        return false;
    }

    public override bool ProcessAttack(EnemyBehavior owner, double delta)
    {
        // 1. Определить, кого защищать
        if (_guardTarget == null || !IsInstanceValid(_guardTarget))
        {
            _guardTarget = FindGuardTarget();
        }

        if (_guardTarget == null)
        {
            _body.Velocity = Vector2.Zero;
            return true;
        }

        // 2. Поиск ближайшего врага, угрожающего защищаемой цели
        var enemy = FindClosestEnemyThreatening(_guardTarget);
        if (enemy != null)
        {
            float distToEnemy = _body.GlobalPosition.DistanceTo(enemy.GlobalPosition);
            if (distToEnemy <= AttackRange)
            {
                // Атакуем
                _body.Velocity = Vector2.Zero;
                _attackTimer += (float)delta;
                if (_attackTimer >= AttackCooldown)
                {
                    _attackTimer = 0f;
                    PerformAttack(enemy, owner);
                }
            }
            else
            {
                // Идём навстречу угрозе
                Vector2 dirToEnemy = (enemy.GlobalPosition - _body.GlobalPosition).Normalized();
                _body.Velocity = dirToEnemy * Speed;
            }
            return true;
        }

        // 3. Если угроз нет – подойти к защищаемому
        float distToGuard = _body.GlobalPosition.DistanceTo(_guardTarget.GlobalPosition);
        if (distToGuard > GuardRadius * 0.8f)
        {
            Vector2 dir = (_guardTarget.GlobalPosition - _body.GlobalPosition).Normalized();
            _body.Velocity = dir * Speed;
        }
        else
        {
            _body.Velocity = Vector2.Zero;
        }
        return true;
    }

    private Node2D FindClosestEnemyThreatening(Node2D guardTarget)
    {
        Node2D bestEnemy = null;
        float bestScore = float.MinValue;

        foreach (var node in _body.GetTree().GetNodesInGroup("Enemies"))
        {
            if (node == _body) continue;
            var enemyNode = node as Node2D;
            if (enemyNode == null) continue;
            var behavior = enemyNode.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
            if (behavior == null || !FactionManager.AreHostile(_behavior.FactionId, behavior.FactionId))
                continue;

            float enemyDistToGuard = guardTarget.GlobalPosition.DistanceTo(enemyNode.GlobalPosition);
            if (enemyDistToGuard <= GuardRadius)
            {
                float score = (GuardRadius - enemyDistToGuard) * 10f; 
                float myDistToEnemy = _body.GlobalPosition.DistanceTo(enemyNode.GlobalPosition);
                score -= myDistToEnemy * 0.5f;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestEnemy = enemyNode;
                }
            }
        }
        return bestEnemy;
    }

    private Node2D FindGuardTarget()
    {
        // Приоритет: командир (если жив)
        if (_behavior.Commander != null && IsInstanceValid(_behavior.Commander))
        {
            return _behavior.Commander.GetParent<CharacterBody2D>();
        }

        // Иначе самый раненый союзник
        if (_behavior.Commander != null && IsInstanceValid(_behavior.Commander))
        {
            EnemyBehavior bestAlly = null;
            float lowestHp = 1f;
            foreach (var member in _behavior.Commander.GetMembers())
            {
                if (member == _behavior) continue;
                var body = member.GetParent<CharacterBody2D>();
                var hp = body.GetNode<HealthComponent>("HealthComponent");
                if (hp != null && hp.CurrentHealth / hp.MaxHealth < lowestHp)
                {
                    lowestHp = hp.CurrentHealth / hp.MaxHealth;
                    bestAlly = member;
                }
            }
            return bestAlly?.GetParent<CharacterBody2D>();
        }

        return null;
    }

    private void PerformAttack(Node2D target, EnemyBehavior owner)
    {
        var attackScene = GD.Load<PackedScene>("res://Entitys/Specials/MeleeAttack.tscn");
        var attack = attackScene.Instantiate<MeleeAttack>();
        attack.Scale = new Vector2(2.5f, 2.5f);
        attack.GlobalPosition = _body.GlobalPosition.Lerp(target.GlobalPosition, 0.5f);
        attack.LookAt(target.GlobalPosition);
        attack.Damage = Damage;
        owner.GetTree().CurrentScene.AddChild(attack);

        var targetHealth = target.GetNode<HealthComponent>("HealthComponent");
        targetHealth?.Damage(Damage);
    }
}