using Godot;

[GlobalClass]
public partial class UnitEntry : Resource
{
    [Export] public PackedScene UnitScene { get; set; }
    [Export] public Vector2 Position { get; set; }
    [Export] public string FactionOverride { get; set; } = "";
    [Export] public bool IsCommander { get; set; } = false;
}