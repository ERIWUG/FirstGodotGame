using Godot;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
public partial class SkillComponent : Node
{
    [Signal]
    public delegate void SpellLearnedEventHandler(SpellData spell);

    [Signal]
    public delegate void SpellCastEventHandler(SpellData spell);

    [Signal]
    public delegate void CooldownUpdatedEventHandler(string spellId, float remainingCooldown);

    // Изученные заклинания
    private List<SpellData> _knownSpells = new();

    // Кулдауны: ключ — ID заклинания (или его имя), значение — таймер
    private Dictionary<string, float> _cooldowns = new();

    // Ссылки на другие компоненты
    private ResourceComponent _resources;
    private CombatStatsComponent _stats;
    private StatusEffectsComponent _statusEffects;
    private HealthComponent _health;

    public override void _Ready()
    {
        _resources = GetNodeOrNull<ResourceComponent>("../ResourceComponent");
        _stats = GetNodeOrNull<CombatStatsComponent>("../CombatStatsComponent");
        _statusEffects = GetNodeOrNull<StatusEffectsComponent>("../StatusEffectsComponent");
        _health = GetNode<HealthComponent>("../HealthComponent");
    }

    public override void _Process(double delta)
    {
        // Обновляем кулдауны
        var keys = _cooldowns.Keys.ToList();
        foreach (var key in keys)
        {
            if (_cooldowns[key] > 0)
            {
                _cooldowns[key] -= (float)delta;
                EmitSignal(SignalName.CooldownUpdated, key, _cooldowns[key]);
            }
        }
    }

    // Добавить заклинание в книгу
    public void LearnSpell(SpellData spell)
    {
        if (!_knownSpells.Contains(spell))
        {
            _knownSpells.Add(spell);
            EmitSignal(SignalName.SpellLearned, spell);
            GD.Print($"{Owner.Name} изучил заклинание: {spell.SpellName}");
        }
    }

    // Забыть заклинание
    public void ForgetSpell(SpellData spell)
    {
        _knownSpells.Remove(spell);
    }

    // Получить список всех известных заклинаний
    public List<SpellData> GetKnownSpells() => _knownSpells.ToList();

    // Проверить, готово ли заклинание (мана, кулдаун)
    public bool CanCast(SpellData spell)
    {
        if (spell == null) return false;
        if (!_knownSpells.Contains(spell)) return false;
        if (!_resources.HasEnough("MP", spell.TotalManaCost)) return false;
        if (_cooldowns.ContainsKey(spell.SpellName) && _cooldowns[spell.SpellName] > 0) return false;
        return true;
    }

    // Применить заклинание к цели
    public bool CastSpell(SpellData spell, Node target)
    {
        if (target == null) return false;
        if (!CanCast(spell)) return false;

        // Тратим ману
        _resources.Consume("MP", spell.TotalManaCost);

        // Запускаем кулдаун
        if (spell.TotalCooldown > 0)
            _cooldowns[spell.SpellName] = spell.TotalCooldown;

        // Рассчитываем урон
        float basePower = GetSpellPower(spell.PrimaryElement);
        float finalDamage = basePower * spell.DamageMultiplier;

        // Наносим урон, если цель имеет HealthComponent
        var targetHealth = target.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (targetHealth != null)
        {
            targetHealth.Damage(finalDamage);
        }

        // Применяем статусные эффекты
        foreach (var mod in spell.Modifiers)
        {
            if (!string.IsNullOrEmpty(mod.StatusEffectId))
            {
                var targetStatus = target.GetNodeOrNull<StatusEffectsComponent>("StatusEffectsComponent");
                targetStatus?.ApplyEffectById(mod.StatusEffectId, mod.StatusDuration);
            }
        }

        EmitSignal(SignalName.SpellCast, spell);
        GD.Print($"{Owner.Name} использовал {spell.SpellName}! Нанесено {finalDamage} урона.");
        return true;
    }

    // Рассчитать магическую силу для указанной стихии
    private float GetSpellPower(string element)
    {
        // Базовая формула: Интеллект + бонус от ядер
        float basePower = _stats.Intelligence * 1.5f;

        // Можно добавить бонусы от ядер (например, через модификаторы статов)
        // Пока просто возвращаем базовое значение
        return Mathf.Max(5, basePower);
    }

    // Сбросить все кулдауны (например, для тестов или особых эффектов)
    public void ResetAllCooldowns()
    {
        _cooldowns.Clear();
    }

    // Получить оставшийся кулдаун для заклинания
    public float GetCooldown(string spellName)
    {
        return _cooldowns.ContainsKey(spellName) ? _cooldowns[spellName] : 0f;
    }
}