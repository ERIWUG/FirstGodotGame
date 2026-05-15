using Godot;

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
    }
}