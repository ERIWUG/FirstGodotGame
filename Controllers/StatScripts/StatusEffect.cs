using Godot;
using System.Collections.Generic;

public enum DurationType
{
    Seconds,   // Истекает по реальному времени (через _Process)
    Rounds,    // Истекает по окончании раундов (управляется TurnManager)
    Permanent  // Пока не снимут вручную
}

public struct StatModifierData
{
    public string StatName;
    public float Value;
    public StatModifierType Type;
}

[GlobalClass]
public partial class StatusEffect : Resource
{
    [Export] public string Id { get; set; }
    [Export] public string Name { get; set; }
    [Export] public Texture2D Icon { get; set; }

    [Export] public DurationType DurationType { get; set; } = DurationType.Seconds;
    [Export] public float Duration { get; set; }
    [Export] public float TickInterval { get; set; } = 1.0f;

    [Export] public bool IsDebuff { get; set; }
    [Export] public bool PreventActions { get; set; }
    [Export] public bool PreventMovement { get; set; }

    public List<StatModifierData> StatModifiers { get; set; } = new();

    public float RemainingDuration { get; set; }
    public float TickTimer { get; set; }

    // Виртуальные методы для переопределения в наследниках
    public virtual void OnApply(StatusEffectsComponent target) { }
    public virtual void OnTick(StatusEffectsComponent target) { }
    public virtual void OnExpire(StatusEffectsComponent target) { }

    // Внутренние методы для вызова из компонента
    public void ExecuteOnApply(StatusEffectsComponent target) => OnApply(target);
    public void ExecuteOnTick(StatusEffectsComponent target) => OnTick(target);
    public void ExecuteOnExpire(StatusEffectsComponent target) => OnExpire(target);
}