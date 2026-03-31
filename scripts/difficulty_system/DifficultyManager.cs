using Godot;
using System;

namespace ZombieSurvival.scripts.difficulty_system;

public partial class DifficultyManager : Node
{
    [ExportCategory("Scale Amounts")] [Export]
    private float _zombieSpeedScaleAmount;
    [Export] private float _zombieHealthScaleAmount;
    [Export] private float _numZombiesAliveLimitScaleAmount;
    [Export] private float _numZombiesRoundLimitScaleAmount;
    [Export] private float _zombieSpawnTimerIntervalScaleAmount;

    [ExportCategory("Limits")] [Export] private float _maxZombieSpeed;
    [Export] private float _maxZombieHealth;
    [Export] private int _maxNumZombiesAliveLimit;
    [Export] private int _maxNumZombiesRoundLimit;
    [Export] private float _zombieSpawnTimerIntervalLimit;

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

    public override void _Ready()
    {
        CurrentZombieHealth = _startingZombieHealth;
        CurrentZombieSpeed = _startingZombieSpeed;
        CurrentNumZombiesAliveLimit = _startingNumZombiesAliveLimit;
        CurrentNumZombiesRoundLimit = _startingNumZombiesRoundLimit;
        CurrentZombieSpawnTimerInterval = _startingZombieSpawnTimerInterval;
    }

    public void ScaleDifficulty()
    {
        CurrentZombieHealth = Math.Clamp(CurrentZombieHealth * _zombieHealthScaleAmount, 0, _maxZombieHealth);
        CurrentZombieSpeed = Math.Clamp(CurrentZombieSpeed * _zombieSpeedScaleAmount, 0, _maxZombieSpeed);
        CurrentNumZombiesAliveLimit = (int)Math.Clamp(CurrentNumZombiesAliveLimit * _numZombiesAliveLimitScaleAmount,
            0, _maxNumZombiesAliveLimit);
        CurrentNumZombiesRoundLimit = (int)Math.Clamp(CurrentNumZombiesRoundLimit * _numZombiesRoundLimitScaleAmount,
            0, _maxNumZombiesRoundLimit);
        CurrentZombieSpawnTimerInterval =
            Math.Clamp(CurrentZombieSpawnTimerInterval * _zombieSpawnTimerIntervalScaleAmount,
                _zombieSpawnTimerIntervalLimit, int.MaxValue);
    }
}