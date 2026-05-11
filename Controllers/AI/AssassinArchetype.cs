using Godot;
using System.Linq;
using System.Collections.Generic;

[GlobalClass]
public partial class AssassinArchetype : BaseArchetype
{
    [Export] public float Speed = 200.0f;
    [Export] public float Damage = 30.0f;
    [Export] public float AttackCooldown = 0.6f;
    [Export] public float AttackRange = 30.0f;
    [Export] public float AvoidanceRadius = 80.0f;
    [Export] public float AvoidanceStrength = 400.0f;
    [Export] public float PushStrength = 150.0f;

    private float _attackTimer = 0f;
    private float _stuckTimer = 0f;
    private Vector2 _lastPosition;
    private const float StuckThreshold = 10f;

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
        // 1. Найти приоритетную цель
        var priorityTarget = FindPriorityTarget();
        if (priorityTarget == null)
        {
            _body.Velocity = Vector2.Zero;
            return true;
        }

        float distToPriority = _body.GlobalPosition.DistanceTo(priorityTarget.GlobalPosition);

        // 2. Если приоритетная цель в радиусе атаки – атаковать
        if (distToPriority <= AttackRange)
        {
            _body.Velocity = Vector2.Zero;
            _attackTimer += (float)delta;
            if (_attackTimer >= AttackCooldown)
            {
                _attackTimer = 0f;
                PerformAttack(priorityTarget, owner);
            }
            _stuckTimer = 0f;
            return true;
        }

        // 3. Проверим, не застряли ли мы. Если застряли и рядом есть враг – атакуем его, чтобы расчистить путь.
        float moved = _body.GlobalPosition.DistanceTo(_lastPosition);
        if (moved < StuckThreshold)
        {
            _stuckTimer += (float)delta;
        }
        else
        {
            _stuckTimer = 0f;
        }

        Node2D blockingEnemy = null;
        if (_stuckTimer > 0.2f)
        {
            blockingEnemy = FindClosestEnemyInRange(AttackRange * 1.5f);
        }

        if (blockingEnemy != null)
        {
            // Атакуем блокирующего врага
            _body.Velocity = Vector2.Zero;
            _attackTimer += (float)delta;
            if (_attackTimer >= AttackCooldown)
            {
                _attackTimer = 0f;
                PerformAttack(blockingEnemy, owner);
            }
            // Не сбрасываем _stuckTimer, чтобы продолжать атаковать, пока враг не умрёт или не отойдёт
            return true;
        }

        // 4. Двигаемся к приоритетной цели с avoidance и расталкиванием
        Vector2 desiredDirection = (priorityTarget.GlobalPosition - _body.GlobalPosition).Normalized();
        Vector2 desiredVelocity = desiredDirection * Speed;

        Vector2 avoidance = ComputeAvoidanceVelocity(desiredDirection, priorityTarget);
        Vector2 push = ComputePushVelocity(priorityTarget);
        Vector2 finalVelocity = (desiredVelocity + avoidance + push).Normalized() * Speed;

        if (_stuckTimer > 0.3f)
        {
            Vector2 randomDir = new Vector2((float)GD.Randf() * 2 - 1, (float)GD.Randf() * 2 - 1).Normalized();
            finalVelocity = randomDir * Speed * 0.8f;
            _stuckTimer = 0f;
        }

        _body.Velocity = finalVelocity;
        _lastPosition = _body.GlobalPosition;
        return true;
    }

    private Node2D FindClosestEnemyInRange(float range)
    {
        Node2D closest = null;
        float closestDist = range;
        foreach (var node in _body.GetTree().GetNodesInGroup("Enemies"))
        {
            if (node == _body) continue;
            var enemyNode = node as Node2D;
            if (enemyNode == null) continue;
            var behavior = enemyNode.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
            if (behavior == null || !FactionManager.AreHostile(_behavior.FactionId, behavior.FactionId))
                continue;
            float dist = _body.GlobalPosition.DistanceTo(enemyNode.GlobalPosition);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = enemyNode;
            }
        }
        return closest;
    }

    private Vector2 ComputePushVelocity(Node2D priorityTarget)
    {
        Vector2 push = Vector2.Zero;
        foreach (var node in _body.GetTree().GetNodesInGroup("Enemies"))
        {
            if (node == _body || node == priorityTarget) continue;
            var otherBody = node as CharacterBody2D;
            if (otherBody == null) continue;

            Vector2 toOther = otherBody.GlobalPosition - _body.GlobalPosition;
            float distance = toOther.Length();
            float minDist = 20f;

            if (distance < minDist && distance > 0.1f)
            {
                float strength = Mathf.InverseLerp(minDist, 0, distance) * PushStrength;
                push += -toOther.Normalized() * strength;
            }
        }
        return push;
    }

    private Vector2 ComputeAvoidanceVelocity(Vector2 desiredDirection, Node2D priorityTarget)
    {
        Vector2 avoidance = Vector2.Zero;
        foreach (var node in _body.GetTree().GetNodesInGroup("Enemies"))
        {
            if (node == _body || node == priorityTarget) continue;
            var otherBody = node as CharacterBody2D;
            if (otherBody == null) continue;

            Vector2 toOther = otherBody.GlobalPosition - _body.GlobalPosition;
            float distance = toOther.Length();

            if (distance < AvoidanceRadius && distance > 0.1f)
            {
                float strength = Mathf.InverseLerp(AvoidanceRadius, 0, distance);
                Vector2 pushDir = -toOther.Normalized();
                avoidance += pushDir * strength * AvoidanceStrength;
            }
        }
        return avoidance;
    }

    private Node2D FindPriorityTarget()
    {
        Node2D bestTarget = null;
        float closestCaster = float.MaxValue;
        float closestAny = float.MaxValue;

        foreach (var node in _body.GetTree().GetNodesInGroup("Enemies"))
        {
            if (node == _body) continue;
            var enemyNode = node as Node2D;
            if (enemyNode == null) continue;
            var behavior = enemyNode.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
            if (behavior == null || !FactionManager.AreHostile(_behavior.FactionId, behavior.FactionId))
                continue;

            float dist = _body.GlobalPosition.DistanceTo(enemyNode.GlobalPosition);
            bool isCaster = behavior.GetParent().GetNodeOrNull<SkillComponent>("SkillComponent") != null
                            && !behavior.CanMelee;

            if (isCaster && dist < closestCaster)
            {
                closestCaster = dist;
                bestTarget = enemyNode;
            }
            if (dist < closestAny)
            {
                closestAny = dist;
                if (!isCaster && bestTarget == null)
                    bestTarget = enemyNode;
            }
        }

        return bestTarget;
    }

    private void PerformAttack(Node2D target, EnemyBehavior owner)
    {
        var attackScene = GD.Load<PackedScene>("res://Entitys/Specials/MeleeAttack.tscn");
        var attack = attackScene.Instantiate<MeleeAttack>();
        attack.Scale = new Vector2(2.2f, 2.2f);
        attack.GlobalPosition = _body.GlobalPosition.Lerp(target.GlobalPosition, 0.5f);
        attack.LookAt(target.GlobalPosition);
        attack.Damage = Damage;
        owner.GetTree().CurrentScene.AddChild(attack);

        var targetHealth = target.GetNode<HealthComponent>("HealthComponent");
        targetHealth?.Damage(Damage);
    }
}