using Godot;
using System;

[GlobalClass]
public partial class SoulComponent : Node
{
    // --- Сигналы ---
    [Signal]
    public delegate void PotentialChangedEventHandler(int maxSlots);

    [Signal]
    public delegate void ManaControlChangedEventHandler(float controlLevel);

    // --- Экспортируемые параметры ---
    [Export] public int BasePotential { get; set; } = 5; // Максимальное количество слотов ядер (1-10)
    [Export] public float BaseManaControl { get; set; } = 0f; // Стартовый контроль маны

    // --- Публичные свойства ---
    public int MaxCoreSlots { get; private set; }
    public float ManaControl { get; private set; }

    public override void _Ready()
    {
        MaxCoreSlots = BasePotential;
        ManaControl = BaseManaControl;
    }

    // Увеличить контроль маны (вызывается при успешном использовании магии)
    public void IncreaseManaControl(float amount)
    {
        ManaControl += amount;
        EmitSignal(SignalName.ManaControlChanged, ManaControl);
        GD.Print($"{Owner.Name}: Контроль Маны повышен до {ManaControl}");
    }

    // Проверить, можно ли добавить ещё одно ядро
    public bool CanAddCore(int currentCoreCount)
    {
        return currentCoreCount < MaxCoreSlots;
    }

    // Установить новый потенциал (например, после легендарного квеста)
    public void SetPotential(int newPotential)
    {
        MaxCoreSlots = Mathf.Clamp(newPotential, 1, 10);
        EmitSignal(SignalName.PotentialChanged, MaxCoreSlots);
    }
}