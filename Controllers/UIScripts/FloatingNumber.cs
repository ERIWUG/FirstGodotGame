using Godot;

public partial class FloatingNumber : Label
{
    [Export] public float Duration { get; set; } = 1.2f;
    [Export] public float RiseSpeed { get; set; } = 40.0f;

    public override void _Ready()
    {
        // Простая анимация: поднимаемся вверх, уменьшаем прозрачность, исчезаем
        var tween = CreateTween().SetParallel();
        tween.TweenProperty(this, "position:y", Position.Y - RiseSpeed, Duration);
        tween.TweenProperty(this, "modulate:a", 0.0f, Duration);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }
}