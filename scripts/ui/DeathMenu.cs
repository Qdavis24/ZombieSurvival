using Godot;
using System;

public partial class DeathMenu : CanvasLayer
{
	[Signal]
	public delegate void QuitButtonPressedEventHandler();

	[Export] private Button _quit;
	
	public override void _Ready()
	{
		_quit.Pressed += OnQUitButtonPressed;
	}

	private void OnQUitButtonPressed()
	{
		EmitSignal(SignalName.QuitButtonPressed);
		AudioManager.I.PlayUiClick();
	}
}
