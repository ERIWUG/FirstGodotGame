using Godot;
using System.Linq;
using System.Collections.Generic;

[GlobalClass]
public partial class NecromancerArchetype : BaseArchetype
{
    [Export] public float PreferredCastDistance = 300.0f;
    [Export] public float MinCastDistance = 100.0f;

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
        // Ищем врага для атаки/поднятия
        var target = FindTarget();
        if (target == null)
        {
            _body.Velocity = Vector2.Zero;
            return true;
        }

        float dist = _body.GlobalPosition.DistanceTo(target.GlobalPosition);

        // Если цель далеко — подходим
        if (dist > PreferredCastDistance)
        {
            Vector2 dir = (target.GlobalPosition - _body.GlobalPosition).Normalized();
            _body.Velocity = dir * 100f; // используем стандартную скорость, можно вынести в [Export]
            return true;
        }

        // Если цель слишком близко — отступаем
        if (dist < MinCastDistance)
        {
            Vector2 dir = (_body.GlobalPosition - target.GlobalPosition).Normalized();
            _body.Velocity = dir * 100f;
            return true;
        }

        // Идеальная дистанция — кастуем
        _body.Velocity = Vector2.Zero;
        owner.ForceTarget(target);
        owner.TryCastSpell(); 
        return true;
    }

    private Node2D FindTarget()
    {
        // Ищем врага, на котором ещё нет метки смерти
        Node2D bestTarget = null;
        float closestDist = float.MaxValue;
        foreach (var node in _body.GetTree().GetNodesInGroup("Enemies"))
        {
            if (node == _body) continue;
            var enemyNode = node as Node2D;
            if (enemyNode == null) continue;
            var behavior = enemyNode.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
            if (behavior == null || !FactionManager.AreHostile(_behavior.FactionId, behavior.FactionId))
                continue;

            // Проверяем, нет ли уже на нём эффекта raise_dead
            var statusComp = enemyNode.GetNodeOrNull<StatusEffectsComponent>("StatusEffectsComponent");
            if (statusComp != null && statusComp.HasEffect("raise_dead"))
                continue;

            float dist = _body.GlobalPosition.DistanceTo(enemyNode.GlobalPosition);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = enemyNode;
            }
        }
        return bestTarget;
    }

    public override SpellData SelectSpell(List<SpellData> spells, Node2D currentTarget)
    {
        // Если цель не помечена, используем "Поднять скелета"
        var targetBody = currentTarget as CharacterBody2D;
        if (targetBody != null)
        {
            var statusComp = targetBody.GetNodeOrNull<StatusEffectsComponent>("StatusEffectsComponent");
            if (statusComp == null || !statusComp.HasEffect("raise_dead"))
            {
                var raiseSpell = spells.FirstOrDefault(s => s.Modifiers.Any(m => m.StatusEffectId == "raise_dead"));
                if (raiseSpell != null)
                    return raiseSpell;
            }
        }
        // Иначе атакуем обычным заклинанием
        return spells.FirstOrDefault();
    }
}