using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class SquadData : Resource
{
    [Export] public string SquadName { get; set; } = "Новый отряд";
    [Export] public string FactionId { get; set; } = "neutral";
    [Export] public bool HasCommander { get; set; } = true;
    [Export] public int CommanderIndex { get; set; } = 0; // какой по счёту юнит станет командиром

    [Export] public Godot.Collections.Array<UnitEntry> Units { get; set; } = new();
}
