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
                case "healer":
                    LoadHealerSpells();
                    break;
                case "necromancer":
                    LoadNecromancerSpells();
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

        // --- Рыбное превращение ---
        var iceElement = GD.Load<ModifierData>("res://Resources/Modifiers/ice_element.tres");
        var fishTransformEffect = GD.Load<ModifierData>("res://Resources/Modifiers/fish_transform_effect.tres");
        if (iceElement != null && fishTransformEffect != null)
        {
            var fishSpell = new SpellData();
            fishSpell.Modifiers = new List<ModifierData> {  iceElement, fishTransformEffect, longCast };
            fishSpell.RecalculateStats();
            fishSpell.SpellName = "Превращение в рыбу";
            _skill.LearnSpell(fishSpell);
        }
    }

    private void LoadFireSpecialistSpells()
    {
        // Пример другого набора – можно реализовать позже
    }

   private void LoadHealerSpells()
    {
        var projectile = GD.Load<ModifierData>("res://Resources/Modifiers/projectyle_form.tres");
        var heal = GD.Load<ModifierData>("res://Resources/Modifiers/heal_element.tres");
        var regen = GD.Load<ModifierData>("res://Resources/Modifiers/regen_effect.tres");
        var longCast = GD.Load<ModifierData>("res://Resources/Modifiers/long_cast.tres");

        if (projectile == null || heal == null || regen == null || longCast == null)
        {
            GD.PrintErr("EnemySkillManager: Не найдены модификаторы для пресета healer");
            return;
        }

        // Исцеляющий луч
        var healBeam = new SpellData();
        healBeam.Modifiers = new List<ModifierData> { projectile, heal,longCast };
        healBeam.RecalculateStats();
        healBeam.SpellName = "Исцеляющий луч";
        _skill.LearnSpell(healBeam);

        // Благословение жизни (регенерация)
        var blessing = new SpellData();
        blessing.Modifiers = new List<ModifierData> { projectile, heal, regen, longCast };
        blessing.RecalculateStats();
        blessing.SpellName = "Благословение жизни";
        _skill.LearnSpell(blessing);
    }

    private void LoadNecromancerSpells()
{
    var projectile = GD.Load<ModifierData>("res://Resources/Modifiers/projectyle_form.tres");
    var darkness = GD.Load<ModifierData>("res://Resources/Modifiers/darkness_element.tres")
                ?? GD.Load<ModifierData>("res://Resources/Modifiers/fire_element.tres");
    var raiseDeadEffect = GD.Load<ModifierData>("res://Resources/Modifiers/raise_dead_effect.tres");
    var longCast = GD.Load<ModifierData>("res://Resources/Modifiers/long_cast.tres");

    if (projectile == null || darkness == null || raiseDeadEffect == null || longCast == null)
    {
        GD.PrintErr("EnemySkillManager: Не найдены модификаторы для пресета necromancer");
        return;
    }

    // Тёмный разряд (базовый атакующий)
    var spell1 = new SpellData();
    spell1.Modifiers = new List<ModifierData> { projectile, darkness, longCast };
    spell1.RecalculateStats();
    spell1.SpellName = "Тёмный разряд";
    _skill.LearnSpell(spell1);

    // Поднять скелета
    var raiseSpell = new SpellData();
    raiseSpell.Modifiers = new List<ModifierData> { projectile, raiseDeadEffect, longCast };
    raiseSpell.RecalculateStats();
    raiseSpell.SpellName = "Поднять скелета";
    _skill.LearnSpell(raiseSpell);
}
}