using Godot;
using System;
using ZombieSurvival.scripts.difficulty_system;
using ZombieSurvival.scripts.spawn_system;

namespace ZombieSurvival.scripts.round_system;

public partial class RoundManager : Node
{
    [Signal]
    public delegate void RoundFinishedEventHandler();

    [Signal]
    public delegate void RoundStartedEventHandler();

    [Export] private Node3D _player;
    [Export] private SpawnManager _spawnManager;
    [Export] private DifficultyManager _difficultyManager;

    private int _currRound;

    public override void _Ready()
    {
        _spawnManager.InitTarget(_player);
        _spawnManager.InitStats(_difficultyManager.CurrentZombieSpeed, _difficultyManager.CurrentZombieHealth,
            _difficultyManager.CurrentNumZombiesAliveLimit, _difficultyManager.CurrentNumZombiesRoundLimit,
            _difficultyManager.CurrentZombieSpawnTimerInterval);

        _spawnManager.SpawnersDepleted += RoundOver;
    }

    private void RoundOver()
    {
        EmitSignalRoundFinished();
        _currRound++;
        _difficultyManager.ScaleDifficulty();
        _spawnManager.InitStats(_difficultyManager.CurrentZombieSpeed, _difficultyManager.CurrentZombieHealth,
            _difficultyManager.CurrentNumZombiesAliveLimit, _difficultyManager.CurrentNumZombiesRoundLimit,
            _difficultyManager.CurrentZombieSpawnTimerInterval);
        EmitSignalRoundStarted();
    }
}