using Godot;
using System;
[GlobalClass]
public partial class PoisonEffect : StatusEffect
{
    
    [Export] public float DamagePerTick { get; set; } = 2f; // яд бьёт слабее огня

    public override void OnTick(StatusEffectsComponent target)
    {
        target.DealDamage(DamagePerTick);
        // Визуальный эффект: зелёный оттенок
        var sprite = target.GetParent().GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite != null)
            sprite.Modulate = new Color(0.5f, 1f, 0.5f);
    }

    public override void OnExpire(StatusEffectsComponent target)
    {
        var sprite = target.GetParent().GetNodeOrNull<Sprite2D>("Sprite2D");
        if (sprite != null)
            sprite.Modulate = Colors.White;
    }
}
