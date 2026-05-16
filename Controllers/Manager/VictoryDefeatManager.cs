using Godot;
using System.Collections.Generic;

public partial class VictoryDefeatManager : Node
{
    [Export] public float VictoryDelay = 10.0f;
    [Export] public float CheckInterval = 2.0f; // Уменьшено до 2 сек для более плавного таймера

    private bool _battleOver = false;
    private float _timer = 0f;
    private float _checkTimer = 0f;
    private string _winnerFaction = "";

    public override void _Process(double delta)
    {
        if (_battleOver) return;

        // 1. Увеличиваем таймер на каждом кадре, если фракция осталась одна
        if (_timer > 0) // Таймер запущен
        {
            _timer += (float)delta;
            if (_timer >= VictoryDelay)
            {
                _battleOver = true;
                AnnounceResult();
                return;
            }
        }

        // 2. Проверяем состояние боя с интервалом CheckInterval
        _checkTimer += (float)delta;
        if (_checkTimer >= CheckInterval)
        {
            _checkTimer = 0f;
            CheckBattleState();
        }
    }

    private void CheckBattleState()
    {
        var factions = new HashSet<string>();
        var commandersAlive = new Dictionary<string, bool>();

        foreach (var node in GetTree().GetNodesInGroup("Enemies"))
        {
            var behavior = (node as Node2D)?.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
            if (behavior == null || behavior.CurrentState == AIState.Dead) continue;

            // Игнорируем перманентно бегущих
            if (behavior.IsPermanentlyFleeing()) continue;

            string faction = behavior.FactionId;
            factions.Add(faction);

            var squad = (node as Node2D)?.GetNodeOrNull<SquadCommander>("SquadCommander");
            if (squad != null && IsInstanceValid(squad) && squad.CommanderBehavior != null &&
                squad.CommanderBehavior.CurrentState != AIState.Dead)
            {
                commandersAlive[faction] = true;
            }
            else
            {
                if (!commandersAlive.ContainsKey(faction))
                    commandersAlive[faction] = false;
            }
        }

        // Принудительное отступление для фракций без командира
        foreach (var node in GetTree().GetNodesInGroup("Enemies"))
        {
            var behavior = (node as Node2D)?.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
            if (behavior == null || behavior.CurrentState == AIState.Dead || behavior.IsPermanentlyFleeing()) continue;

            string faction = behavior.FactionId;
            if (commandersAlive.ContainsKey(faction) && !commandersAlive[faction])
            {
                behavior.SetPermanentRetreat();
                behavior.EnterState(AIState.Fleeing);
            }
        }

        // Таймер победы
        if (factions.Count <= 1)
        {
            if (_timer == 0f)
            {
                _timer = 0.01f;
                if (factions.Count == 1)
                {
                    foreach (var f in factions) _winnerFaction = f;
                }
            }
        }
        else
        {
            _timer = 0f;
        }
    }

    private void AnnounceResult()
    {
        var factionColors = new Dictionary<string, Color>
        {
            {"shadow_cult", Colors.Red},
            {"mercenary", Colors.CornflowerBlue},
            {"neutral", Colors.Gray}
        };

        if (string.IsNullOrEmpty(_winnerFaction))
        {
            SquadJournal.Instance.AddColoredEntry("", Colors.Yellow, "Бой окончен. Ничья.");
        }
        else
        {
            var color = factionColors.ContainsKey(_winnerFaction) ? factionColors[_winnerFaction] : Colors.White;
            SquadJournal.Instance.AddColoredEntry("", color, $"Победа фракции {_winnerFaction}!");
        }

        // Реакции личностей
        foreach (var node in GetTree().GetNodesInGroup("Enemies"))
        {
            var personality = (node as Node2D)?.GetNodeOrNull<PersonalityComponent>("PersonalityComponent");
            if (personality == null) continue;
            var behavior = (node as Node2D)?.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
            if (behavior == null) continue;

            if (behavior.FactionId == _winnerFaction)
                personality.ReactToVictory(_winnerFaction);
            else
                personality.ReactToDefeat(_winnerFaction);
        }
    }
}