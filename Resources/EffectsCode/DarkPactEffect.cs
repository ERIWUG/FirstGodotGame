using Godot;
[GlobalClass]
public partial class DarkPactEffect : StatusEffect
{
    [Export] public float HealthCostPerTick = 2f;
    public override void OnTick(StatusEffectsComponent target)
    {
        target.DealDamage(HealthCostPerTick);
    }
}