using Godot;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
public partial class SpellCrafterComponent : Node
{
    [Signal]
    public delegate void SpellCraftedEventHandler(SpellData spell);

    [Signal]
    public delegate void CraftingFailedEventHandler(string reason);

    private ManaCoreComponent _coreComponent;
    private List<ModifierData> _currentModifiers = new();
    private int _maxSlots;

    public override void _Ready()
    {
        _coreComponent = GetNode<ManaCoreComponent>("../ManaCoreComponent");
        UpdateMaxSlots();
    }

    // Обновить количество доступных слотов на основе ядер
    public void UpdateMaxSlots()
    {
        _maxSlots = 0;
        foreach (var core in _coreComponent.GetAllCores())
        {
            if (!core.IsShattered)
                _maxSlots += core.GetSlotCount();
        }
    }

    // Попытаться добавить модификатор в текущее заклинание
    public bool TryAddModifier(ModifierData mod)
    {
        // Проверка слота
        if (_currentModifiers.Count >= _maxSlots)
        {
            EmitSignal(SignalName.CraftingFailed, "Нет свободных слотов!");
            return false;
        }

        // Проверка наличия нужного ядра и его ранга
        bool hasRequiredCore = _coreComponent.GetAllCores().Any(c =>
            c.Element == mod.RequiredElement && !c.IsShattered && (int)c.Rank >= mod.RequiredCoreRank);

        if (!hasRequiredCore)
        {
            EmitSignal(SignalName.CraftingFailed, $"Требуется ядро {mod.RequiredElement} ранга {mod.RequiredCoreRank}!");
            return false;
        }

        _currentModifiers.Add(mod);
        return true;
    }

    // Удалить модификатор
    public void RemoveModifier(int index)
    {
        if (index >= 0 && index < _currentModifiers.Count)
            _currentModifiers.RemoveAt(index);
    }

    // Очистить текущую сборку
    public void ClearCurrent() => _currentModifiers.Clear();

    // Создать SpellData из текущего набора модификаторов
    public SpellData CraftSpell(string customName = "")
    {
        if (_currentModifiers.Count == 0)
        {
            EmitSignal(SignalName.CraftingFailed, "Нет модификаторов!");
            return null;
        }

        var spell = new SpellData();
        spell.Modifiers = new List<ModifierData>(_currentModifiers);
        spell.RecalculateStats();
        spell.SpellName = string.IsNullOrEmpty(customName) ? GenerateSpellName() : customName;

        EmitSignal(SignalName.SpellCrafted, spell);
        return spell;
    }

    private string GenerateSpellName()
    {
        // Простой генератор имени: "Огненный Снаряд", "Ледяной Луч" и т.д.
        string form = "", element = "";
        foreach (var mod in _currentModifiers)
        {
            if (mod.Type == ModifierType.Form) form = mod.Name;
            if (mod.Type == ModifierType.Element) element = mod.Name;
        }
        return $"{element} {form}".Trim();
    }

    // Для UI: получить список всех доступных модификаторов (из базы данных)
    public List<ModifierData> GetAvailableModifiers()
    {
        // Здесь должен быть доступ к глобальной базе модификаторов.
        // Можно загружать из папки "res://Modifiers/" или хранить в синглтоне.
        // Пока вернём заглушку.
        return new List<ModifierData>();
    }

    public int GetCurrentSlotCount() => _currentModifiers.Count;
    public int GetMaxSlots() => _maxSlots;
}