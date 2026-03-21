using Godot;
using System;

namespace ZombieSurvival.scripts.difficulty_system;

public partial class DifficultyManager : Node
{
    [Signal]
    public delegate void DifficultyScaledEventHandler(float zombieSpeed, float zombieHealth, int numZombiesAliveLimit,
        int numZombiesRoundLimit, float zombieSpawnTimerInterval);

    public (int numZombiesAliveLimit, int numZombiesRoundLimit) CurrentSpawnerDifficulty =>
        (_currentNumZombiesAliveLimit, _currentNumZombiesRoundLimit);

    public (float zombieSpeed, float zombieHealth) CurrentZombieDifficulty =>
        (_currentZombieSpeed, _currentZombieHealth);

    [ExportCategory("Scale Amounts")] [Export]
    private float _zombieSpeedScaleAmount;

    [Export] private float _zombieHealthScaleAmount;
    [Export] private float _numZombiesAliveLimitScaleAmount;
    [Export] private float _numZombiesRoundLimitScaleAmount;
    [Export] private float _zombieSpawnTimerIntervalScaleAmount;

    [ExportGroup("Limits")] [Export] private float _maxZombieSpeed;
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

    private float _currentZombieSpeed;
    public float CurrentZombieSpeed => _currentZombieSpeed;

    private float _currentZombieHealth;
    public float CurrentZombieHealth => _currentZombieHealth;

    private int _currentNumZombiesAliveLimit;
    public int CurrentNumZombiesAliveLimit => _currentNumZombiesAliveLimit;

    private int _currentNumZombiesRoundLimit;
    public int CurrentNumZombiesRoundLimit => _currentNumZombiesRoundLimit;

    private float _currentZombieSpawnTimerInterval;

    public float CurrentZombieSpawnTimerInterval => _currentZombieSpawnTimerInterval;

    public override void _Ready()
    {
        _currentZombieHealth = _startingZombieHealth;
        _currentZombieSpeed = _startingZombieSpeed;
        _currentNumZombiesAliveLimit = _startingNumZombiesAliveLimit;
        _currentNumZombiesRoundLimit = _startingNumZombiesRoundLimit;
        _currentZombieSpawnTimerInterval = _startingZombieSpawnTimerInterval;
    }

    public void ScaleDifficulty()
    {
        _currentZombieHealth = Math.Clamp(_currentZombieHealth * _zombieHealthScaleAmount, 0, _maxZombieHealth);
        _currentZombieSpeed = Math.Clamp(_currentZombieSpeed * _zombieSpeedScaleAmount, 0, _maxZombieSpeed);
        _currentNumZombiesAliveLimit = (int)Math.Clamp(_currentNumZombiesAliveLimit * _numZombiesAliveLimitScaleAmount,
            0, _maxNumZombiesAliveLimit);
        _currentNumZombiesRoundLimit = (int)Math.Clamp(_currentNumZombiesRoundLimit * _numZombiesRoundLimitScaleAmount,
            0, _maxNumZombiesRoundLimit);
        _currentZombieSpawnTimerInterval =
            Math.Clamp(_currentZombieSpawnTimerInterval * _zombieSpawnTimerIntervalScaleAmount,
                _zombieSpawnTimerIntervalLimit, int.MaxValue);

        EmitSignalDifficultyScaled(_currentZombieSpeed, _currentZombieHealth, _currentNumZombiesAliveLimit,
            _currentNumZombiesRoundLimit, _currentZombieSpawnTimerInterval);
    }
}