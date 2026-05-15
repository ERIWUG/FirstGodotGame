using Godot;
using System.Collections.Generic;
[GlobalClass]
public partial class BaseArchetype : Node
{
    protected EnemyBehavior _behavior;
    protected CharacterBody2D _body;
    protected HealthComponent _health;
    protected SkillComponent _skill;
    protected SquadCommander _squad;
    protected SpellController _spellController;

    protected ResourceComponent _resources;

    public override void _Ready()
    {
        _behavior = GetParent().GetNode<EnemyBehavior>("EnemyBehavior");
        _body = GetParent<CharacterBody2D>();
        _health = _body.GetNode<HealthComponent>("HealthComponent");
        _skill = _body.GetNodeOrNull<SkillComponent>("SkillComponent");
        _squad = _body.GetNodeOrNull<SquadCommander>("SquadCommander");
        _resources = _body.GetNodeOrNull<ResourceComponent>("ResourceComponent");
        _spellController = _body.GetNodeOrNull<SpellController>("SpellController"); 
    }

    /// <summary>
    /// Позволяет архетипу переопределить выбор цели перед атакой/лечением.
    /// Возвращает null, если цель не изменена (поведение по умолчанию).
    /// </summary>
    public virtual Node2D OverrideTarget(Node2D currentTarget)
    {
        return currentTarget;
    }

    /// <summary>
    /// Позволяет архетипу отреагировать на приказ. Если возвращает true,
    /// стандартная обработка приказа не выполняется.
    /// </summary>
    public virtual bool HandleOrder(OrderData order)
    {
        return false;
    }

    /// <summary>
    /// Позволяет архетипу выбрать заклинание вместо стандартной логики.
    /// Возвращает null, если выбор должен остаться за стандартной логикой (атакующие заклинания).
    /// </summary>
    public virtual SpellData SelectSpell(List<SpellData> availableSpells, Node2D currentTarget)
    {
        return null;
    }

    // В BaseArchetype.cs
    /// <summary>
    /// Вызывается каждый кадр, когда юнит в состоянии Attacking.
    /// Возвращает true, если архетип полностью обработал этот тик (стандартная логика не нужна).
    /// </summary>
    public virtual bool ProcessAttack(EnemyBehavior owner, double delta)
    {
        return false; // по умолчанию используем старую логику
    }
}