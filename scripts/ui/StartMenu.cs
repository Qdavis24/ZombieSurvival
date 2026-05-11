using Godot;
using System;

public partial class StartMenu : CanvasLayer
{
	[Signal]
	public delegate void StartGamePressedEventHandler();
	[Signal]
	public delegate void OptionsPressedEventHandler();
	[Signal]
	public delegate void ControlsPressedEventHandler();

	[Export] private Button _startGameButton;
	[Export] private Button _optionsButton;
	[Export] private Button _controlsButton;

	public override void _Ready()
	{
		_startGameButton.Pressed += OnStartGamePressed;
		_optionsButton.Pressed += OnOptionsPressed;
		_controlsButton.Pressed += OnControlsPressed;
	}

	private void OnStartGamePressed()
	{
		EmitSignal(SignalName.StartGamePressed);
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
}
