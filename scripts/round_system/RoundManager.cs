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

    [Export] private float _timeTillRoundChangeIcon = 2.0f;
    [Export] private float _initialRoundStartTimer = 2.0f;
    [Export] private float _maxRoundStartTimer = 10.0f;
    [Export] private Node3D _player;
    [Export] private SpawnManager _spawnManager;
    [Export] private DifficultyManager _difficultyManager;

    
    private int _currRound = 1;

    public override void _Ready()
    {
        _spawnManager.InitTarget(_player);
        // _spawnManager.InitStats(_difficultyManager.CurrentZombieSpeed, _difficultyManager.CurrentZombieHealth,
        //     _difficultyManager.CurrentNumZombiesAliveLimit, _difficultyManager.CurrentNumZombiesRoundLimit,
        //     _difficultyManager.CurrentZombieSpawnTimerInterval);
        
        // Init with total zombies for the round being equal to max amount of zombies alive (round 1 balance)
        _spawnManager.InitStats(_difficultyManager.CurrentZombieSpeed, _difficultyManager.CurrentZombieHealth,
             _difficultyManager.CurrentNumZombiesAliveLimit, _difficultyManager.CurrentNumZombiesAliveLimit,
             _difficultyManager.CurrentZombieSpawnTimerInterval);

        _spawnManager.SpawnersDepleted += RoundOver;
    }

    private async void RoundOver()
    {
        await ToSignal(GetTree().CreateTimer(_timeTillRoundChangeIcon), SceneTreeTimer.SignalName.Timeout);
        EmitSignal(nameof(RoundFinished), _currRound + 1);
        AudioManager.I.RotateCombatSongIfReady();

        // Wait before starting next round
        float t = Mathf.Clamp((float)_currRound / 10.0f, 0f, 1f);
        float scaledGap = Mathf.Lerp(_initialRoundStartTimer, _maxRoundStartTimer, Mathf.Sqrt(t));
        await ToSignal(GetTree().CreateTimer(scaledGap), SceneTreeTimer.SignalName.Timeout);

        _currRound++;
        _difficultyManager.ScaleDifficulty();
        _spawnManager.InitStats(_difficultyManager.CurrentZombieSpeed, _difficultyManager.CurrentZombieHealth,
            _difficultyManager.CurrentNumZombiesAliveLimit, _difficultyManager.CurrentNumZombiesRoundLimit,
            _difficultyManager.CurrentZombieSpawnTimerInterval);

        EmitSignalRoundStarted();
    }
}
