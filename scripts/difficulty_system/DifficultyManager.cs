using Godot;
using System;

namespace ZombieSurvival.scripts.difficulty_system;

public partial class DifficultyManager : Node
{
    [ExportCategory("Scale Amounts")] [Export]
    private float _zombieSpeedScaleAmount;
    [Export] private float _zombieHealthScaleAmount;
    [Export] private int _zombieAliveIncrement = 5;
    [Export] private float _zombieRoundTotalMultiplier = 2.0f;
    [Export] private float _zombieSpawnTimerIntervalScaleAmount;

    [ExportCategory("Limits")] [Export] private float _maxZombieSpeed;
    [Export] private float _maxZombieHealth;
    [Export] private int _maxNumZombiesAliveLimit;
    [Export] private int _maxNumZombiesRoundLimit;
    [Export] private float _zombieSpawnTimerIntervalLimit = 0.35f;

    [ExportCategory("Starting Stats")] [Export]
    private float _startingZombieSpeed;
    [Export] private float _startingZombieHealth;
    [Export] private int _startingNumZombiesAliveLimit;
    [Export] private int _startingNumZombiesRoundLimit;
    [Export] private float _startingZombieSpawnTimerInterval;

    public float CurrentZombieSpeed { get; private set; }
    public float CurrentZombieHealth { get; private set; }
    public int CurrentNumZombiesAliveLimit { get; private set; }
    public int CurrentNumZombiesRoundLimit { get; private set; }
    public float CurrentZombieSpawnTimerInterval { get; private set; }
    private int CurrentRound { get; set; } = 1;

    public override void _Ready()
    {
        CurrentZombieHealth = _startingZombieHealth;
        CurrentZombieSpeed = _startingZombieSpeed;
        CurrentNumZombiesAliveLimit = _startingNumZombiesAliveLimit;
        CurrentNumZombiesRoundLimit = _startingNumZombiesRoundLimit;
        CurrentZombieSpawnTimerInterval = _startingZombieSpawnTimerInterval;
        CurrentRound = 1;
    }

    public void ScaleDifficulty()
    {
        CurrentRound++;
        
        CurrentZombieHealth = Math.Clamp(CurrentZombieHealth * _zombieHealthScaleAmount, 0, _maxZombieHealth);
        CurrentZombieSpeed = Math.Clamp(CurrentZombieSpeed * _zombieSpeedScaleAmount, 0, _maxZombieSpeed);
        
        CurrentZombieSpawnTimerInterval = Math.Clamp(
            CurrentZombieSpawnTimerInterval * _zombieSpawnTimerIntervalScaleAmount,
            _zombieSpawnTimerIntervalLimit,
            float.MaxValue
        );

        if (CurrentRound > 3)
        {
            // Every round above 3 will grow by normal settings
            CurrentNumZombiesAliveLimit = Math.Clamp(
                CurrentNumZombiesAliveLimit + _zombieAliveIncrement,
                0,
                _maxNumZombiesAliveLimit
            );
            CurrentNumZombiesRoundLimit = Math.Clamp(
                (int)(CurrentNumZombiesAliveLimit * _zombieRoundTotalMultiplier),
                0,
                _maxNumZombiesRoundLimit
            );
        } else if (CurrentRound == 3)
        {
            // Round 3 will increment by 3, and use the total limit
            CurrentNumZombiesAliveLimit = _startingNumZombiesAliveLimit + 3;
            CurrentNumZombiesRoundLimit = Math.Clamp(
                (int)(CurrentNumZombiesAliveLimit * _zombieRoundTotalMultiplier),
                0,
                _maxNumZombiesRoundLimit
            );
            
        } else if (CurrentRound == 2)
        {
            // Round 2 will just be the starting amount, and use the total limit
            CurrentNumZombiesAliveLimit = _startingNumZombiesAliveLimit;
            CurrentNumZombiesRoundLimit = 10;
        }
        else
        {
            // Round 1 does not get triggered by ScaleDifficulty during normal flow.
            CurrentNumZombiesAliveLimit = _startingNumZombiesAliveLimit;
            CurrentNumZombiesRoundLimit = _startingNumZombiesRoundLimit;
        }
    }
}
