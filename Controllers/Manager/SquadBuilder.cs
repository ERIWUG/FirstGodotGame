using Godot;
using System.Collections.Generic;
public static class SquadBuilder
{
    public static void SpawnSquad(Node parent, SquadData data)
    {
        for (int i = 0; i < data.Units.Count; i++)
        {
            var entry = data.Units[i];
            if (entry.UnitScene == null) continue;

            var enemy = entry.UnitScene.Instantiate<CharacterBody2D>();
            enemy.Position = entry.Position;
            enemy.AddToGroup("Enemies");
            parent.AddChild(enemy);

            var behavior = enemy.GetNode<EnemyBehavior>("EnemyBehavior");
            string faction = string.IsNullOrEmpty(entry.FactionOverride) ? data.FactionId : entry.FactionOverride;
            behavior.FactionId = faction;
            behavior.UnitNameOverride = NameGenerator.Generate();

            // Определяем, является ли юнит командиром
            bool isCommander = entry.IsCommander || (data.HasCommander && i == data.CommanderIndex);
            if (isCommander)
            {
                var commander = new SquadCommander();
                commander.Name = "SquadCommander";
                commander.SquadName = data.SquadName;
                enemy.AddChild(commander);
            }

           
        }

        // Генерация связей внутри отряда
        var squadMembers = new List<CharacterBody2D>();
        foreach (var node in parent.GetTree().GetNodesInGroup("Enemies"))
        {
            if (node is CharacterBody2D body && body.GetParent() == parent)
                squadMembers.Add(body);
        }

        var rng = new RandomNumberGenerator();
        rng.Randomize();

        // Для каждого юнита создаём до 2 связей с случайными союзниками
        foreach (var body in squadMembers)
        {
            var bondComp = body.GetNodeOrNull<BondComponent>("BondComponent");
            if (bondComp == null) continue;

            for (int i = 0; i < 2; i++)
            {
                if (rng.Randf() < 0.6f) // 60% шанс на связь
                {
                    // Выбираем случайного союзника (не себя)
                    var ally = body;
                    while (ally == body)
                        ally = squadMembers[(int)(rng.Randi() % squadMembers.Count)];

                    string allyName = ally.GetNodeOrNull<EnemyBehavior>("EnemyBehavior")?.DisplayName ?? ally.Name;

                    string bondType;
                    float roll = rng.Randf();
                    if (roll < 0.7f) bondType = "Боевые братья";
                    else if (roll < 0.85f) bondType = "Наставник";
                    else if (roll < 0.95f) bondType = "Должник";
                    else bondType = "Вражда";

                    bondComp.AddBond(allyName, bondType);
                }
            }
        }
    }
}