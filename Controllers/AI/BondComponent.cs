using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class BondComponent : Node
{
    // Ключ – имя союзника, значение – тип связи
    private Dictionary<string, string> _bonds = new();

    private EnemyBehavior _behavior;
    private PersonalityComponent _personality;

    public override void _Ready()
    {
        _behavior = GetParent().GetNode<EnemyBehavior>("EnemyBehavior");
        _personality = GetParent().GetNodeOrNull<PersonalityComponent>("PersonalityComponent");
    }

    // Добавить связь с указанным союзником
    public void AddBond(string allyName, string bondType)
    {
        if (!_bonds.ContainsKey(allyName))
            _bonds[allyName] = bondType;
    }

    // Вызывается, когда умирает союзник с именем allyName
    public void OnAllyDeath(string allyName)
    {
        if (!_bonds.TryGetValue(allyName, out string bondType))
            return;

        string phrase = bondType switch
        {
            "Боевые братья" => PickRandom(new[] {
                $"«{allyName}! Я отомщу за тебя!»",
                $"«Они заплатят за {allyName}!»",
                $"«Брат! Я не прощу им это!»"
            }),
            "Вражда" => PickRandom(new[] {
                $"«{allyName} получил по заслугам.»",
                $"«Наконец-то {allyName} заткнулся.»",
                $"«Я же говорил, что {allyName} плохо кончит.»"
            }),
            "Наставник" => PickRandom(new[] {
                $"«Я не забуду твоих уроков, {allyName}...»",
                $"«Учитель... Я продолжу твой путь.»"
            }),
            "Должник" => PickRandom(new[] {
                $"«Я так и не успел отдать тебе должок, {allyName}...»",
                $"«Прости, {allyName}, что не уберёг.»"
            }),
            _ => $"«Прощай, {allyName}...»"
        };

        // Отправляем реплику в журнал (цвета и имя добавит сам журнал)
        if (_personality != null)
            _personality.JournalColored(phrase);
    }

    // Возвращает случайную строку из массива
    private string PickRandom(string[] options)
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        return options[rng.Randi() % options.Length];
    }
}