using Godot;

public partial class Projectile : Area2D
{
    public Vector2 Velocity { get; set; }
    public float Damage { get; set; }
    public Node2D Source { get; set; }
    public float Lifetime { get; set; } = 5.0f;
    public float ImmunityTime { get; set; } = 0.15f; // время неуязвимости при старте

    private bool _canCollide = false;

    public override void _Ready()
    {
        // Подключаем сигнал удара
        BodyEntered += OnBodyEntered;
        
        // Таймер на удаление через Lifetime секунд
        GetTree().CreateTimer(Lifetime).Timeout += QueueFree;
        
        // Таймер на включение коллизии через ImmunityTime секунд
        GetTree().CreateTimer(ImmunityTime).Timeout += () => { _canCollide = true; };
    }

    public override void _PhysicsProcess(double delta)
    {
        Position += Velocity * (float)delta;
    }

	private void OnBodyEntered(Node2D body) {
		if (!_canCollide || body == Source) return;
		var health = body.GetNodeOrNull<HealthComponent>("HealthComponent");
		if (health != null) {
			health.Damage(Damage);
			QueueFree();
		}
	}
}