using Godot;

public partial class AITestSetup : Node2D
{
    private Label _logLabel;

    public override void _Ready()
    {
        // 1. Создаём игрока (можно тоже вынести в сцену, но для теста оставим кодом)
        var player = CreateCharacter("Player", Colors.Red, new Vector2(100, 300));
        player.AddToGroup("Player");

        var camera = new Camera2D
        {
            Name = "Camera2D",
            Position = new Vector2(400, 300), // центр предполагаемой арены
            Zoom = new Vector2(1, 1)
        };
        AddChild(camera);
        camera.MakeCurrent();

        // 2. Загружаем врагов из сцен
        SpawnEnemy("res://Entitys/Enemies/Fanatic.tscn", new Vector2(700, 100));
        SpawnEnemy("res://Entitys/Enemies/Mercenary.tscn", new Vector2(700, 300));
        SpawnEnemy("res://Entitys/Enemies/Guard.tscn", new Vector2(700, 500));

        // 3. Строим UI (как и раньше)
        var canvasLayer = new CanvasLayer();
        AddChild(canvasLayer);

        var panel = new Panel();
        panel.Size = new Vector2(300, 200);
        panel.Position = new Vector2(10, 10);
        panel.Modulate = new Color(0, 0, 0, 0.7f);
        canvasLayer.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.Position = new Vector2(15, 15);
        vbox.Size = new Vector2(280, 180);
        canvasLayer.AddChild(vbox);

        var btnAssault = new Button { Text = "Приказ: Атака" };
        var btnRetreat = new Button { Text = "Приказ: Отступление" };
        var btnResetFear = new Button { Text = "Сбросить страх" };
        _logLabel = new Label { Text = "Готов", Modulate = Colors.White };

        vbox.AddChild(btnAssault);
        vbox.AddChild(btnRetreat);
        vbox.AddChild(btnResetFear);
        vbox.AddChild(_logLabel);

        btnAssault.Pressed += () =>
        {
            Log("Отправлен приказ АТАКА");
            SendOrderToAll(OrderType.Assault, commanderBonus: 30);
        };
        btnRetreat.Pressed += () =>
        {
            Log("Отправлен приказ ОТСТУПЛЕНИЕ");
            SendOrderToAll(OrderType.Retreat, commanderBonus: 0);
        };
        btnResetFear.Pressed += () =>
        {
            Log("Страх сброшен");
            ResetAllFear();
        };

       
    }

    // Вспомогательный метод для создания игрока (можно оставить кодом, так как игрок один)
    private CharacterBody2D CreateCharacter(string name, Color color, Vector2 position)
    {
        var body = new CharacterBody2D { Name = name, Position = position };
        AddChild(body);

        var shape = new CollisionShape2D();
        var rect = new RectangleShape2D { Size = new Vector2(32, 32) };
        shape.Shape = rect;
        body.AddChild(shape);

        var sprite = new Sprite2D();
        var image = Image.Create(32, 32, false, Image.Format.Rgba8);
        image.Fill(color);
        sprite.Texture = ImageTexture.CreateFromImage(image);
        body.AddChild(sprite);

        return body;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton && mouseButton.Pressed)
        {
            var camera = GetNodeOrNull<Camera2D>("Camera2D");
            if (camera == null) return;

            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
                camera.Zoom *= 1.1f; // приблизить
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
                camera.Zoom *= 0.9f; // отдалить
        }
    }

    // Загрузка врага из сцены и установка позиции
    private void SpawnEnemy(string scenePath, Vector2 position)
    {
        var packed = GD.Load<PackedScene>(scenePath);
        if (packed == null)
        {
            GD.PrintErr($"Не удалось загрузить сцену: {scenePath}");
            return;
        }

        var enemy = packed.Instantiate<CharacterBody2D>();
        enemy.Position = position;
        enemy.AddToGroup("Enemies");
        AddChild(enemy);
    }

    // Рассылка приказа всем врагам из группы "Enemies"
    private void SendOrderToAll(OrderType type, int commanderBonus)
    {
        var playerPos = GetNode<CharacterBody2D>("Player").GlobalPosition;
        var enemies = GetTree().GetNodesInGroup("Enemies");
        foreach (var enemy in enemies)
        {
            var behavior = enemy.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
            behavior?.ExecuteOrder(new OrderData
            {
                Type = type,
                CommanderBonus = commanderBonus,
                TargetPosition = playerPos
            });
        }
    }

    // Сброс страха у всех врагов
    private void ResetAllFear()
    {
        foreach (var enemy in GetTree().GetNodesInGroup("Enemies"))
        {
            var behavior = enemy.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
            behavior?.ResetFear();
        }
    }

    private void Log(string message)
    {
        _logLabel.Text = message;
        GD.Print(message);
    }
}