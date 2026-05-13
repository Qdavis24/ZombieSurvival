using Godot;
using System;
using System.Collections.Generic;
using ZombieSurvival.scripts.shared;
using ZombieSurvival.scripts.zombie_package;


namespace ZombieSurvival.scripts.spawn_system;

public partial class SpawnManager : Node
{
    [Signal]
    public delegate void SpawnersDepletedEventHandler();

    [Export] private Timer _spawnInterval;

    private List<Spawner> _spawners = new();

    private int _numZombiesSpawned;
    private int _numZombiesAlive;
    public int NumZombiesAlive
    {
        get => _numZombiesAlive;
        set
        {
            _numZombiesAlive = value;
            // *** layer sound control ***
            if (_numZombiesAlive == 0)
            {
                AudioManager.I.PlayLayer1();
            } else if (_numZombiesAlive >= 5 && _numZombiesAlive <= 15)
            {
                AudioManager.I.PlayLayer2();
            } else if (_numZombiesAlive > 15)
            {
                AudioManager.I.PlayLayer3();
            }
        }
    }

    private SpawnerStats _stats;

    private Node3D _zombTarget;

    public override void _Ready()
    {
        _spawnInterval.Timeout += TriggerSpawns;
        EventBus.Instance.MapAreaUnlocked += RefreshSpawners;

        foreach (var child in GetChildren())
        {
            if (child is Spawner spawner)
            {
                spawner.ZombieDied += OnZombieDied;
                _spawners.Add(spawner);
            }
        }
        
        RefreshSpawners((int)MapArea.StartingZone);
    }

    private void RefreshSpawners(int mapArea)
    {
        foreach (Spawner spawner in _spawners)
        {
            if (spawner.Area == (MapArea)mapArea)
            {
                spawner.Unlocked =  true;
            }
        }
    }

    private void OnZombieDied()
    {
        NumZombiesAlive--;
        if (_numZombiesAlive == 0 && _numZombiesSpawned == _stats.NumZombiesRoundLimit) // depleted
        {
            _spawnInterval.Stop();
            EmitSignalSpawnersDepleted();
        }
    }


    public void InitStats(float zombieSpeed, float zombieHealth, int numZombiesAliveLimit, int numZombiesRoundLimit,
        float zombieSpawnTimerInterval)
    {
        _stats = new SpawnerStats
        {
            ZombieSpawnTimerInterval = zombieSpawnTimerInterval,
            NumZombiesAliveLimit = numZombiesAliveLimit,
            NumZombiesRoundLimit = numZombiesRoundLimit
        };

        var zombStats = new ZombieStats
        {
            Health = zombieHealth,
            Speed = zombieSpeed
        };

        foreach (var spawner in _spawners)
        {
            spawner.InitZombieStats(zombStats);
        }
        
        Reset();
    }

    public void InitTarget(Node3D zombieTarget)
    {
        _zombTarget = zombieTarget;
        foreach (var spawner in _spawners)
        {
            spawner.InitZombieTarget(zombieTarget);
        }
    }

    private void Reset()
    {
        NumZombiesAlive = 0;
        _numZombiesSpawned = 0;
        _spawnInterval.WaitTime = _stats.ZombieSpawnTimerInterval;
        _spawnInterval.Start();
    }

    private void TriggerSpawns()
    {
        for (int i = 0; i < _spawners.Count; i++)
        {
            var spawner = _spawners[i];
            if (!spawner.Unlocked) continue;
            if (NumZombiesAlive < _stats.NumZombiesAliveLimit && _numZombiesSpawned < _stats.NumZombiesRoundLimit)
            {
                spawner.SpawnZombie();
                _numZombiesSpawned++;
                NumZombiesAlive++;
            }
        }
    }
}
