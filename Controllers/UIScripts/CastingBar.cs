using Godot;

public partial class CastingBar : Control
{
    private ProgressBar _bar;
    private EnemyBehavior _behavior;
    
    public override void _Ready()
    {
        _bar = GetNode<ProgressBar>("ProgressBar");
        Visible = false;

        var target = GetParentOrNull<Node2D>();
        if (target != null)
        {
            _behavior = target.GetNodeOrNull<EnemyBehavior>("EnemyBehavior");
            if (_behavior != null)
            {
                _behavior.CastingStarted += OnCastingStarted;
                _behavior.CastingFinished += OnCastingFinished;
            }
        }

       
    }

    public override void _Process(double delta)
    {
        if (_behavior != null && _behavior.CurrentState == AIState.Casting)
            _bar.Value = _behavior.GetCastElapsed();
    }

    private void OnCastingStarted()
    {
        Visible = true;
        _bar.Value = 0;
        _bar.MaxValue = _behavior.GetCastDuration();
    }

    private void OnCastingFinished()
    {
        Visible = false;
    }
}