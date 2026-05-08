using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class EnemySkillManager : Node
{
    [Export] public string[] SpellPresets = { "shadow_mage" }; // какие наборы заклинаний дать

    private SkillComponent _skill;
    private ResourceComponent _resources;

    public override void _Ready()
    {
        _skill = GetNodeOrNull<SkillComponent>("../SkillComponent");
        _resources = GetNodeOrNull<ResourceComponent>("../ResourceComponent");

        if (_skill != null && _resources != null)
        {
            LearnSpellsForPresets();
        }
    }

    private void LearnSpellsForPresets()
    {
        foreach (var preset in SpellPresets)
        {
            switch (preset)
            {
                case "shadow_mage":
                    LoadShadowMageSpells();
                    break;
                case "fire_specialist":
                    LoadFireSpecialistSpells();
                    break;
                // можно добавить другие пресеты
            }
        }
    }

    private void LoadShadowMageSpells()
    {
        var projectile = GD.Load<ModifierData>("res://Resources/Modifiers/projectyle_form.tres");
        var darkness = GD.Load<ModifierData>("res://Resources/Modifiers/darkness_element.tres")
                    ?? GD.Load<ModifierData>("res://Resources/Modifiers/fire_element.tres");
        var longCast = GD.Load<ModifierData>("res://Resources/Modifiers/long_cast.tres");
        var fearEffect = GD.Load<ModifierData>("res://Resources/Modifiers/fear_effect.tres");
        var explosionForm = GD.Load<ModifierData>("res://Resources/Modifiers/explosion_form.tres");
        var fireElement = GD.Load<ModifierData>("res://Resources/Modifiers/fire_element.tres");
        var darkPact = GD.Load<ModifierData>("res://Resources/Modifiers/dark_pact.tres");

        if (projectile == null || longCast == null || fearEffect == null || explosionForm == null || fireElement == null || darkPact == null)
        {
            GD.PrintErr($"EnemySkillManager: Не найдены модификаторы для пресета shadow_mage");
            return;
        }

        // Тёмный разряд
        var spell1 = new SpellData();
        spell1.Modifiers = new List<ModifierData> { projectile, darkness, longCast };
        spell1.RecalculateStats();
        spell1.SpellName = "Тёмный разряд";
        _skill.LearnSpell(spell1);

        // Сфера Страха
        var spell2 = new SpellData();
        spell2.Modifiers = new List<ModifierData> { projectile, fearEffect, longCast };
        spell2.RecalculateStats();
        spell2.SpellName = "Сфера Страха";
        _skill.LearnSpell(spell2);

        // Взрыв Хаоса
        var spell3 = new SpellData();
        spell3.Modifiers = new List<ModifierData> { explosionForm, fireElement, longCast };
        spell3.RecalculateStats();
        spell3.SpellName = "Взрыв Хаоса";
        _skill.LearnSpell(spell3);

        // Тёмный Пакт
        var spell4 = new SpellData();
        spell4.Modifiers = new List<ModifierData> { darkPact, longCast };
        spell4.RecalculateStats();
        spell4.SpellName = "Тёмный Пакт";
        _skill.LearnSpell(spell4);
    }

    private void LoadFireSpecialistSpells()
    {
        // Пример другого набора – можно реализовать позже
    }
}