using Godot;

public partial class OptionsMenu : CanvasLayer
{
    [Signal] public delegate void ClosedEventHandler();

    private const string SettingsPath = "user://audio_settings.cfg";
    private const string AudioSection = "audio";
    private const float MutedDb = -80f;

    private static readonly string[] SfxBuses = ["SFX", "Explosion"];

    [Export] private HSlider _masterSlider;
    [Export] private HSlider _musicSlider;
    [Export] private HSlider _sfxSlider;
    [Export] private Label _masterValueLabel;
    [Export] private Label _musicValueLabel;
    [Export] private Label _sfxValueLabel;
    [Export] private Button _closeButton;

    private bool _loading;

    public override void _Ready()
    {
        Visible = false;

        ConfigureSlider(_masterSlider);
        ConfigureSlider(_musicSlider);
        ConfigureSlider(_sfxSlider);

        LoadSettings();

        _masterSlider.ValueChanged += OnMasterValueChanged;
        _musicSlider.ValueChanged += OnMusicValueChanged;
        _sfxSlider.ValueChanged += OnSfxValueChanged;

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
        config.Save(SettingsPath);
    }
}
