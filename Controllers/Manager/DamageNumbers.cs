using Godot;

public static class DamageNumbers
{
    private static PackedScene _floatingNumberScene;

    public static void Initialize()
    {
        _floatingNumberScene = GD.Load<PackedScene>("res://Scenes/UIBricks/FloatingNumber.tscn");
    }

    public static void Show(Vector2 globalPosition, float amount, Color? color = null)
    {
        if (_floatingNumberScene == null)
            Initialize();

        var instance = _floatingNumberScene.Instantiate<FloatingNumber>();
        instance.Text = amount.ToString("F0");
        instance.GlobalPosition = globalPosition + new Vector2(0, -20); // чуть выше точки
        instance.Modulate = color ?? Colors.White;

        // Добавляем на верхний слой UI (или в CanvasLayer)
        // Удобнее всего – прямо в корневую сцену
        var tree = Engine.GetMainLoop() as SceneTree;
        tree.CurrentScene.AddChild(instance);
    }
}