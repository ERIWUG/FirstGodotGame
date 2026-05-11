using Godot;
using System.Linq;
using System.Collections.Generic;

[GlobalClass]
public partial class HealerArchetype : BaseArchetype
{
    [Export] public float HealThreshold = 0.7f;

    public override Node2D OverrideTarget(Node2D currentTarget)
    {
        if (_skill == null) return currentTarget;
        if (_behavior?.Commander == null) return currentTarget;

        // Ищем лечащее заклинание (хотя бы одно)
        var healSpell = _skill.GetKnownSpells().FirstOrDefault(s => s.Modifiers.Any(m => m.Id == "heal"));
        if (healSpell == null) return currentTarget;

        // Находим самого раненого союзника
        EnemyBehavior bestAlly = null;
        float lowestHp = 1f;
        foreach (var member in _behavior.Commander.GetMembers())
        {
            if (member == _behavior) continue;
            if (!IsInstanceValid(member)) continue;
            var body = member.GetParent<CharacterBody2D>();
            var hp = body.GetNode<HealthComponent>("HealthComponent");
            if (hp == null) continue;
            float ratio = hp.CurrentHealth / hp.MaxHealth;
            if (ratio < HealThreshold && ratio < lowestHp)
            {
                lowestHp = ratio;
                bestAlly = member;
            }
        }

        if (bestAlly != null)
            return bestAlly.GetParent<CharacterBody2D>();

        // Нет раненых — остаёмся на месте, цель не важна
        return null;
    }

    public override bool HandleOrder(OrderData order)
    {
        if (order.Type == OrderType.Assault)
        {
            var ally = OverrideTarget(null);
            if (ally != null)
            {
                _behavior.ForceTarget(ally);
                _behavior.EnterState(AIState.Attacking);
                return true;
            }
            _behavior.EnterState(AIState.Idle);
            return true;
        }
        return false;
    }

    public override SpellData SelectSpell(List<SpellData> spells, Node2D currentTarget)
    {
        // Просто возвращаем первое лечащее заклинание
        return spells.FirstOrDefault(s => s.Modifiers.Any(m => m.Id == "heal"));
    }
    public override bool ProcessAttack(EnemyBehavior owner, double delta)
    {
        // 1. Если есть раненый союзник – выбираем его целью
        var ally = OverrideTarget(owner.GetCurrentTarget());
        if (ally != null)
        {
            owner.ForceTarget(ally);
        }
        else
        {
            // Нет раненых – стоим на месте
            owner.EnterState(AIState.Idle);
            return true;
        }

        // 2. Если можем кастовать – кастуем
        if (_skill != null && _resources != null)
        {
            var spell = SelectSpell(_skill.GetKnownSpells(), ally);
            if (spell != null && _skill.CanCast(spell))
            {
                // Останавливаемся и начинаем каст
                _body.Velocity = Vector2.Zero;
                owner.TryCastSpell(); // используем существующий метод врага
                return true;
            }
        }

        // 3. Если каст невозможен (кулдаун, нет маны) – просто ждём
        _body.Velocity = Vector2.Zero;
        return true;
    }
}