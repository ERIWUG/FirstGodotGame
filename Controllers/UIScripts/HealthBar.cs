using Godot;

public partial class HealthBar : Control
{
    private ProgressBar _bar;
    private HealthComponent _health;
   

    public override void _Ready()
    {
        _bar = GetNode<ProgressBar>("ProgressBar");
        Visible = false;

        var target = GetParentOrNull<Node2D>();
        if (target != null)
        {
            _health = target.GetNodeOrNull<HealthComponent>("HealthComponent");
            if (_health != null)
            {
                _health.HealthChanged += OnHealthChanged;
                _bar.MaxValue = _health.MaxHealth;
                _bar.Value = _health.CurrentHealth;
                Visible = true;
            }
        }

       
    }

    private void OnHealthChanged(float current, float max)
    {
        _bar.MaxValue = max;
        _bar.Value = current;
        if (current <= 0) QueueFree();
    }
}