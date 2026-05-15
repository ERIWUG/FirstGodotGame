using Godot;

public partial class BattleLog : Panel
{
    private RichTextLabel _log;
    private Color _defaultColor = Colors.White;

    public override void _Ready()
    {
        // Фиксированный размер панели
        this.AnchorLeft = 0;
        this.AnchorTop = 0;
        this.AnchorRight = 0;
        this.AnchorBottom = 0;
        this.Size = new Vector2(400, 300);

        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0, 0, 0, 0.7f);
        this.AddThemeStyleboxOverride("panel", panelStyle);

        // RichTextLabel с автопрокруткой и переносом строк
        _log = new RichTextLabel();
        _log.AnchorRight = 1;
        _log.AnchorBottom = 1;
        _log.BbcodeEnabled = true;
        _log.ScrollFollowing = true;
        _log.AutowrapMode = TextServer.AutowrapMode.Word;
        AddChild(_log);

        // Подписываемся на сигналы
        SquadJournal.Instance.EntryAdded += OnEntryAdded;
        SquadJournal.Instance.ColoredEntryAdded += OnColoredEntryAdded;

        // Загружаем старые записи (все как обычный текст)
        var allEntries = SquadJournal.Instance.GetAllEntries();
        foreach (var entry in allEntries)
            _log.AppendText(entry + "\n");

        // Позиция справа
        CallDeferred(nameof(SetFixedPosition));
        Visible = false;
    }

    private void SetFixedPosition()
    {
        var windowSize = DisplayServer.WindowGetSize();
        float x = windowSize.X - this.Size.X - 20;
        float y = 50;
        this.SetGlobalPosition(new Vector2(x, y));
    }

    private void OnEntryAdded(string entry)
    {
        _log.AppendText(entry + "\n");
    }

    private void OnColoredEntryAdded(string speakerName, Color nameColor, string message)
    {
        // Цвет имени — hex-строка, сообщение — белое
        string colorHex = nameColor.ToHtml(false);
        _log.AppendText($"[color={colorHex}]{speakerName}[/color]: {message}\n");
    }

    public void ToggleVisible()
    {
        Visible = !Visible;
    }
}