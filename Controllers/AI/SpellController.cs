using Godot;
using System.Linq;
using System.Collections.Generic;

[GlobalClass]
public partial class SpellController : Node
{
    [Export] public float PreferredCastDistance = 300.0f;
    [Export] public float MinCastDistance = 80.0f;

    private CharacterBody2D _body;
    private SkillComponent _skill;
    private ResourceComponent _resources;
    private BaseArchetype _archetype;
    private MovementController _movement;

    private float _castTimer;
    private SpellData _pendingSpell;

    public override void _Ready()
    {
        _body = GetParent<CharacterBody2D>();
        _skill = _body.GetNodeOrNull<SkillComponent>("SkillComponent");
        _resources = _body.GetNodeOrNull<ResourceComponent>("ResourceComponent");
        _archetype = _body.GetNodeOrNull<BaseArchetype>("Archetype");
        _movement = _body.GetNodeOrNull<MovementController>("MovementController");
    }

    public bool CanCast()
    {
        return _skill != null && _resources != null && _skill.GetKnownSpells().Count > 0;
    }

    /// <summary>
    /// Пытается начать каст заклинания. Возвращает true, если каст начат или продолжается.
    /// </summary>
    public bool TryCast(Node2D target, double delta)
    {
        if (!CanCast()) return false;
        if (target == null || !IsInstanceValid(target)) return false;

        // Если уже кастуем — продолжаем
        if (_pendingSpell != null)
        {
            _castTimer += (float)delta;
            if (_castTimer >= _pendingSpell.TotalCastTime)
            {
                FinishCasting(target);
                return true;
            }
            return true; // всё ещё кастуем
        }

        // Выбираем заклинание
        var spell = EvaluateSpell(target);
        if (spell == null || !_skill.CanCast(spell)) return false;

        // Если заклинание мгновенное — сразу применяем
        if (spell.TotalCastTime <= 0)
        {
            _skill.CastSpell(spell, target);
            return true;
        }

        // Начинаем долгий каст
        _pendingSpell = spell;
        _castTimer = 0f;
        return true;
    }

    private SpellData EvaluateSpell(Node2D target)
    {
        var spells = _skill.GetKnownSpells();
        if (spells.Count == 0) return null;

        // Если есть архетип, даём ему выбрать
        if (_archetype != null)
        {
            var archetypeSpell = _archetype.SelectSpell(spells, target);
            if (archetypeSpell != null) return archetypeSpell;
        }

        // Стандартный выбор (первое попавшееся)
        return spells[0];
    }

    private void FinishCasting(Node2D target)
    {
        _skill.CastSpell(_pendingSpell, target);

        // Создаём визуальные эффекты
        if (_pendingSpell.Modifiers.Any(m => m.Type == ModifierType.Form && m.Id == "projectile"))
        {
            var projectileScene = GD.Load<PackedScene>("res://Entitys/Specials/Projectile.tscn");
            var projectile = projectileScene.Instantiate<Projectile>();
            projectile.Position = _body.GlobalPosition;
            projectile.Source = _body;
            projectile.Damage = _pendingSpell.TotalManaCost * 2;
            var targetPos = target.GlobalPosition;
            projectile.Velocity = (targetPos - _body.GlobalPosition).Normalized() * 300f;
            projectile.GlobalPosition = _body.GlobalPosition;
            _body.GetTree().CurrentScene.AddChild(projectile);
        }
        else if (_pendingSpell.Modifiers.Any(m => m.Id == "explosion"))
        {
            var aoeScene = GD.Load<PackedScene>("res://Entitys/Specials/AoeEffect.tscn");
            var aoe = aoeScene.Instantiate<AoeEffect>();
            aoe.GlobalPosition = target.GlobalPosition;
            aoe.Damage = _pendingSpell.TotalManaCost * 2;
            _body.GetTree().CurrentScene.AddChild(aoe);
        }

        _pendingSpell = null;
    }

    public void InterruptCast()
    {
        _pendingSpell = null;
        _castTimer = 0f;
    }

    public bool IsCasting() => _pendingSpell != null;
    public float GetCastProgress() => _pendingSpell != null ? _castTimer / _pendingSpell.TotalCastTime : 0f;
}