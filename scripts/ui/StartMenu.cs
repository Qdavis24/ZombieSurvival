using Godot;
using System;

public partial class StartMenu : CanvasLayer
{
	[Signal]
	public delegate void StartGamePressedEventHandler(int levelIndex);
	[Signal]
	public delegate void OptionsPressedEventHandler();
	[Signal]
	public delegate void ControlsPressedEventHandler();

	[Export] private Button _startGameButton;
	[Export] private Button _previousLevelButton;
	[Export] private Button _nextLevelButton;
	[Export] private Button _optionsButton;
	[Export] private Button _controlsButton;
	[Export] private Label _selectedLevelLabel;

	private readonly string[] _levelNames = { "Downtown", "Bunker", "Forest" };
	private int _selectedLevelIndex;

	public override void _Ready()
	{
		_startGameButton.Pressed += OnStartGamePressed;
		_previousLevelButton.Pressed += OnPreviousLevelPressed;
		_nextLevelButton.Pressed += OnNextLevelPressed;
		_optionsButton.Pressed += OnOptionsPressed;
		_controlsButton.Pressed += OnControlsPressed;
		UpdateSelectedLevelLabel();
	}

	private void OnStartGamePressed()
	{
		EmitSignal(SignalName.StartGamePressed, _selectedLevelIndex);
		AudioManager.I.PlayUiClick();
	}

	private void OnOptionsPressed()
	{
		EmitSignal(SignalName.OptionsPressed);
		AudioManager.I.PlayUiClick();
	}

	private void OnControlsPressed()
	{
		EmitSignal(SignalName.ControlsPressed);
		AudioManager.I.PlayUiClick();
	}

	private void OnPreviousLevelPressed()
	{
		_selectedLevelIndex--;
		if (_selectedLevelIndex < 0)
			_selectedLevelIndex = _levelNames.Length - 1;
		UpdateSelectedLevelLabel();
		AudioManager.I.PlayUiClick();
	}

	private void OnNextLevelPressed()
	{
		_selectedLevelIndex = (_selectedLevelIndex + 1) % _levelNames.Length;
		UpdateSelectedLevelLabel();
		AudioManager.I.PlayUiClick();
	}

	private void UpdateSelectedLevelLabel()
	{
		_selectedLevelLabel.Text = _levelNames[_selectedLevelIndex];
	}
}
