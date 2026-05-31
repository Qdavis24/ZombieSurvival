using Godot;
using System;

public partial class DeathMenu : CanvasLayer
{
	[Signal]
	public delegate void QuitButtonPressedEventHandler();

	[Export] private Button _quit;
	[Export] private Label _roundLabel;
	[Export] private Label _killsLabel;
	[Export] private Label _headshotsLabel;

	public override void _Ready()
	{
		_quit.Pressed += OnQUitButtonPressed;
	}

	public void SetStats(int round, int kills, int headshots)
	{
		if (_roundLabel != null) _roundLabel.Text = $"Round Reached: {round}";
		if (_killsLabel != null) _killsLabel.Text = $"Zombies Killed: {kills}";
		if (_headshotsLabel != null) _headshotsLabel.Text = $"Headshots: {headshots}";
	}

	private void OnQUitButtonPressed()
	{
		EmitSignal(SignalName.QuitButtonPressed);
		AudioManager.I.PlayUiClick();
	}
}
