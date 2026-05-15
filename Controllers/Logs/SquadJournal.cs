using Godot;
using System.Collections.Generic;

public partial class SquadJournal : Node
{
    public static SquadJournal Instance { get; private set; }

    private List<string> _entries = new();
    
    [Signal] public delegate void EntryAddedEventHandler(string entry);
    // Новый сигнал: имя, цвет имени, сообщение
    [Signal] public delegate void ColoredEntryAddedEventHandler(string speakerName, Color nameColor, string message);

    public override void _Ready()
    {
        Instance = this;
    }

    public void AddEntry(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string entry = $"[{timestamp}] {message}";
        _entries.Add(entry);
        EmitSignal(SignalName.EntryAdded, entry);
        GD.Print($"[Journal] {entry}");
    }

    public void AddColoredEntry(string speakerName, Color nameColor, string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string entry = $"[{timestamp}] {speakerName}: {message}";
        _entries.Add(entry);
        EmitSignal(SignalName.ColoredEntryAdded, speakerName, nameColor, message);
        GD.Print($"[Journal] {entry}");
    }

    public List<string> GetAllEntries() => new List<string>(_entries);
}