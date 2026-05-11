using Godot;

[GlobalClass]
public partial class RaiseDeadEffect : StatusEffect
{
    [Export] public PackedScene SkeletonScene;
    public string CasterFaction { get; set; }
    public SquadCommander CasterSquad { get; set; }   // <-- прямая ссылка на отряд некроманта

    private HealthComponent _targetHealth;

    public override void OnApply(StatusEffectsComponent target)
    {
        var body = target.GetParent<CharacterBody2D>();
        _targetHealth = body?.GetNodeOrNull<HealthComponent>("HealthComponent");
        if (_targetHealth != null)
            _targetHealth.HealthDepleted += SpawnSkeleton;
    }

    private void SpawnSkeleton()
    {
        if (SkeletonScene == null) return;
        _targetHealth?.GetTree().CurrentScene.CallDeferred("add_child", CreateSkeleton());
    }

    private CharacterBody2D CreateSkeleton()
    {
        var skeleton = SkeletonScene.Instantiate<CharacterBody2D>();
        skeleton.GlobalPosition = _targetHealth.GetParent<CharacterBody2D>().GlobalPosition;
        skeleton.AddToGroup("Enemies");

        var skeletonBehavior = skeleton.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
        if (skeletonBehavior == null) return skeleton;

        // Назначаем фракцию
        if (!string.IsNullOrEmpty(CasterFaction))
            skeletonBehavior.FactionId = CasterFaction;

        // Добавляем скелета в отряд некроманта, если он есть
        CasterSquad?.AddMember(skeletonBehavior);

        return skeleton;
    }

    public override void OnExpire(StatusEffectsComponent target)
    {
        if (_targetHealth != null)
            _targetHealth.HealthDepleted -= SpawnSkeleton;
    }
}