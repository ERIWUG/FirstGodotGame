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
    private RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Randomize();
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
        IsBrave = _rng.Randf() < 0.6f;
        IsCynical = _rng.Randf() < 0.3f;
        IsLoyal = _rng.Randf() < 0.8f;
        IsAggressive = _rng.Randf() < 0.4f;
    }

    private bool _firstBlood = false;
    private void OnDamaged(float amount, float currentHealth)
    {
        if (!_firstBlood && currentHealth < _health.MaxHealth * 0.5f)
        {
            _firstBlood = true;
            string phrase = IsBrave ? PickRandom(_braveWound) :
                            IsCynical ? PickRandom(_cynicalWound) :
                            PickRandom(_neutralWound);
            JournalColored(phrase);
        }
    }

    private bool _alreadyDead = false;
    private void OnDeath()
    {
        if (_alreadyDead) return;
        _alreadyDead = true;

        string phrase;
        if (IsBrave && IsLoyal)
            phrase = PickRandom(_braveLoyalDeath);
        else if (IsCynical)
            phrase = PickRandom(_cynicalDeath);
        else if (IsAggressive)
            phrase = PickRandom(_aggressiveDeath);
        else
            phrase = PickRandom(_neutralDeath);

        JournalColored(phrase);
        // Оповещаем связанных союзников
        var commander = _behavior?.Commander;
        if (commander != null)
        {
            string myName = _behavior?.DisplayName ?? GetParent().Name;
            foreach (var member in commander.GetMembers())
            {
                if (member == _behavior) continue;
                var bondComp = member.GetParent<CharacterBody2D>()?.GetNodeOrNull<BondComponent>("BondComponent");
                if (bondComp != null)
                    bondComp.OnAllyDeath(myName);
            }
        }
    }

    public void ReactToRetreat()
    {
        string phrase = IsLoyal ? PickRandom(_loyalRetreat) :
                        IsCynical ? PickRandom(_cynicalRetreat) :
                        PickRandom(_neutralRetreat);
        JournalColored(phrase);
    }

    public void ReactToVictory(string winnerFaction)
    {
        string phrase = IsBrave ? PickRandom(_braveVictory) :
                        IsAggressive ? PickRandom(_aggressiveVictory) :
                        PickRandom(_neutralVictory);
        JournalColored(phrase);
    }

    public void ReactToDefeat(string winnerFaction)
    {
        string phrase = IsLoyal ? PickRandom(_loyalDefeat) :
                        IsCynical ? PickRandom(_cynicalDefeat) :
                        IsAggressive ? PickRandom(_aggressiveDefeat) :
                        PickRandom(_neutralDefeat);
        JournalColored(phrase);
    }

    private string PickRandom(string[] pool) => pool[_rng.Randi() % pool.Length];

    public void JournalColored(string message)
    {
        Color color = Colors.Gray;
        if (_behavior != null)
            color = FactionManager.GetColor(_behavior.FactionId);
        string name = _behavior?.DisplayName ?? GetParent().Name;
        SquadJournal.Instance?.AddColoredEntry(name, color, message);
    }

    // Пулы фраз (без имён!)
    private readonly string[] _braveWound = {
        "«Рана? Просто царапина! Я в порядке.»",
        "«Это всего лишь царапина. Я выдержу.»",
        "«Не обращайте внимания, продолжаем бой!»"
    };
    private readonly string[] _cynicalWound = {
        "«Ну вот, и меня зацепили. Надеюсь, оно того стоило.»",
        "«Как предсказуемо. И почему я не удивлён?»",
        "«Плата за глупость, не иначе.»"
    };
    private readonly string[] _neutralWound = {
        "«Я ранен! Нужна помощь целителя!»",
        "«Ауч! Кто-нибудь, подлатайте меня!»",
        "«Зацепили... Надеюсь, живучий.»"
    };

    private readonly string[] _braveLoyalDeath = {
        "«За командира!» — были последние слова.",
        "«Слава отряду!» — выкрикнул напоследок.",
        "«Я сделал всё, что мог...» — прошептал."
    };
    private readonly string[] _cynicalDeath = {
        "«Вот и всё... Я знал, что этим кончится.» — пробормотал напоследок.",
        "«Тупая, бессмысленная смерть.» — последние слова.",
        "«Как иронично...» — усмехнулся в последний раз."
    };
    private readonly string[] _aggressiveDeath = {
        "«Я забираю тебя с собой!» — прорычал.",
        "«Умри, сволочь!» — крикнул перед смертью.",
        "«Я вернусь!» — прохрипел."
    };
    private readonly string[] _neutralDeath = {
        "Молча пал в бою.",
        "Упал без единого звука.",
        "Тихо испустил последний вздох."
    };

    private readonly string[] _loyalRetreat = {
        "«Приказ есть приказ. Отступаем.»",
        "«Отходим! Командир, мы с тобой!»",
        "«Так надо. Мы ещё вернёмся.»"
    };
    private readonly string[] _cynicalRetreat = {
        "«Я так и знал. Бежим!»",
        "«Всё как всегда. Пора сматываться.»",
        "«Очередной провал. Не удивительно.»"
    };
    private readonly string[] _neutralRetreat = {
        "«Спасайся кто может!» — закричал.",
        "«Отступаем! Быстрее!»",
        "«Назад! Нас перебьют!»"
    };

    private readonly string[] _braveVictory = {
        "«Мы сделали это! Победа!»",
        "«Великий день! Мы непобедимы!»",
        "«За нами слава!»"
    };
    private readonly string[] _aggressiveVictory = {
        "«Я хочу ещё крови!» — рычит.",
        "«Мало! Дайте мне ещё врагов!»",
        "«Это было весело. Кто следующий?»"
    };
    private readonly string[] _neutralVictory = {
        "«Отличная работа.» — спокойно говорит.",
        "«Неплохо. Заслужили отдых.»",
        "«Мы победили. Так держать.»"
    };

    private readonly string[] _loyalDefeat = {
        "«Мы подвели командира...»",
        "«Я должен был сражаться лучше.»",
        "«Простите, командир...»"
    };
    private readonly string[] _cynicalDefeat = {
        "«Как я и предсказывал — мы проиграли.»",
        "«Ничего другого я и не ожидал.»",
        "«Закономерный итог.»"
    };
    private readonly string[] _aggressiveDefeat = {
        "«Это ещё не конец! Я вернусь!»",
        "«Они заплатят за это!»",
        "«Повезло... в этот раз.»"
    };
    private readonly string[] _neutralDefeat = {
        "«Всё кончено...»",
        "«Мы проиграли.»",
        "«Печальный день.»"
    };
}