using Godot;
using System;

public partial class StartMenu : Node
{
	[Export] private AudioStream _startMenuMusic;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		AudioManager.I.PlayMusic(_startMenuMusic);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
