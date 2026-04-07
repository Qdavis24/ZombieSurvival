using Godot;
using System;
using ZombieSurvival.scripts;
using ZombieSurvival.scripts.round_system;

public partial class Game : Node3D
{
	[Export] public PlayerController Player;
	[Export] public RoundManager RoundManager;
	
	private GameManager _gameManager;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		RoundManager.RoundFinished += OnRoundFinished;
		_gameManager = GetParent<GameManager>();
		Player.PlayerDied += _gameManager.PlayerDied;
	}

	private void OnRoundFinished(int round)
	{
		_gameManager.SetRound(round);
	}
}
