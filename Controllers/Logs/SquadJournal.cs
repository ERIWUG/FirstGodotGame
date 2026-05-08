using Godot;
using System;
using System.Collections.Generic;

public partial class SquadJournal : Node
{
    public static SquadJournal Instance { get; private set; }

    private List<string> _entries = new();
    [Signal] public delegate void EntryAddedEventHandler(string entry);

    public override void _Ready()
    {
        Instance = this;
    }

    public void AddEntry(string message)
    {
        string timestamp = DateTime.Now.ToString("HH:mm:ss");
        string entry = $"[{timestamp}] {message}";
        _entries.Add(entry);
        EmitSignal(SignalName.EntryAdded, entry);
        GD.Print($"JOURNAL: {entry}");
    }

    public List<string> GetAllEntries() => new List<string>(_entries);
}