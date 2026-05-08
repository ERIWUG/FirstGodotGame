using Godot;

public partial class MeleeAttack : Area2D
{
    [Export] public float Damage { get; set; } = 10f;
    [Export] public float Duration { get; set; } = 0.4f; // длительность всей атаки

    private AnimatedSprite2D _animatedSprite;
    private CollisionShape2D _collisionShape;

    public override void _Ready()
    {
        _animatedSprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");

        // Проигрываем анимацию
        _animatedSprite.Play("attack"); // предполагаем, что анимация называется "attack"
        
        // При столкновении с телом наносим урон
        BodyEntered += OnBodyEntered;

        // Уничтожаем сцену после завершения анимации
        GetTree().CreateTimer(Duration).Timeout += QueueFree;
    }

    private void OnBodyEntered(Node2D body)
    {
        // Находим HealthComponent у цели и наносим урон
        var health = body.GetNodeOrNull<HealthComponent>("HealthComponent");
        health?.Damage(Damage);
    }
}