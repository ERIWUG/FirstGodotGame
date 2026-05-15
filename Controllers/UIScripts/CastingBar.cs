using Godot;

public partial class CastingBar : Control
{
    private ProgressBar _bar;
    private SpellController _spell;

    public override void _Ready()
    {
        _bar = GetNode<ProgressBar>("ProgressBar");
        Visible = false;

        // Ищем SpellController у родителя (там же, где лежит CastingBar)
        var parent = GetParentOrNull<Node2D>();
        if (parent != null)
            _spell = parent.GetNodeOrNull<SpellController>("SpellController");
    }

    public override void _Process(double delta)
    {
        if (_spell != null && _spell.IsCasting())
        {
            Visible = true;
            _bar.MaxValue = 1.0f; // будем показывать прогресс от 0 до 1
            _bar.Value = _spell.GetCastProgress();
        }
        else
        {
            Visible = false;
        }
    }
}