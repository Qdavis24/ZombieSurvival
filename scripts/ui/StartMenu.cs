using Godot;
using System;

public partial class StartMenu : CanvasLayer
{
	[Signal]
	public delegate void StartGamePressedEventHandler();
	[Signal]
	public delegate void OptionsPressedEventHandler();

	[Export] private Button _startGameButton;
	[Export] private Button _optionsButton;

	public override void _Ready()
	{
		_startGameButton.Pressed += OnStartGamePressed;
		_optionsButton.Pressed += OnOptionsPressed;
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
}
