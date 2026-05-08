using Godot;
using System.Collections.Generic;


[GlobalClass]
public partial class StatusEffectsComponent : Node
{
    // --- Сигналы (исправлены) ---
    [Signal]
    public delegate void EffectAppliedEventHandler(string effectId);

    [Signal]
    public delegate void EffectRemovedEventHandler(string effectId);

    [Signal]
    public delegate void EffectTickedEventHandler(string effectId);

    private List<StatusEffect> _activeEffects = new();
    private CombatStatsComponent _stats;
    private HealthComponent _health;

    public override void _Ready()
    {
        _stats = GetNode<CombatStatsComponent>("../CombatStatsComponent");
        _health = GetNode<HealthComponent>("../HealthComponent");
    }

    

    public override void _Process(double delta)
    {
        // Обрабатываем только эффекты с типом длительности Seconds
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = _activeEffects[i];
            if (effect.DurationType != DurationType.Seconds) continue;

            effect.RemainingDuration -= (float)delta;
            if (effect.TickInterval > 0)
            {
                effect.TickTimer -= (float)delta;
                if (effect.TickTimer <= 0)
                {
                    effect.TickTimer = effect.TickInterval;
                    ApplyTick(effect);
                }
            }

            if (effect.RemainingDuration <= 0)
            {
                RemoveEffect(effect);
            }
        }
    }

    public bool HasEffect(string effectId)
    {
        foreach (var effect in _activeEffects)
        {
            if (effect.Id == effectId)
                return true;
        }
        return false;
    }

    public void ApplyEffectById(string effectId, float duration)
    {
        if (string.IsNullOrEmpty(effectId))
        {
            GD.PrintErr("ApplyEffectById: effectId is null or empty");
            return;
        }

        string path = $"res://Resources/StatusEffects/{effectId}.tres";
        if (!ResourceLoader.Exists(path))
        {
            GD.PrintErr($"Effect resource not found: {path}");
            return;
        }

        var effectResource = GD.Load<StatusEffect>(path);
        if (effectResource == null)
        {
            GD.PrintErr($"Failed to load effect: {path}");
            return;
        }

        // Клонируем, чтобы каждый экземпляр эффекта на цели был независимым
        var effectInstance = (StatusEffect)effectResource.Duplicate();
        effectInstance.Duration = duration;
        ApplyEffect(effectInstance);
    }

    public void OnRoundPassed()
    {
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = _activeEffects[i];
            if (effect.DurationType != DurationType.Rounds) continue;

            effect.RemainingDuration -= 1;
            if (effect.TickInterval > 0)
            {
                effect.TickTimer -= 1;
                if (effect.TickTimer <= 0)
                {
                    effect.TickTimer = effect.TickInterval;
                    ApplyTick(effect);
                }
            }

            if (effect.RemainingDuration <= 0)
            {
                RemoveEffect(effect);
            }
        }
    }

    public void ApplyEffect(StatusEffect effectTemplate)
    {
        var effect = (StatusEffect)effectTemplate.Duplicate();
        effect.RemainingDuration = effect.Duration;
        effect.TickTimer = effect.TickInterval;

        _activeEffects.Add(effect);

        // Добавляем модификаторы
        foreach (var modData in effect.StatModifiers)
        {
            var modifier = new StatModifier { Value = modData.Value, Type = modData.Type };
            _stats.AddModifier(modData.StatName, modifier);
        }

        effect.ExecuteOnApply(this); // используем метод вместо делегата
        GD.Print($"Emitting EffectApplied with ID: '{effect.Id}'");
        EmitSignal(SignalName.EffectApplied, effect.Id);
        GD.Print($"{Owner.Name} получает эффект: {effect.Name} (ID: {effect.Id})");
    }

    public void RemoveEffect(StatusEffect effect)
    {
        if (!_activeEffects.Contains(effect)) return;

        // Удаляем модификаторы
        foreach (var modData in effect.StatModifiers)
        {
            var modifier = new StatModifier { Value = modData.Value, Type = modData.Type };
            _stats.RemoveModifier(modData.StatName, modifier);
        }

        effect.ExecuteOnExpire(this);

        _activeEffects.Remove(effect);
        EmitSignal(SignalName.EffectRemoved, effect.Id);
        GD.Print($"{Owner.Name} теряет эффект: {effect.Name}");
    }

    public void ClearAllEffects()
    {
        foreach (var effect in _activeEffects.ToArray())
        {
            RemoveEffect(effect);
        }
    }

    public bool CanAct()
    {
        foreach (var effect in _activeEffects)
            if (effect.PreventActions) return false;
        return true;
    }

    public bool CanMove()
    {
        foreach (var effect in _activeEffects)
            if (effect.PreventMovement) return false;
        return true;
    }

    private void ApplyTick(StatusEffect effect)
    {
        effect.ExecuteOnTick(this);
        EmitSignal(SignalName.EffectTicked, effect.Id);
    }

    // Вспомогательные методы
    public void DealDamage(float amount) => _health?.Damage(amount);
    public void HealDamage(float amount) => _health?.Heal(amount);
    public CombatStatsComponent GetStats() => _stats;
    public HealthComponent GetHealth() => _health;
}