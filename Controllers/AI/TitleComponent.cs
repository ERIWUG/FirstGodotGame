using Godot;

[GlobalClass]
public partial class TitleComponent : Node
{
    [Export] public float HealthMultiplier { get; set; } = 1.0f;
    [Export] public float SpeedMultiplier { get; set; } = 1.0f;
    [Export] public float DamageMultiplier { get; set; } = 1.0f;
    [Export] public float SpellPowerMultiplier { get; set; } = 1.0f;
    [Export] public bool ImmuneToFear { get; set; } = false;

    private HealthComponent _health;
    private MovementController _movement;
    private CombatController _combat;
    private SpellController _spell;
    private EnemyBehavior _behavior;

    public override void _Ready()
    {
        _behavior = GetParent().GetNode<EnemyBehavior>("EnemyBehavior");
        _health = GetParent().GetNode<HealthComponent>("HealthComponent");
        _movement = GetParent().GetNodeOrNull<MovementController>("MovementController");
        _combat = GetParent().GetNodeOrNull<CombatController>("CombatController");
        _spell = GetParent().GetNodeOrNull<SpellController>("SpellController");

        // Получаем имя юнита (уже сгенерированное SquadBuilder)
        string unitName = _behavior?.DisplayName ?? GetParent().Name;
        ApplyTitleBonuses(unitName);
    }

    private void ApplyTitleBonuses(string unitName)
    {
        // Разбиваем имя на слова (например, "Грон Железный" → ["Грон", "Железный"])
        var words = unitName.Split(' ');

        // Проверяем последние слова имени (титул всегда в конце)
        foreach (var word in words)
        {
            switch (word)
            {
                // Обычные титулы
                case "Железный":
                    HealthMultiplier = 1.2f;
                    break;
                case "Стремительный":
                    SpeedMultiplier = 1.3f;
                    break;
                case "Яростный":
                    DamageMultiplier = 1.15f;
                    break;
                case "Мудрый":
                    SpellPowerMultiplier = 1.2f;
                    break;
                case "Стальной":
                    HealthMultiplier = 1.1f;
                    DamageMultiplier = 1.05f;
                    break;
                case "Теневой":
                    SpeedMultiplier = 1.15f;
                    break;
                case "Кровавый":
                    DamageMultiplier = 1.1f;
                    break;
                case "Безжалостный":
                    DamageMultiplier = 1.2f;
                    break;
                case "Отважный":
                    ImmuneToFear = true;
                    break;
                case "Непоколебимый":
                    HealthMultiplier = 1.15f;
                    ImmuneToFear = true;
                    break;
                case "Пылкий":
                    DamageMultiplier = 1.05f;
                    SpellPowerMultiplier = 1.1f;
                    break;
                case "Шепчущий":
                    SpeedMultiplier = 1.1f;
                    SpellPowerMultiplier = 1.1f;
                    break;
                case "Хитрый":
                    SpeedMultiplier = 1.2f;
                    break;
                case "Мрачный":
                    DamageMultiplier = 1.05f;
                    SpellPowerMultiplier = 1.15f;
                    break;

                // Редкие титулы (мощные бонусы)
                case "Погибель":
                    if (unitName.Contains("Драконов")) // "Погибель Драконов"
                    {
                        DamageMultiplier = 1.5f;
                        ImmuneToFear = true;
                    }
                    break;
                case "Повелитель":
                    if (unitName.Contains("Бурь"))
                    {
                        SpellPowerMultiplier = 1.5f;
                        SpeedMultiplier = 1.1f;
                    }
                    break;
                case "Страж":
                    if (unitName.Contains("Вечности"))
                    {
                        HealthMultiplier = 1.5f;
                        ImmuneToFear = true;
                    }
                    break;
                case "Тень":
                    if (unitName.Contains("Королей"))
                    {
                        SpeedMultiplier = 1.3f;
                        DamageMultiplier = 1.3f;
                    }
                    break;
                case "Охотник":
                    if (unitName.Contains("Магов"))
                    {
                        SpellPowerMultiplier = 1.4f;
                        DamageMultiplier = 1.2f;
                    }
                    break;
                case "Разрушитель":
                    if (unitName.Contains("Орд"))
                    {
                        DamageMultiplier = 1.4f;
                        HealthMultiplier = 1.2f;
                    }
                    break;
                case "Защитник":
                    if (unitName.Contains("Слабых"))
                    {
                        HealthMultiplier = 1.4f;
                        ImmuneToFear = true;
                    }
                    break;
                case "Мастер":
                    if (unitName.Contains("Клинка"))
                    {
                        DamageMultiplier = 1.5f;
                        SpeedMultiplier = 1.2f;
                    }
                    break;
                case "Хранитель":
                    if (unitName.Contains("Тайн"))
                    {
                        SpellPowerMultiplier = 1.5f;
                        ImmuneToFear = true;
                    }
                    break;
                case "Архитектор":
                    if (unitName.Contains("Судеб"))
                    {
                        HealthMultiplier = 1.3f;
                        SpellPowerMultiplier = 1.3f;
                        DamageMultiplier = 1.3f;
                    }
                    break;
            }
        }

        // Применяем бонусы к компонентам
        ApplyBonuses();
    }

    private void ApplyBonuses()
    {
        // Здоровье
        if (_health != null && HealthMultiplier != 1.0f)
        {
            float newMax = _health.MaxHealth * HealthMultiplier;
            _health.MaxHealth = newMax;
            _health.SetHealth(newMax); // обновляем текущее здоровье
        }

        // Скорость
        if (_movement != null && SpeedMultiplier != 1.0f)
            _movement.Speed *= SpeedMultiplier;

        // Урон в ближнем бою
        if (_combat != null && DamageMultiplier != 1.0f)
            _combat.Damage *= DamageMultiplier;

        // Магическая сила (через SpellController или SkillComponent)
        if (_spell != null && SpellPowerMultiplier != 1.0f)
        {
            // SpellController должен иметь поле DamageMultiplier
            _spell.DamageMultiplier *= SpellPowerMultiplier;
        }

        // Иммунитет к страху
        if (ImmuneToFear && _behavior != null)
        {
            _behavior.FearResistance = 1000f; // практически не чувствует страх
            _behavior.Courage = 100f;         // всегда готов к бою
        }
    }
}