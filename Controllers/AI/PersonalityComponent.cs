using Godot;

[GlobalClass]
public partial class PersonalityComponent : Node
{
    [Export] public bool RandomizeTraits { get; set; } = true;
    [Export] public bool IsBrave { get; set; } = false;
    [Export] public bool IsCynical { get; set; } = false;
    [Export] public bool IsLoyal { get; set; } = false;
    [Export] public bool IsAggressive { get; set; } = false;

    private HealthComponent _health;
    private EnemyBehavior _behavior;

    public override void _Ready()
    {
        if (RandomizeTraits)
            GenerateRandomTraits();

        _health = GetParent().GetNode<HealthComponent>("HealthComponent");
        _behavior = GetParent().GetNode<EnemyBehavior>("EnemyBehavior");

        if (_health != null)
        {
            _health.HealthDepleted += OnDeath;
            _health.Damaged += OnDamaged;
        }
    }

    private void GenerateRandomTraits()
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        IsBrave = rng.Randf() < 0.6f;
        IsCynical = rng.Randf() < 0.3f;
        IsLoyal = rng.Randf() < 0.8f;
        IsAggressive = rng.Randf() < 0.4f;
    }

    private bool _firstBlood = false;
    private void OnDamaged(float amount, float currentHealth)
    {
        if (!_firstBlood && currentHealth < _health.MaxHealth * 0.5f)
        {
            _firstBlood = true;
            string name = _behavior?.DisplayName ?? GetParent().Name;
            if (IsBrave)
                JournalColored(name, "«Рана? Просто царапина! Я в порядке.»");
            else if (IsCynical)
                JournalColored(name, "«Ну вот, и меня зацепили. Надеюсь, оно того стоило.»");
            else
                JournalColored(name, "«Я ранен! Нужна помощь целителя!»");
        }
    }

    private bool _alreadyDead = false;
    private void OnDeath()
    {
        if (_alreadyDead) return;
        _alreadyDead = true;

        string name = _behavior?.DisplayName ?? GetParent().Name;
        string phrase;
        if (IsBrave && IsLoyal)
            phrase = $"«За командира!» — были последние слова {name}.";
        else if (IsCynical)
            phrase = $"«Вот и всё... Я знал, что этим кончится.» — пробормотал {name} напоследок.";
        else if (IsAggressive)
            phrase = $"«Я забираю тебя с собой!» — прорычал {name}.";
        else
            phrase = $"{name} молча пал в бою.";

        JournalColored(name, phrase);
    }

    public void ReactToRetreat()
    {
        string name = _behavior?.DisplayName ?? GetParent().Name;
        if (IsLoyal)
            JournalColored(name, "«Приказ есть приказ. Отступаем.»");
        else if (IsCynical)
            JournalColored(name, "«Я так и знал. Бежим!»");
        else
            JournalColored(name, $"«Спасайся кто может!» — закричал {name}.");
    }

    public void ReactToVictory(string winnerFaction)
    {
        string name = _behavior?.DisplayName ?? GetParent().Name;
        if (IsBrave)
            JournalColored(name, $"«Мы сделали это! {winnerFaction} победили!»");
        else if (IsAggressive)
            JournalColored(name, $"«Я хочу ещё крови!» — рычит {name}.");
        else
            JournalColored(name, $"«Отличная работа.» — спокойно говорит {name}.");
    }

    public void ReactToDefeat(string winnerFaction)
    {
        string name = _behavior?.DisplayName ?? GetParent().Name;
        if (IsLoyal)
            JournalColored(name, "«Мы подвели командира...»");
        else if (IsCynical)
            JournalColored(name, "«Как я и предсказывал — мы проиграли.»");
        else if (IsAggressive)
            JournalColored(name, "«Это ещё не конец! Я вернусь!»");
        else
            JournalColored(name, "«Всё кончено...»");
    }

    private void JournalColored(string speakerName, string message)
    {
        Color color = Colors.Gray;
        if (_behavior != null)
            color = FactionManager.GetColor(_behavior.FactionId);
        SquadJournal.Instance?.AddColoredEntry(speakerName, color, message);
    }
}