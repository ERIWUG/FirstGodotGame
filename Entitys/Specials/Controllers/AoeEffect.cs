using Godot;
public partial class AoeEffect : Area2D
{
    [Export] public float Damage { get; set; } = 20f;
    [Export] public float Lifetime { get; set; } = 0.8f; // длительность эффекта

    private AnimatedSprite2D _animatedSprite;

    public override void _Ready()
    {
        // Пытаемся найти анимированный спрайт
        _animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        _animatedSprite?.Play("explode"); // замени на имя своей анимации

        // Включаем обработку столкновений с телами
        BodyEntered += OnBodyEntered;

        // Уничтожаем эффект через Lifetime секунд
        GetTree().CreateTimer(Lifetime).Timeout += QueueFree;
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body == null) return;

        var health = body.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (health != null)
        {
            health.Damage(Damage);
            GD.Print($"AoE взрыв нанёс {Damage} урона по {body.Name}");
        }
    }
}