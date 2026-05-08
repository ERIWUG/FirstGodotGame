using Godot;

public partial class MagicDuel : Node2D
{
    private EnemyBehavior _mage1;
    private EnemyBehavior _mage2;

    public override void _Ready()
    {
        // Камера
        var camera = new Camera2D { Position = new Vector2(600, 350), Zoom = new Vector2(2, 2) };
        AddChild(camera);
        camera.MakeCurrent();

        // Левый маг (Фанатик)
        var fanatic = SpawnEnemy("res://Entitys/Enemies/Fanatic.tscn", new Vector2(300, 350), "shadow_cult");
        _mage1 = fanatic.GetNode<EnemyBehavior>("EnemyBehavior");

        // Правый маг (Ведьма)
        var witch = SpawnEnemy("res://Entitys/Enemies/Witch.tscn", new Vector2(900, 350), "mercenary");
        _mage2 = witch.GetNode<EnemyBehavior>("EnemyBehavior");

        // Кнопка «В бой!»
        var canvasLayer = new CanvasLayer();
        AddChild(canvasLayer);
        var startButton = new Button { Text = "В бой!", Position = new Vector2(10, 10) };
        startButton.Pressed += OnStartDuel;
        canvasLayer.AddChild(startButton);
    }

    private Node2D SpawnEnemy(string path, Vector2 pos, string faction)
    {
        var packed = GD.Load<PackedScene>(path);
        var enemy = packed.Instantiate<CharacterBody2D>();
        enemy.Position = pos;
        enemy.AddToGroup("Enemies");
        AddChild(enemy);

        var behavior = enemy.GetNode<EnemyBehavior>("EnemyBehavior");
        behavior.FactionId = faction;
        // Чтобы не мешаться, убираем у них трусость и страх
        behavior.Courage = 100;
        behavior.FearResistance = 1000;
        
        return enemy;
    }

    private void OnStartDuel()
    {
        if (_mage1 != null)
        {
            // Даём приказ атаковать ведьму
            _mage1.ExecuteOrder(new OrderData { Type = OrderType.Assault });
        }
        if (_mage2 != null)
        {
            _mage2.ExecuteOrder(new OrderData { Type = OrderType.Assault });
        }
    }
}