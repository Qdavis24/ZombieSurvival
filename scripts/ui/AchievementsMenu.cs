using Godot;

public partial class AchievementsMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    [Export] private VBoxContainer _list;
    [Export] private Label _totalsLabel;
    [Export] private Button _closeButton;

    private Font _font;

    private static readonly Color GoldColor = new(1f, 0.85f, 0.4f);
    private static readonly Color LockedNameColor = new(0.8f, 0.8f, 0.82f);
    private static readonly Color DescColor = new(0.62f, 0.62f, 0.64f);
    private static readonly Color UnlockedColor = new(0.5f, 0.9f, 0.5f);
    private static readonly Color DimColor = new(0.5f, 0.5f, 0.52f);

    public override void _Ready()
    {
        Visible = false;
        _font = GD.Load<Font>("res://scenes/ui/BoldsPixels.ttf");

        if (_closeButton != null)
            _closeButton.Pressed += Close;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible)
            return;

        if (@event.IsActionPressed("pause"))
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    public void Open()
    {
        Visible = true;
        Refresh();
    }

    public void Close()
    {
        if (!Visible)
            return;

        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    private void Refresh()
    {
        var manager = AchievementManager.Instance;
        if (manager == null || _list == null)
            return;

        if (_totalsLabel != null)
            _totalsLabel.Text = $"Kills: {manager.TotalKills}     Headshots: {manager.TotalHeadshots}     Best Round: {manager.BestRound}";

        foreach (var child in _list.GetChildren())
            child.QueueFree();

        foreach (var def in manager.Definitions)
            _list.AddChild(BuildRow(manager, def));
    }

    private Control BuildRow(AchievementManager manager, AchievementManager.AchievementDef def)
    {
        var unlocked = manager.IsUnlocked(def.Id);

        var panel = new PanelContainer();
        var style = new StyleBoxFlat
        {
            BgColor = unlocked ? new Color(0.13f, 0.11f, 0.06f, 0.9f) : new Color(0.08f, 0.08f, 0.09f, 0.9f),
            BorderColor = unlocked ? new Color(GoldColor, 0.8f) : new Color(0.3f, 0.3f, 0.33f, 0.8f),
        };
        style.SetBorderWidthAll(1);
        style.SetCornerRadiusAll(6);
        style.SetContentMarginAll(10);
        panel.AddThemeStyleboxOverride("panel", style);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);
        panel.AddChild(hbox);

        var text = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        text.AddChild(MakeLabel(def.Name, 26, unlocked ? GoldColor : LockedNameColor));
        text.AddChild(MakeLabel(def.Description, 16, DescColor));
        hbox.AddChild(text);

        hbox.AddChild(BuildStatus(manager, def, unlocked));
        return panel;
    }

    private Control BuildStatus(AchievementManager manager, AchievementManager.AchievementDef def, bool unlocked)
    {
        if (unlocked)
        {
            var label = MakeLabel("Unlocked", 24, UnlockedColor);
            label.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            return label;
        }

        if (def.Target > 0)
        {
            var current = manager.GetProgress(def);
            var box = new VBoxContainer { CustomMinimumSize = new Vector2(200, 0), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };

            var bar = new ProgressBar
            {
                MinValue = 0,
                MaxValue = def.Target,
                Value = Mathf.Min(current, def.Target),
                ShowPercentage = false,
                CustomMinimumSize = new Vector2(200, 14),
            };
            box.AddChild(bar);

            var label = MakeLabel($"{current} / {def.Target}", 18, GoldColor);
            label.HorizontalAlignment = HorizontalAlignment.Right;
            box.AddChild(label);
            return box;
        }

        var locked = MakeLabel("Locked", 24, DimColor);
        locked.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        return locked;
    }

    private Label MakeLabel(string textValue, int fontSize, Color color)
    {
        var label = new Label { Text = textValue };
        if (_font != null)
            label.AddThemeFontOverride("font", _font);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        return label;
    }
}
