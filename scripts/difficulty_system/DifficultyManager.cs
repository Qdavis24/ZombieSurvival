using Godot;
using System;

namespace ZombieSurvival.scripts.difficulty_system;
public partial class DifficultyManager : Node
{
    [Signal]
    public delegate void DifficultyScaledEventHandler(float zombieSpeed, float zombieHealth, int numZombiesAliveLimit, int numZombiesRoundLimit);
    
    public (int numZombiesAliveLimit, int numZombiesRoundLimit) CurrentSpawnerDifficulty => 
        (_currentNumZombiesAliveLimit, _currentNumZombiesRoundLimit);

    public (float zombieSpeed, float zombieHealth) CurrentZombieDifficulty => 
        (_currentZombieSpeed, _currentZombieHealth);
    
    [ExportCategory("Scale Amounts")]
    [Export] private float _zombieSpeedScaleAmount;
    [Export] private float _zombieHealthScaleAmount;
    [Export] private float _numZombiesAliveLimitScaleAmount;
    [Export] private float _numZombiesRoundLimitScaleAmount;
    
    [ExportGroup("Limits")]
    [Export] private float _maxZombieSpeed;
    [Export] private float _maxZombieHealth;
    [Export] private int _maxNumZombiesAliveLimit;
    [Export] private int _maxNumZombiesRoundLimit;
    
    [ExportCategory("Starting Stats")]
    [Export] private float _startingZombieSpeed;
    [Export] private float _startingZombieHealth;
    [Export] private int _startingNumZombiesAliveLimit;
    [Export] private int _startingNumZombiesRoundLimit;

    private float _currentZombieSpeed;
    public float CurrentZombieSpeed => _currentZombieSpeed;

    private float _currentZombieHealth;
    public float CurrentZombieHealth => _currentZombieHealth;

    private int _currentNumZombiesAliveLimit;
    public int CurrentNumZombiesAliveLimit => _currentNumZombiesAliveLimit;

    private int _currentNumZombiesRoundLimit;
    public int CurrentNumZombiesRoundLimit => _currentNumZombiesRoundLimit;

    public override void _Ready()
    {
        _currentZombieHealth = _startingZombieHealth;
        _currentZombieSpeed = _startingZombieSpeed;
        _currentNumZombiesAliveLimit = _startingNumZombiesAliveLimit;
        _currentNumZombiesRoundLimit = _startingNumZombiesRoundLimit;
    }

    public void ScaleDifficulty()
    {
        _currentZombieHealth = Math.Clamp(_currentZombieHealth * _zombieHealthScaleAmount, 0, _maxZombieHealth);
        _currentZombieSpeed = Math.Clamp(_currentZombieSpeed * _zombieSpeedScaleAmount, 0, _maxZombieSpeed);
        _currentNumZombiesAliveLimit = (int)Math.Clamp(_currentNumZombiesAliveLimit * _numZombiesAliveLimitScaleAmount, 0, _maxNumZombiesAliveLimit);
        _currentNumZombiesRoundLimit = (int)Math.Clamp(_currentNumZombiesRoundLimit * _numZombiesRoundLimitScaleAmount, 0, _maxNumZombiesRoundLimit);
        
        EmitSignalDifficultyScaled(_currentZombieSpeed, _currentZombieHealth, _currentNumZombiesAliveLimit, _currentNumZombiesRoundLimit);
    }
    
}
