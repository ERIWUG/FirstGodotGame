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

        // --- Отряд А (теневой культ) ---
        // Командир (Фанатик)
        SpawnEnemy("res://Entitys/Enemies/Fanatic.tscn", new Vector2(150, 350), "shadow_cult", true);
        // Огр (1)
        SpawnEnemy("res://Entitys/Enemies/Ogre.tscn", new Vector2(200, 250), "shadow_cult");
        // Рыцари (2)
        SpawnEnemy("res://Entitys/Enemies/Knight.tscn", new Vector2(250, 150), "shadow_cult");
        SpawnEnemy("res://Entitys/Enemies/Knight.tscn", new Vector2(250, 550), "shadow_cult");
        // Бард (1)
        SpawnEnemy("res://Entitys/Enemies/Bard.tscn", new Vector2(180, 450), "shadow_cult");
        // Наёмник (1)
        SpawnEnemy("res://Entitys/Enemies/Mercenary.tscn", new Vector2(300, 80), "shadow_cult");
        // Эльф-лучник (1)
        SpawnEnemy("res://Entitys/Enemies/ElfArcher.tscn", new Vector2(300, 600), "shadow_cult");
        // Стражник (1)
        SpawnEnemy("res://Entitys/Enemies/Guard.tscn", new Vector2(300, 350), "shadow_cult");

        // --- Отряд Б (наёмники) ---
        // Командир (Наёмник-лидер)
        SpawnEnemy("res://Entitys/Enemies/Mercenary.tscn", new Vector2(1050, 350), "mercenary", true);
        // Огр (1)
        SpawnEnemy("res://Entitys/Enemies/Ogre.tscn", new Vector2(1000, 450), "mercenary");
        // Рыцари (2)
        SpawnEnemy("res://Entitys/Enemies/Knight.tscn", new Vector2(950, 150), "mercenary");
        SpawnEnemy("res://Entitys/Enemies/Knight.tscn", new Vector2(950, 550), "mercenary");
        // Эльф-лучник (1)
        SpawnEnemy("res://Entitys/Enemies/ElfArcher.tscn", new Vector2(1020, 250), "mercenary");
        // Ведьма (1)
        SpawnEnemy("res://Entitys/Enemies/Witch.tscn", new Vector2(980, 300), "mercenary");
        // Наёмник (1)
        SpawnEnemy("res://Entitys/Enemies/Mercenary.tscn", new Vector2(900, 100), "mercenary");
        // Стражник (1)
        SpawnEnemy("res://Entitys/Enemies/Guard.tscn", new Vector2(900, 600), "mercenary");

        // UI
        var canvasLayer = new CanvasLayer();
        AddChild(canvasLayer);
        _startButton = new Button { Text = "Начать бой", Position = new Vector2(10, 10) };
        _startButton.Pressed += OnStartBattle;
        canvasLayer.AddChild(_startButton);

        // Собираем отряды заново, когда все уже созданы
        foreach (var enemy in GetTree().GetNodesInGroup("Enemies"))
        {
            var commander = enemy.GetNodeOrNull<SquadCommander>("SquadCommander");
            commander?.GatherSquad();
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