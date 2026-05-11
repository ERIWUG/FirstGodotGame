using Godot;

[GlobalClass]
public partial class RegenEffect : StatusEffect
{
    [Export] public float HealPerTick { get; set; } = 3f;

    public override void OnTick(StatusEffectsComponent target)
    {
        target.HealDamage(HealPerTick);
    }
}