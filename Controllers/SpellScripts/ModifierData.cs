using Godot;

public enum ModifierType
{
    Form,       // Снаряд, Луч, Аура
    Element,    // Огонь, Вода, Воздух...
    Effect      // Горение, Взрыв, Замедление...
}

[GlobalClass]
public partial class ModifierData : Resource
{
    [Export] public string Id { get; set; }                 // Уникальный идентификатор (например "fire_arrow")
    [Export] public string Name { get; set; }               // Отображаемое имя
    [Export] public Texture2D Icon { get; set; }
    [Export] public ModifierType Type { get; set; }
    [Export] public CoreElement RequiredElement { get; set; } // Какая стихия нужна (или Mana для универсальных)
    [Export] public int RequiredCoreRank { get; set; } = 1;   // Минимальный ранг ядра
    [Export] public float ManaCost { get; set; } = 5f;        // Добавочная стоимость MP
    [Export] public float Cooldown { get; set; } = 0f;        // Добавочный кулдаун
    [Export] public float CastTimeBonus { get; set; } = 0.0f;

    // Эффекты, которые этот модификатор применяет к заклинанию
    [Export] public float DamageMultiplier { get; set; } = 1.0f;
    [Export] public float RadiusMultiplier { get; set; } = 1.0f;
    [Export] public float SpeedMultiplier { get; set; } = 1.0f;
    [Export] public string StatusEffectId { get; set; }       // ID эффекта для StatusEffectsComponent
    [Export] public float StatusDuration { get; set; } = 0f;
}