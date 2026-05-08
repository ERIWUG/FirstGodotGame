using Godot;

public partial class StatusIcon : Sprite2D
{
    [Export] public float Duration = 1.5f; // Сколько секунд висит

    public override void _Ready()
    {
        // Автоматически удаляемся через Duration секунд
        GetTree().CreateTimer(Duration).Timeout += QueueFree;
        
        // Небольшая анимация: плавно исчезаем в конце
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0.0f, Duration * 0.5f).SetDelay(Duration * 0.5f);
    }
}