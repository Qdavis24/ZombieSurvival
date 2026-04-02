using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	[Signal] public delegate void QuitButtonPressedEventHandler();
	[Signal] public delegate void OptionsButtonPressedEventHandler();
	
	[Export] private Button _quit;
	[Export] private Button _options;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_quit.Pressed += OnQuitButtonPressed;
		_options.Pressed += OnOptionsButtonPressed;
	}

	public void OnQuitButtonPressed()
	{
		EmitSignal(SignalName.QuitButtonPressed);
		AudioManager.I.PlayUiClick();
	}
	
	public void OnOptionsButtonPressed()
	{
		EmitSignal(SignalName.OptionsButtonPressed);
		AudioManager.I.PlayUiClick();
	}
}
