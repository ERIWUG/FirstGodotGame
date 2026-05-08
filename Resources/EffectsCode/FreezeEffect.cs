using Godot;
using System;
[GlobalClass]
public partial class FreezeEffect : StatusEffect
{
    [Export] public float SlowAmount { get; set; } = 0.5f; // замедление на 50%

    public override void OnApply(StatusEffectsComponent target)
    {
        // Замедляем врага, если у неsго есть MovementComponent (или просто меняем Modulate)
        var sprite = target.GetParent().GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite != null)
            sprite.Modulate = new Color(0.5f, 0.5f, 1f); // синий
    }

    public override void OnExpire(StatusEffectsComponent target)
    {
        var sprite = target.GetParent().GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite != null)
            sprite.Modulate = Colors.White;
    }
}
