using Godot;
using System;

[GlobalClass]
public partial class BurnEffect : StatusEffect
{
    [Export] public float DamagePerTick { get; set; } = 5f;

    public override void OnTick(StatusEffectsComponent target)
    {
        target.DealDamage(DamagePerTick);
        target.GetParent().GetNode<Sprite2D>("Sprite2D").Modulate = Colors.Red;
    }

    public override void OnExpire(StatusEffectsComponent target)
    {  
        target.GetParent().GetNode<Sprite2D>("Sprite2D").Modulate = Colors.White;
    }
}
