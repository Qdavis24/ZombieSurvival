using Godot;

public partial class OptionsMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private const string SettingsPath = "user://audio_settings.cfg";
    private const string AudioSection = "audio";
    private const string InputSection = "input";
    private const string VideoSection = "video";
    private const float MutedDb = -80f;
    private const float MinSensitivity = 0.1f;
    private const float MaxSensitivity = 3.0f;
    private const float DefaultSensitivity = 1.0f;
    private const int DefaultResolutionIndex = 2;

    private static readonly string[] SfxBuses = ["SFX", "Explosion"];
    private static readonly Vector2I[] Resolutions =
    {
        new(1280, 720), new(1600, 900), new(1920, 1080), new(2560, 1440),
    };

    // Read by PlayerController when it spawns; OptionsMenu._Ready loads it at startup.
    public static float MouseSensitivityMultiplier { get; private set; } = DefaultSensitivity;

    [Export] private HSlider _masterSlider;
    [Export] private HSlider _musicSlider;
    [Export] private HSlider _sfxSlider;
    [Export] private Label _masterValueLabel;
    [Export] private Label _musicValueLabel;
    [Export] private Label _sfxValueLabel;
    [Export] private Button _closeButton;
    [Export] private HSlider _sensitivitySlider;
    [Export] private Label _sensitivityValueLabel;
    [Export] private OptionButton _resolutionOption;
    [Export] private CheckButton _fullscreenCheck;

    private bool _loading;
    private bool _startupFullscreen = true;
    private int _startupResolutionIndex = DefaultResolutionIndex;

    public override void _Ready()
    {
        Visible = false;

        ConfigureSlider(_masterSlider);
        ConfigureSlider(_musicSlider);
        ConfigureSlider(_sfxSlider);
        ConfigureSensitivitySlider();
        PopulateResolutions();

        LoadSettings();

        _masterSlider.ValueChanged += OnMasterValueChanged;
        _musicSlider.ValueChanged += OnMusicValueChanged;
        _sfxSlider.ValueChanged += OnSfxValueChanged;

        if (_sensitivitySlider != null) _sensitivitySlider.ValueChanged += OnSensitivityChanged;
        if (_resolutionOption != null) _resolutionOption.ItemSelected += OnResolutionSelected;
        if (_fullscreenCheck != null) _fullscreenCheck.Toggled += OnFullscreenToggled;

        if (_closeButton != null)
            _closeButton.Pressed += Close;

        // Apply the saved window mode once, after the window is fully initialised.
        // The project boots windowed, so we only ever transition *into* the target
        // state (reliable cross-platform) instead of fighting a boot-fullscreen.
        Callable.From(ApplyStartupVideo).CallDeferred();
    }

    private void ApplyStartupVideo() => ApplyVideo(_startupFullscreen, _startupResolutionIndex);

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
    }

    public void Close()
    {
        if (!Visible)
            return;

        Visible = false;
        EmitSignal(SignalName.Closed);
    }

    private static void ConfigureSlider(HSlider slider)
    {
        if (slider == null)
            return;

        slider.MinValue = 0;
        slider.MaxValue = 100;
        slider.Step = 1;
    }

    private void LoadSettings()
    {
        _loading = true;

        var config = new ConfigFile();
        var err = config.Load(SettingsPath);

        if (err == Error.Ok)
        {
            SetSliderValue(_masterSlider, GetSavedValue(config, "master", GetBusValue("Master")));
            SetSliderValue(_musicSlider, GetSavedValue(config, "music", GetBusValue("Music")));
            SetSliderValue(_sfxSlider, GetSavedValue(config, "sfx", GetBusValue("SFX")));
        }
        else
        {
            SetSliderValue(_masterSlider, GetBusValue("Master"));
            SetSliderValue(_musicSlider, GetBusValue("Music"));
            SetSliderValue(_sfxSlider, GetBusValue("SFX"));
        }

        // Sensitivity + video load with sensible defaults whether or not the file existed.
        var sensitivity = Mathf.Clamp((float)config.GetValue(InputSection, "mouse_sensitivity", DefaultSensitivity),
            MinSensitivity, MaxSensitivity);
        MouseSensitivityMultiplier = sensitivity;
        if (_sensitivitySlider != null) _sensitivitySlider.Value = sensitivity;
        UpdateSensitivityLabel(sensitivity);

        var fullscreen = (bool)config.GetValue(VideoSection, "fullscreen", true);
        var resolutionIndex = Mathf.Clamp((int)config.GetValue(VideoSection, "resolution_index", DefaultResolutionIndex),
            0, Resolutions.Length - 1);
        if (_fullscreenCheck != null) _fullscreenCheck.ButtonPressed = fullscreen;
        if (_resolutionOption != null)
        {
            _resolutionOption.Selected = resolutionIndex;
            _resolutionOption.Disabled = fullscreen;
        }
        // Applied deferred from _Ready once the window exists (see ApplyStartupVideo).
        _startupFullscreen = fullscreen;
        _startupResolutionIndex = resolutionIndex;

        _loading = false;
        if (err == Error.Ok)
            ApplyAll();
        else
            UpdateLabels();
    }

    private static float GetSavedValue(ConfigFile config, string key, float defaultValue)
    {
        var value = (float)config.GetValue(AudioSection, key, defaultValue);
        return Mathf.Clamp(value, 0f, 100f);
    }

    private static void SetSliderValue(HSlider slider, float value)
    {
        if (slider != null)
            slider.Value = value;
    }

    private void OnMasterValueChanged(double value)
    {
        ApplyBus("Master", (float)value);
        UpdateLabel(_masterValueLabel, (float)value);
        SaveSettings();
    }

    private void OnMusicValueChanged(double value)
    {
        ApplyBus("Music", (float)value);
        UpdateLabel(_musicValueLabel, (float)value);
        SaveSettings();
    }

    private void OnSfxValueChanged(double value)
    {
        foreach (var busName in SfxBuses)
            ApplyBus(busName, (float)value);

        UpdateLabel(_sfxValueLabel, (float)value);
        SaveSettings();
    }

    private void ApplyAll()
    {
        var master = GetSliderValue(_masterSlider);
        var music = GetSliderValue(_musicSlider);
        var sfx = GetSliderValue(_sfxSlider);

        ApplyBus("Master", master);
        ApplyBus("Music", music);
        foreach (var busName in SfxBuses)
            ApplyBus(busName, sfx);

        UpdateLabel(_masterValueLabel, master);
        UpdateLabel(_musicValueLabel, music);
        UpdateLabel(_sfxValueLabel, sfx);
    }

    private static float GetSliderValue(HSlider slider)
    {
        return slider != null ? (float)slider.Value : 100f;
    }

    private static float GetBusValue(string busName)
    {
        var busIndex = AudioServer.GetBusIndex(busName);
        if (busIndex < 0)
            return 100f;

        var volumeDb = AudioServer.GetBusVolumeDb(busIndex);
        if (volumeDb <= MutedDb)
            return 0f;

        return Mathf.Clamp(Mathf.DbToLinear(volumeDb) * 100f, 0f, 100f);
    }

    private static void ApplyBus(string busName, float value)
    {
        var busIndex = AudioServer.GetBusIndex(busName);
        if (busIndex < 0)
        {
            GD.PrintErr($"{nameof(OptionsMenu)}: audio bus '{busName}' does not exist.");
            return;
        }

        AudioServer.SetBusVolumeDb(busIndex, value <= 0f ? MutedDb : Mathf.LinearToDb(value / 100f));
    }

    private static void UpdateLabel(Label label, float value)
    {
        if (label != null)
            label.Text = $"{Mathf.RoundToInt(value)}%";
    }

    private void UpdateLabels()
    {
        UpdateLabel(_masterValueLabel, GetSliderValue(_masterSlider));
        UpdateLabel(_musicValueLabel, GetSliderValue(_musicSlider));
        UpdateLabel(_sfxValueLabel, GetSliderValue(_sfxSlider));
    }

    private void SaveSettings()
    {
        if (_loading)
            return;

        var config = new ConfigFile();
        config.SetValue(AudioSection, "master", GetSliderValue(_masterSlider));
        config.SetValue(AudioSection, "music", GetSliderValue(_musicSlider));
        config.SetValue(AudioSection, "sfx", GetSliderValue(_sfxSlider));
        if (_sensitivitySlider != null)
            config.SetValue(InputSection, "mouse_sensitivity", (float)_sensitivitySlider.Value);
        if (_fullscreenCheck != null)
            config.SetValue(VideoSection, "fullscreen", _fullscreenCheck.ButtonPressed);
        if (_resolutionOption != null)
            config.SetValue(VideoSection, "resolution_index", _resolutionOption.Selected);
        config.Save(SettingsPath);
    }

    private void ConfigureSensitivitySlider()
    {
        if (_sensitivitySlider == null)
            return;

        _sensitivitySlider.MinValue = MinSensitivity;
        _sensitivitySlider.MaxValue = MaxSensitivity;
        _sensitivitySlider.Step = 0.05;
    }

    private void PopulateResolutions()
    {
        if (_resolutionOption == null)
            return;

        _resolutionOption.Clear();
        foreach (var r in Resolutions)
            _resolutionOption.AddItem($"{r.X} x {r.Y}");
    }

    private void OnSensitivityChanged(double value)
    {
        var multiplier = (float)value;
        MouseSensitivityMultiplier = multiplier;
        UpdateSensitivityLabel(multiplier);
        EventBus.Instance?.EmitSignal(EventBus.SignalName.MouseSensitivityChanged, multiplier);
        SaveSettings();
    }

    private void OnResolutionSelected(long index)
    {
        if (_fullscreenCheck == null || !_fullscreenCheck.ButtonPressed)
            ApplyVideo(false, (int)index);
        SaveSettings();
    }

    private void OnFullscreenToggled(bool pressed)
    {
        if (_resolutionOption != null)
            _resolutionOption.Disabled = pressed;
        ApplyVideo(pressed, _resolutionOption?.Selected ?? DefaultResolutionIndex);
        SaveSettings();
    }

    private static void ApplyVideo(bool fullscreen, int resolutionIndex)
    {
        if (fullscreen)
        {
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
            return;
        }

        resolutionIndex = Mathf.Clamp(resolutionIndex, 0, Resolutions.Length - 1);
        var size = Resolutions[resolutionIndex];
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        DisplayServer.WindowSetSize(size);

        var screenSize = DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());
        DisplayServer.WindowSetPosition((screenSize - size) / 2);
    }

    private void UpdateSensitivityLabel(float value)
    {
        if (_sensitivityValueLabel != null)
            _sensitivityValueLabel.Text = value.ToString("0.00");
    }
}
