using Godot;

[GlobalClass]
public partial class MovementController : Node
{
    [Export] public float Speed = 100.0f;
    [Export] public float AvoidanceRadius = 50.0f;
    [Export] public float AvoidanceStrength = 200.0f;

    private CharacterBody2D _body;

    public override void _Ready()
    {
        _body = GetParent<CharacterBody2D>();
    }

    public void MoveToward(Node2D target)
    {
        if (target == null || !IsInstanceValid(target)) return;
        Vector2 desiredDirection = (target.GlobalPosition - _body.GlobalPosition).Normalized();
        _body.Velocity = ComputeAvoidanceVelocity(desiredDirection);
    }

    public void FleeFrom(Node2D target)
    {
        if (target == null || !IsInstanceValid(target)) return;
        Vector2 desiredDirection = (_body.GlobalPosition - target.GlobalPosition).Normalized();
        _body.Velocity = ComputeAvoidanceVelocity(desiredDirection);
    }

    public void SlowApproach(Node2D target)
    {
        if (target == null || !IsInstanceValid(target)) return;
        Vector2 desiredDirection = (target.GlobalPosition - _body.GlobalPosition).Normalized();
        _body.Velocity = ComputeAvoidanceVelocity(desiredDirection) * 0.3f;
    }

    public void MoveToPosition(Vector2 targetPosition, double delta)
    {
        float dist = _body.GlobalPosition.DistanceTo(targetPosition);
        if (dist < 10.0f)
        {
            _body.Velocity = Vector2.Zero;
            return;
        }
        Vector2 dir = (targetPosition - _body.GlobalPosition).Normalized();
        _body.Velocity = dir * Speed;
    }

    public void Stop()
    {
        _body.Velocity = Vector2.Zero;
    }

    private Vector2 ComputeAvoidanceVelocity(Vector2 desiredDirection)
    {
        Vector2 avoidance = Vector2.Zero;
        foreach (var node in _body.GetTree().GetNodesInGroup("Enemies"))
        {
            if (node == _body) continue;
            var otherBody = node as CharacterBody2D;
            if (otherBody == null) continue;

            Vector2 toOther = otherBody.GlobalPosition - _body.GlobalPosition;
            float distance = toOther.Length();

            if (distance < AvoidanceRadius && distance > 0.1f)
            {
                float strength = Mathf.InverseLerp(AvoidanceRadius, 0, distance);
                Vector2 pushDir = -toOther.Normalized();
                avoidance += pushDir * strength * AvoidanceStrength;
            }
        }
        Vector2 finalDir = (desiredDirection * Speed + avoidance).Normalized();
        return finalDir * Speed;
    }
}