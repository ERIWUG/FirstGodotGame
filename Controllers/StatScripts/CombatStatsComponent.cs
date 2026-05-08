using Godot;
using System.Collections.Generic;

public enum StatModifierType
{
    Flat,
    PercentAdd,
    PercentMult
}

public struct StatModifier
{
    public float Value { get; set; }
    public StatModifierType Type { get; set; }
}

[GlobalClass]
public partial class CombatStatsComponent : Node
{
    // --- Сигналы ---
    [Signal]
    public delegate void StatChangedEventHandler(string statName, float newValue);

    // --- Базовые статы ---
    [Export] public float BaseStrength { get; set; } = 10f;
    [Export] public float BaseDexterity { get; set; } = 10f;
    [Export] public float BaseConstitution { get; set; } = 10f;
    [Export] public float BaseIntelligence { get; set; } = 10f;
    [Export] public float BaseWisdom { get; set; } = 10f;
    [Export] public float BaseCharisma { get; set; } = 10f;

    // Словари модификаторов
    private Dictionary<string, List<StatModifier>> _modifiers = new()
    {
        {"Strength", new List<StatModifier>()},
        {"Dexterity", new List<StatModifier>()},
        {"Constitution", new List<StatModifier>()},
        {"Intelligence", new List<StatModifier>()},
        {"Wisdom", new List<StatModifier>()},
        {"Charisma", new List<StatModifier>()}
    };
    private bool _isDirty = true;

    // Кэш финальных значений
    private float _cachedStrength;
    public float Strength => EnsureCalculated() ? _cachedStrength : 0;

    private float _cachedDexterity;
    public float Dexterity => EnsureCalculated() ? _cachedDexterity : 0;

    private float _cachedConstitution;
    public float Constitution => EnsureCalculated() ? _cachedConstitution : 0;

    private float _cachedIntelligence;
    public float Intelligence => EnsureCalculated() ? _cachedIntelligence : 0;

    private float _cachedWisdom;
    public float Wisdom => EnsureCalculated() ? _cachedWisdom : 0;

    private float _cachedCharisma;
    public float Charisma => EnsureCalculated() ? _cachedCharisma : 0;

    public override void _Ready()
    {
        _modifiers["Strength"] = new List<StatModifier>();
        _modifiers["Dexterity"] = new List<StatModifier>();
        _modifiers["Constitution"] = new List<StatModifier>();
        _modifiers["Intelligence"] = new List<StatModifier>();
        _modifiers["Wisdom"] = new List<StatModifier>();
        _modifiers["Charisma"] = new List<StatModifier>();

        RecalculateAll();
    }

    public void IncreaseBaseStat(string statName, float amount)
    {
        switch (statName)
        {
            case "Strength":  BaseStrength += amount; break;
            case "Dexterity": BaseDexterity += amount; break;
            case "Constitution": BaseConstitution += amount; break;
            case "Intelligence": BaseIntelligence += amount; break;
            case "Wisdom": BaseWisdom += amount; break;
            case "Charisma": BaseCharisma += amount; break;
            default: return;
        }
        RecalculateAll(); // Это обновит кэш и отправит сигнал StatChanged
    }

    public void AddModifier(string statName, StatModifier modifier)
    {
        if (!_modifiers.ContainsKey(statName))
        {
            GD.PrintErr($"CombatStatsComponent: Unknown stat '{statName}'");
            return;
        }
        _modifiers[statName].Add(modifier);
        _isDirty = true;
        RecalculateAll();
    }

    public void RemoveModifier(string statName, StatModifier modifier)
    {
        if (!_modifiers.ContainsKey(statName)) return;
        if (_modifiers[statName].Remove(modifier))
        {
            _isDirty = true;
            RecalculateAll();
        }
    }

    public void ClearAllModifiers()
    {
        foreach (var key in _modifiers.Keys)
            _modifiers[key].Clear();
        _isDirty = true;
        RecalculateAll();
    }

    public void RecalculateAll()
    {
        _cachedStrength = CalculateFinalStat("Strength", BaseStrength);
        _cachedDexterity = CalculateFinalStat("Dexterity", BaseDexterity);
        _cachedConstitution = CalculateFinalStat("Constitution", BaseConstitution);
        _cachedIntelligence = CalculateFinalStat("Intelligence", BaseIntelligence);
        _cachedWisdom = CalculateFinalStat("Wisdom", BaseWisdom);
        _cachedCharisma = CalculateFinalStat("Charisma", BaseCharisma);
        _isDirty = false;

        EmitSignal(SignalName.StatChanged, "Strength", _cachedStrength);
        EmitSignal(SignalName.StatChanged, "Dexterity", _cachedDexterity);
        EmitSignal(SignalName.StatChanged, "Constitution", _cachedConstitution);
        EmitSignal(SignalName.StatChanged, "Intelligence", _cachedIntelligence);
        EmitSignal(SignalName.StatChanged, "Wisdom", _cachedWisdom);
        EmitSignal(SignalName.StatChanged, "Charisma", _cachedCharisma);
    }

    private bool EnsureCalculated()
    {
        if (_isDirty) RecalculateAll();
        return true;
    }

    private float CalculateFinalStat(string statName, float baseValue)
    {
        float flatBonus = 0f;
        float percentAddBonus = 0f;
        float percentMultBonus = 1f;

        foreach (var mod in _modifiers[statName])
        {
            switch (mod.Type)
            {
                case StatModifierType.Flat:
                    flatBonus += mod.Value;
                    break;
                case StatModifierType.PercentAdd:
                    percentAddBonus += mod.Value;
                    break;
                case StatModifierType.PercentMult:
                    percentMultBonus *= mod.Value;
                    break;
            }
        }
        return (baseValue + flatBonus) * (1f + percentAddBonus) * percentMultBonus;
    }
}