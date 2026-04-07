using Godot;
using System;
using ZombieSurvival.scripts.difficulty_system;
using ZombieSurvival.scripts.spawn_system;

namespace ZombieSurvival.scripts.round_system;

public partial class RoundManager : Node
{
    [Signal]
    public delegate void RoundFinishedEventHandler(int round);

    [Signal]
    public delegate void RoundStartedEventHandler();

    [Export] private float _roundEndGapTime = 2.0f;
    [Export] private float _roundStartGapTime = 2.0f;
    [Export] private Node3D _player;
    [Export] private SpawnManager _spawnManager;
    [Export] private DifficultyManager _difficultyManager;

    private int _currRound = 1;

    public override void _Ready()
    {
        _spawnManager.InitTarget(_player);
        _spawnManager.InitStats(_difficultyManager.CurrentZombieSpeed, _difficultyManager.CurrentZombieHealth,
            _difficultyManager.CurrentNumZombiesAliveLimit, _difficultyManager.CurrentNumZombiesRoundLimit,
            _difficultyManager.CurrentZombieSpawnTimerInterval);

        _spawnManager.SpawnersDepleted += RoundOver;
    }

    private async void RoundOver()
    {
        await ToSignal(GetTree().CreateTimer(_roundEndGapTime), SceneTreeTimer.SignalName.Timeout);
        EmitSignal(nameof(RoundFinished), _currRound + 1);

        // Wait before starting next round
        await ToSignal(GetTree().CreateTimer(_roundStartGapTime), SceneTreeTimer.SignalName.Timeout);

        _currRound++;
        _difficultyManager.ScaleDifficulty();
        _spawnManager.InitStats(_difficultyManager.CurrentZombieSpeed, _difficultyManager.CurrentZombieHealth,
            _difficultyManager.CurrentNumZombiesAliveLimit, _difficultyManager.CurrentNumZombiesRoundLimit,
            _difficultyManager.CurrentZombieSpawnTimerInterval);

        EmitSignalRoundStarted();
    }
}