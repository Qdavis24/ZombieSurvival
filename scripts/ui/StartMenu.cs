using Godot;
using System;

public partial class StartMenu : CanvasLayer
{
	[Signal]
	public delegate void StartGamePressedEventHandler();

	[Export] private Button _startGameButton;

	public override void _Ready()
	{
		_startGameButton.Pressed += OnStartGamePressed;
	}

	private void OnStartGamePressed()
	{
		EmitSignal(SignalName.StartGamePressed);
	}
}
