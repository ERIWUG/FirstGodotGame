using Godot;
using System.Collections.Generic;

public partial class VictoryDefeatManager : Node
{
    [Export] public float VictoryDelay = 10.0f;
    [Export] public float CheckInterval = 5.0f;

    private bool _battleOver = false;
    private float _timer = 0f;
    private float _checkTimer = 0f;
    private string _winnerFaction = "";

    public override void _Ready()
    {
        _checkTimer = CheckInterval - 1.0f;
    }

    public override void _Process(double delta)
    {
        if (_battleOver) return;

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
        foreach (var node in GetTree().GetNodesInGroup("Enemies"))
        {
            var behavior = (node as Node2D)?.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
            if (behavior != null && behavior.CurrentState != AIState.Dead)
                factions.Add(behavior.FactionId);
        }

        // Дополнительно: если у фракции нет живого командира, все её юниты начинают отступать
        foreach (var node in GetTree().GetNodesInGroup("Enemies"))
        {
            var body = node as CharacterBody2D;
            if (body == null) continue;
            var squad = body.GetNodeOrNull<SquadCommander>("SquadCommander");
            if (squad == null || !IsInstanceValid(squad) || squad.CommanderBehavior?.CurrentState == AIState.Dead)
            {
                var behavior = body.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
                if (behavior != null && behavior.CurrentState != AIState.Dead && behavior.CurrentState != AIState.Fleeing)
                    behavior.ExecuteOrder(new OrderData { Type = OrderType.Retreat });
            }
        }

        if (factions.Count <= 1)
        {
            _timer += CheckInterval;
            if (_timer >= VictoryDelay)
            {
                _battleOver = true;
                if (factions.Count == 1)
                {
                    foreach (var f in factions) _winnerFaction = f;
                }
                AnnounceResult();
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
            // Ничья: имя пустое, цвет жёлтый, сообщение "Бой окончен. Ничья."
            SquadJournal.Instance.AddColoredEntry("", Colors.Yellow, "Бой окончен. Ничья.");
        }
        else
        {
            var color = factionColors.ContainsKey(_winnerFaction) ? factionColors[_winnerFaction] : Colors.White;
            // Победа: имя пустое, цвет фракции-победителя, сообщение "Победа фракции X!"
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