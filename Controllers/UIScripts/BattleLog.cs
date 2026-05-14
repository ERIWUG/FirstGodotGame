using Godot;

public partial class BattleLog : Panel
{
    private Label _logLabel;
    private ScrollContainer _scroll;

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

        // ScrollContainer с отключённой горизонтальной прокруткой
        _scroll = new ScrollContainer();
        _scroll.AnchorRight = 1;
        _scroll.AnchorBottom = 1;
        _scroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        AddChild(_scroll);

        // Label для текста
        _logLabel = new Label();
        _logLabel.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        _logLabel.AutowrapMode = TextServer.AutowrapMode.Word; // перенос по словам
        // Гарантируем, что Label не сжимается меньше ширины ScrollContainer
        _logLabel.CustomMinimumSize = new Vector2(_scroll.Size.X, 0);
        _scroll.AddChild(_logLabel);

        // Подписываемся на сигнал
        SquadJournal.Instance.EntryAdded += OnEntryAdded;

        // Загружаем старые записи
        var allEntries = SquadJournal.Instance.GetAllEntries();
        if (allEntries.Count > 0)
            _logLabel.Text = string.Join("\n", allEntries);
        else
            _logLabel.Text = "";

        // Устанавливаем позицию справа
        CallDeferred(nameof(SetFixedPosition));
        Visible = false;
    }

    private void SetFixedPosition()
    {
        var windowSize = DisplayServer.WindowGetSize();
        float x = windowSize.X - this.Size.X - 20; // 20 пикселей от правого края
        float y = 50;
        this.SetGlobalPosition(new Vector2(x, y));
    }

    private void OnEntryAdded(string entry)
    {
        // Добавляем новую строку
        if (string.IsNullOrEmpty(_logLabel.Text))
            _logLabel.Text = entry;
        else
            _logLabel.Text += "\n" + entry;

        // Прокручиваем вниз
        CallDeferred(nameof(ScrollToBottom));
    }

    private void ScrollToBottom()
    {
        _scroll.ScrollVertical = (int)_scroll.GetVScrollBar().MaxValue;
    }

    public void ToggleVisible()
    {
        Visible = !Visible;
        if (Visible)
            ScrollToBottom();
    }
}