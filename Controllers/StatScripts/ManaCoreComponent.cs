using Godot;
using System.Collections.Generic;
using System.Linq;

[GlobalClass]
public partial class ManaCoreComponent : Node
{
    [Signal]
    public delegate void CoreAddedEventHandler(string coreId, string elementName, int rankValue);

    [Signal]
    public delegate void CoreRemovedEventHandler(string coreId);

    [Signal]
    public delegate void CoreRankUpgradedEventHandler(string coreId, int newRankValue);

    private List<ManaCoreData> _cores = new();
    private SoulComponent _soul;
    private ResourceComponent _resource;
    private CombatStatsComponent _stats;

    public override void _Ready()
    {
        _soul = GetNode<SoulComponent>("../SoulComponent");
        _resource = GetNode<ResourceComponent>("../ResourceComponent");
        _stats = GetNode<CombatStatsComponent>("../CombatStatsComponent");

        if (!IsManaCripple())
        {
            var manaCore = CreateBaseManaCore();
            AddCore(manaCore);
        }
    }

    private bool IsManaCripple() => GetParent().HasNode("ManaCrippleFlag");

    private ManaCoreData CreateBaseManaCore()
    {
        var core = new ManaCoreData
        {
            CoreName = "Ядро Маны",
            Element = CoreElement.Mana,
            Rank = CoreRank.OneStar,
            UnlockedModifiers = new List<string> { "projectile", "beam", "aura" }
        };
        return core;
    }

    public void AddCore(ManaCoreData core)
    {
        if (!_soul.CanAddCore(_cores.Count))
        {
            GD.PrintErr($"{Owner.Name}: Невозможно добавить ядро — достигнут лимит слотов!");
            return;
        }

        _cores.Add(core);
        ApplyCoreBonuses(core);
        EmitSignal(SignalName.CoreAdded, core.CoreName, core.Element.ToString(), (int)core.Rank);
        GD.Print($"{Owner.Name} получил ядро: {core.CoreName} ({core.Rank})");
    }

    public void RemoveCore(ManaCoreData core)
    {
        if (_cores.Remove(core))
        {
            RemoveCoreBonuses(core);
            EmitSignal(SignalName.CoreRemoved, core.CoreName);
        }
    }

    public void UpgradeCoreRank(ManaCoreData core, CoreRank newRank)
    {
        if (core.Rank >= newRank) return;

        RemoveCoreBonuses(core);
        core.Rank = newRank;
        ApplyCoreBonuses(core);
        EmitSignal(SignalName.CoreRankUpgraded, core.CoreName, (int)newRank);
    }

    private void ApplyCoreBonuses(ManaCoreData core)
    {
        if (core.IsShattered) return;
        _resource.SetMax("MP", _resource.GetMax("MP") + core.GetManaBonus());

        if (core.Element == CoreElement.Fire)
        {
            var mod = new StatModifier { Value = 0.05f, Type = StatModifierType.PercentAdd };
            _stats.AddModifier("FireDamage", mod);
        }
    }

    private void RemoveCoreBonuses(ManaCoreData core)
    {
        _resource.SetMax("MP", _resource.GetMax("MP") - core.GetManaBonus());
        // Аналогично удалить модификаторы
    }

    public List<ManaCoreData> GetAllCores() => _cores.ToList();
    public int GetCoreCount() => _cores.Count;
}