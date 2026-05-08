using Godot;

public partial class PlayerController : CharacterBody2D
{
    [Export] public float Speed = 200.0f;
     

    public override void _PhysicsProcess(double delta)
    {
        // Получаем вектор движения от клавиатуры
        var inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        Velocity = inputDir * Speed;
        MoveAndSlide();
    }

    public override void _Ready()
{
    
    
}

   
}