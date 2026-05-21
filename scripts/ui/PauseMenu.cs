using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	[Signal] public delegate void QuitButtonPressedEventHandler();
	[Signal] public delegate void OptionsButtonPressedEventHandler();
	
	[Export] private Button _quit;
	[Export] private Button _options;
	
	[Export] private Button _next;
	[Export] private Label _curr;
	[Export] private Button _prev;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_quit.Pressed += OnQuitButtonPressed;
		_options.Pressed += OnOptionsButtonPressed;
		_next.Pressed += OnNextButtonPressed;
		_prev.Pressed += OnPrevButtonPressed;
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
	
	private void OnNextButtonPressed()
	{
		AudioManager.I.PlayUiClick();
		AudioManager.I.NextSong();
		SetCurrentSong();
	}
	private void OnPrevButtonPressed()
	{
		AudioManager.I.PlayUiClick();
		AudioManager.I.PrevSong();
		SetCurrentSong();
	}
	
	public void SetCurrentSong()
	{
		if (!AudioManager.I.HasCombatSongs())
		{
			_curr.Text = "--";
			return;
		}

		var curr = AudioManager.I.GetCurrentSong() + 1;
		_curr.Text = "#" + curr;
	}

}
