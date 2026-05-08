using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class SpellData : Resource
{
    [Export] public string SpellName { get; set; } = "Новое заклинание";
    [Export] public Texture2D Icon { get; set; }

    public List<ModifierData> Modifiers { get; set; } = new();

    // Вычисляемые итоговые характеристики
    public float TotalManaCost { get; private set; }
    public float TotalCooldown { get; private set; }
    public float DamageMultiplier { get; private set; } = 1f;
    public float RadiusMultiplier { get; private set; } = 1f;
    public string PrimaryElement { get; private set; }
    public float TotalCastTime { get; private set; }

    // Пересчитать все параметры на основе списка модификаторов
    public void RecalculateStats()
    {
        TotalManaCost = 0f;
        TotalCooldown = 0f;
        DamageMultiplier = 1f;
        RadiusMultiplier = 1f;
        PrimaryElement = "Mana";

        TotalCastTime = 0f;

        foreach (var mod in Modifiers)
        {
            TotalManaCost += mod.ManaCost;
            TotalCooldown += mod.Cooldown;
            DamageMultiplier *= mod.DamageMultiplier;
            RadiusMultiplier *= mod.RadiusMultiplier;
            TotalCastTime += mod.CastTimeBonus;

            if (mod.Type == ModifierType.Element && mod.RequiredElement != CoreElement.Mana)
                PrimaryElement = mod.RequiredElement.ToString();
        }
    }
}