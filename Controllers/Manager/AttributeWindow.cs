using Godot;
using System.Collections.Generic;
using static Godot.Control;

public partial class AttributeWindow : PopupPanel
{
    private LevelComponent _level;
    private CombatStatsComponent _stats;
    private HealthComponent _health; // для отображения бонусов к HP
    private ResourceComponent _resources; // для бонусов к MP

    private VBoxContainer _mainContainer;
    private Label _pointsLabel;
    private VBoxContainer _statsContainer;

    // Замыкания для увеличения статов
    private Dictionary<string, System.Action> _increaseActions = new();

    public override void _Ready()
    {
        // Строим интерфейс программно
        _mainContainer = new VBoxContainer();
        _mainContainer.SetAnchorsPreset(LayoutPreset.FullRect);
        _mainContainer.AddThemeConstantOverride("separation", 10);
        AddChild(_mainContainer);

        // Заголовок
        var titleLabel = new Label
        {
            Text = "Распределение очков",
            HorizontalAlignment = HorizontalAlignment.Center,
            ThemeTypeVariation = "HeaderLarge" // если есть тема, иначе просто крупный шрифт
        };
        _mainContainer.AddChild(titleLabel);

        // Очки
        _pointsLabel = new Label
        {
            Text = "Очков доступно: 0",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _mainContainer.AddChild(_pointsLabel);

        // Контейнер для атрибутов
        _statsContainer = new VBoxContainer();
        _statsContainer.AddThemeConstantOverride("separation", 5);
        _mainContainer.AddChild(_statsContainer);

        // Кнопка закрытия
        var closeButton = new Button
        {
            Text = "Готово",
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
        };
        closeButton.Pressed += OnClosePressed;
        _mainContainer.AddChild(closeButton);

        // Минимальный размер окна
        
    }

    public void Initialize(LevelComponent level, CombatStatsComponent stats, HealthComponent health, ResourceComponent resources)
    {
        _level = level;
        _stats = stats;
        _health = health;
        _resources = resources;

        BuildStatRows();
        UpdateUI();
    }

    private void BuildStatRows()
    {
        // Очищаем старые строки
        foreach (var child in _statsContainer.GetChildren())
            child.QueueFree();

        _increaseActions.Clear();

        // Добавляем строку для каждого атрибута
        AddStatRow("Сила", "Strength", "Атака ближнего боя");
        AddStatRow("Ловкость", "Dexterity", "Уклонение / Крит");
        AddStatRow("Телосложение", "Constitution", "Макс. здоровье");
        AddStatRow("Интеллект", "Intelligence", "Макс. мана / Маг. атака");
        AddStatRow("Мудрость", "Wisdom", "Маг. защита");
        AddStatRow("Харизма", "Charisma", "Скидки / Шанс убеждения");
    }

    private void AddStatRow(string name, string statName, string bonusDescription)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);

        // Название
        var nameLabel = new Label { Text = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(nameLabel);

        
       
        // Исправленный вариант:
        var valueLabel = new Label { Text = "0", HorizontalAlignment = HorizontalAlignment.Right };
        valueLabel.CustomMinimumSize = new Vector2(30, 0);
        row.AddChild(valueLabel);

        // Кнопка "+"
        var btn = new Button { Text = "+", CustomMinimumSize = new Vector2(30, 30) };
        btn.Pressed += () =>
        {
            if (_level.SpendAttributePoint())
            {
                IncreaseStat(statName);
                UpdateUI();
            }
        };
        row.AddChild(btn);

        // Бонус
        var bonusLabel = new Label { Text = bonusDescription, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddChild(bonusLabel);

        _statsContainer.AddChild(row);

        // Сохраняем действие увеличения стата
        _increaseActions[statName] = () =>
        {
            switch (statName)
            {
                case "Strength": _stats.BaseStrength += 1; break;
                case "Dexterity": _stats.BaseDexterity += 1; break;
                case "Constitution": _stats.BaseConstitution += 1; break;
                case "Intelligence": _stats.BaseIntelligence += 1; break;
                case "Wisdom": _stats.BaseWisdom += 1; break;
                case "Charisma": _stats.BaseCharisma += 1; break;
            }
            _stats.RecalculateAll(); // обновит кэш и сигналы
        };
    }

    private void IncreaseStat(string statName)
    {
        if (_increaseActions.TryGetValue(statName, out var action))
            action();
    }

    public void UpdateUI()
    {
        _pointsLabel.Text = $"Очков доступно: {_level.AvailableAttributePoints}";

        // Обновим значения в строках
        int index = 0;
        foreach (var row in _statsContainer.GetChildren())
        {
            if (row is HBoxContainer hbox)
            {
                // valueLabel — второй элемент (индекс 1)
                var valueLabel = hbox.GetChild<Label>(1);
                // bonusLabel — четвёртый (индекс 3)
                var bonusLabel = hbox.GetChild<Label>(3);

                // Достаём имя стата по порядку (можно сохранить ссылки при создании, но для простоты используем фиксированный порядок)
                string[] statOrder = { "Strength", "Dexterity", "Constitution", "Intelligence", "Wisdom", "Charisma" };
                if (index < statOrder.Length)
                {
                    string stat = statOrder[index];
                    float val = GetStatValue(stat);
                    valueLabel.Text = val.ToString("F0");
                    bonusLabel.Text = GetBonusText(stat, val);
                }
                index++;
            }
        }
    }

    private float GetStatValue(string statName) => statName switch
    {
        "Strength" => _stats.Strength,
        "Dexterity" => _stats.Dexterity,
        "Constitution" => _stats.Constitution,
        "Intelligence" => _stats.Intelligence,
        "Wisdom" => _stats.Wisdom,
        "Charisma" => _stats.Charisma,
        _ => 0
    };

    private string GetBonusText(string statName, float value)
    {
        int bonus = Mathf.FloorToInt((value - 10) / 2);
        return statName switch
        {
            "Strength" => $"Атака: +{bonus}",
            "Dexterity" => $"Инициатива: +{bonus}",
            "Constitution" => $"HP: +{bonus * 2}",
            "Intelligence" => $"MP: +{bonus * 2}",
            "Wisdom" => $"Спасбросок: +{bonus}",
            "Charisma" => $"Скидка: -{bonus}%",
            _ => ""
        };
    }

    private void OnClosePressed()
    {
        Hide();
        GetTree().Paused = false;
    }
}