using Godot;
using System.Linq;

public partial class SquadTestSetup : Node2D
{
    private Button _startButton;

    public override void _Ready()
    {
        // Камера для удобства
        var camera = new Camera2D { Position = new Vector2(600, 350), Zoom = new Vector2(0.8f, 0.8f) };
        AddChild(camera);
        camera.MakeCurrent();

        // Стены, чтобы враги не разбегались
        CreateWall(new Vector2(-10, 0), new Vector2(10, 800));   // левая
        CreateWall(new Vector2(1210, 0), new Vector2(10, 800));  // правая
        CreateWall(new Vector2(0, -10), new Vector2(1220, 10));  // верхняя
        CreateWall(new Vector2(0, 810), new Vector2(1220, 10));  // нижняя

        // UI
        var canvasLayer = new CanvasLayer();
        canvasLayer.Name = "CanvasLayer";
        AddChild(canvasLayer);
        _startButton = new Button { Text = "Начать бой", Position = new Vector2(10, 10) };
        _startButton.Pressed += OnStartBattle;
        canvasLayer.AddChild(_startButton);

        var battleLogScene = GD.Load<PackedScene>("res://Scenes/UIBricks/BattleLog.tscn");
        var battleLog = battleLogScene.Instantiate<BattleLog>();
        canvasLayer.AddChild(battleLog);
        battleLog.Position = new Vector2(900, 10); // пример позиции, можно настроить в сцене
       

        var squadA = GD.Load<SquadData>("res://Resources/Squads/SquadA.tres");
        var squadB = GD.Load<SquadData>("res://Resources/Squads/SquadB.tres");

        // Спавним отряды
        SquadBuilder.SpawnSquad(this, squadA);
        SquadBuilder.SpawnSquad(this, squadB);

        // Собираем отряды для командиров
        foreach (var enemy in GetTree().GetNodesInGroup("Enemies"))
        {
            var commander = enemy.GetNodeOrNull<SquadCommander>("SquadCommander");
            commander?.GatherSquad();
        }

        var victoryManager = new VictoryDefeatManager();
        AddChild(victoryManager);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_journal"))
        {
            GD.Print("[DEBUG] Клавиша J нажата!");
            
            var journal = GetNodeOrNull<BattleLog>("CanvasLayer/BattleLog");
            if (journal == null)
            {
                GD.PrintErr("[DEBUG] Узел BattleLog НЕ НАЙДЕН в пути CanvasLayer/BattleLog");
                return;
            }
            GD.Print("[DEBUG] Узел BattleLog найден, вызываю ToggleVisible...");
            journal.ToggleVisible();
        }
    }

    private Node2D SpawnEnemy(string path, Vector2 pos, string faction, bool isCommander = false)
    {
        var packed = GD.Load<PackedScene>(path);
        var enemy = packed.Instantiate<CharacterBody2D>();

        // Присваиваем имя на основе файла сцены (без расширения)
        string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
        enemy.Name = sceneName;

        var health = enemy.GetNode<HealthComponent>("HealthComponent");
        health.HealthDepleted += () =>
        {
            // Чистим отряды всех командиров, чтобы они забыли умершего
            foreach (var e in GetTree().GetNodesInGroup("Enemies"))
            {
                var comm = e.GetNodeOrNull<SquadCommander>("SquadCommander");
                comm?.CleanupSquad();
            }
           
        };

        enemy.Position = pos;
        enemy.AddToGroup("Enemies");
        AddChild(enemy);

        var behavior = enemy.GetNode<EnemyBehavior>("EnemyBehavior");
        behavior.FactionId = faction;
        behavior.UnitNameOverride = NameGenerator.Generate();

        if (isCommander)
        {
            var commander = new SquadCommander();
            commander.Name = "SquadCommander";
            commander.SquadName = NameGenerator.GenerateSquadName();
            enemy.AddChild(commander);
        }

        return enemy;
    }

    private void CreateWall(Vector2 position, Vector2 size)
    {
        var wall = new StaticBody2D();
        wall.CollisionLayer = 2;
        wall.Position = position;
        AddChild(wall);

        var coll = new CollisionShape2D();
        var rect = new RectangleShape2D { Size = size };
        coll.Shape = rect;
        wall.AddChild(coll);
    }

    private void OnStartBattle()
    {
        foreach (var enemy in GetTree().GetNodesInGroup("Enemies"))
        {
            var commander = enemy.GetNodeOrNull<SquadCommander>("SquadCommander");
            if (commander != null)
            {
                var behavior = enemy.GetNode<EnemyBehavior>("EnemyBehavior");
                behavior.ExecuteOrder(new OrderData { Type = OrderType.Assault });
                commander.IssueOrders();
            }
        }
    }
}