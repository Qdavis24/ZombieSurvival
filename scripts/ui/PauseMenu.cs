using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	[Signal] public delegate void QuitButtonPressedEventHandler();
	
	[Export] private Button _quit;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_quit.Pressed += OnQuitButtonPressed;
	}

	public void OnQuitButtonPressed()
	{
		EmitSignal(SignalName.QuitButtonPressed);

	}
}
