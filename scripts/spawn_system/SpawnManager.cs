using Godot;
using System;
using System.Collections.Generic;
using ZombieSurvival.scripts.zombie_package;


namespace ZombieSurvival.scripts.spawn_system;

public partial class SpawnManager : Node
{
    [Signal]
    public delegate void SpawnersDepletedEventHandler();

    [Export] private Timer _spawnInterval;

    private List<Spawner> _spawners = new();

    private int _numZombiesAlive;
    private int _numZombiesSpawned;

    private SpawnerStats _stats;

    private Node3D _zombTarget;

    public override void _Ready()
    {
        _spawnInterval.Timeout += TriggerSpawns;

        foreach (var child in GetChildren())
        {
            if (child is Spawner spawner)
            {
                spawner.ZombieDied += OnZombieDied;
                _spawners.Add(spawner);
            }
        }
    }

    private void OnZombieDied()
    {
        _numZombiesAlive--;
        if (_numZombiesAlive == 0 && _numZombiesSpawned == _stats.NumZombiesLimit) // depleted
        {
            EmitSignalSpawnersDepleted();
            _spawnInterval.Stop();
        }
    }


    public void Init(SpawnerStats stats, Node3D zombieTarget, ZombieStats zombieStats)
    {
        _stats = stats;
        _zombTarget = zombieTarget;
        foreach (var spawner in _spawners)
        {
            spawner.InitZombieStats(zombieStats);
            spawner.InitZombieTarget(zombieTarget);
        }

        Reset();
    }

    private void Reset()
    {
        _spawnInterval.Start();
        _numZombiesAlive = 0;
        _numZombiesSpawned = 0;
    }

    private void TriggerSpawns()
    {
        _spawners.Sort((a, b) => (a.GlobalPosition - _zombTarget.GlobalPosition).Length()
            .CompareTo((b.GlobalPosition - _zombTarget.GlobalPosition).Length())); // sort by closest to player
        
        for (int i = 0; i < _spawners.Count/2; i++)
        {
            var spawner = _spawners[i];
            var distanceToTarget = (spawner.GlobalPosition - _zombTarget.GlobalPosition).Length();

            if (_numZombiesAlive < _stats.NumZombiesAliveLimit && _numZombiesSpawned < _stats.NumZombiesLimit)
            {
                spawner.SpawnZombie();
                _numZombiesSpawned++;
                _numZombiesAlive++;
            }
        }

    }
}