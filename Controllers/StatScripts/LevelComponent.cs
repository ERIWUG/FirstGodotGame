using Godot;
using System;

[GlobalClass]
public partial class LevelComponent : Node
{
    // --- Сигналы ---
    [Signal]
    public delegate void ExperienceChangedEventHandler(int currentExp, int expToNextLevel);

    [Signal]
    public delegate void AttributePointsChangedEventHandler(int availablePoints);

    public int AvailableAttributePoints { get; private set; } = 0;

    [Signal]
    public delegate void LevelUpEventHandler(int newLevel);

    // --- Экспортируемые параметры ---
    [Export] public int CurrentLevel { get; private set; } = 1;
    [Export] public int CurrentExperience { get; private set; } = 0;

    // Таблица опыта: пороги для уровней 2, 3, 4...
    [Export] public int[] ExpTable { get; set; } = { 100, 300, 600, 1000, 1500 };

    [Export] public bool UseCustomExpFormula { get; set; } = false;
    [Export] public int BaseExpRequirement { get; set; } = 100;
    [Export] public float ExpMultiplier { get; set; } = 1.5f;

    public override void _Ready()
    {
        // При старте убедимся, что уровень соответствует опыту (если вдруг задали в инспекторе)
        CheckForLevelUp();
    }

    public bool SpendAttributePoint()
    {
        if (AvailableAttributePoints <= 0) return false;
        AvailableAttributePoints--;
        EmitSignal(SignalName.AttributePointsChanged, AvailableAttributePoints);
        return true;
    }

    // Добавить опыт
    public void AddExperience(int amount)
    {
        if (amount <= 0) return;

        CurrentExperience += amount;
        EmitSignal(SignalName.ExperienceChanged, CurrentExperience, GetExpToNextLevel());

        GD.Print($"{Owner.Name} получил {amount} опыта. Всего: {CurrentExperience}");

        CheckForLevelUp();
    }

    

    // Получить опыт до следующего уровня
    public int GetExpToNextLevel()
    {
        int required = GetRequiredExpForLevel(CurrentLevel + 1);
        return Math.Max(0, required - CurrentExperience);
    }

    // Требуемый опыт для указанного уровня (суммарный с начала игры)
    public int GetRequiredExpForLevel(int level)
    {
        if (level <= 1) return 0;

        if (!UseCustomExpFormula && ExpTable != null && level - 2 < ExpTable.Length)
        {
            return ExpTable[level - 2]; // ExpTable[0] = опыт для 2 уровня
        }
        else
        {
            return Mathf.FloorToInt(BaseExpRequirement * Mathf.Pow(ExpMultiplier, level - 2));
        }
    }

    // Установить уровень напрямую (для создания высокоуровневого персонажа)
    public void SetLevel(int level)
    {
        level = Math.Max(1, level);
        CurrentLevel = level;
        CurrentExperience = GetRequiredExpForLevel(level);
        EmitSignal(SignalName.LevelUp, CurrentLevel);
        EmitSignal(SignalName.ExperienceChanged, CurrentExperience, GetExpToNextLevel());
    }

    // Внутренняя проверка повышения уровня (теперь с вычитанием опыта)
    private void CheckForLevelUp()
    {
        bool leveledUp = false;
        while (CurrentExperience >= GetRequiredExpForLevel(CurrentLevel + 1))
        {
            CurrentExperience -= GetRequiredExpForLevel(CurrentLevel + 1);
            CurrentLevel++;
            AvailableAttributePoints += 2; // 2 очка за уровень
            leveledUp = true;
            GD.Print($"{Owner.Name} достиг уровня {CurrentLevel}! +2 очка атрибутов.");
        }

        if (leveledUp)
        {
            EmitSignal(SignalName.LevelUp, CurrentLevel);
            EmitSignal(SignalName.AttributePointsChanged, AvailableAttributePoints);
            EmitSignal(SignalName.ExperienceChanged, CurrentExperience, GetExpToNextLevel());
        }
    }
}