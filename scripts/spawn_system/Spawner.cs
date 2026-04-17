using Godot;
using System;
using ZombieSurvival.scripts.shared;
using ZombieSurvival.scripts.zombie_package;

public partial class Spawner : Node3D
{
    [Signal]
    public delegate void ZombieDiedEventHandler();

    [Export] public MapArea Area;
    [Export] private PackedScene _zombiePackedScene;
    [Export] private float _offset = 0f;
    [Export] private float _randomOffset = 40f;

    [Export] private MeshInstance3D _smokeMesh;

    private float _time;
    [Export] private float _smokeScaleAmplitude = 0.1f;
    [Export] private float _smokeScaleSpeed = 1.5f;
    private Vector3 _smokeBaseScale;

    public bool Unlocked;
    private Node3D _zombTarget;
    private  ZombieStats _zombStats;
    
    public void SpawnZombie()
    {
        var zomb = _zombiePackedScene.Instantiate<Zombie>();
        zomb.Dead += EmitSignalZombieDied;
        zomb.Init(_zombTarget, _zombStats);
        AddChild(zomb);
        float randX = _offset + (float)GD.RandRange(-_randomOffset, _randomOffset);
        float randZ = _offset + (float)GD.RandRange(-_randomOffset, _randomOffset);

        zomb.GlobalTransform = GlobalTransform * new Transform3D(
            Basis,
            new Vector3(randX, 0, randZ)
        );
    }

    public void InitZombieStats(ZombieStats zombStats)
    {
        _zombStats = zombStats;
    }

    public void InitZombieTarget(Node3D zombTarget)
    {
        _zombTarget = zombTarget;
    }
    public override void _Ready()
    {
        if (_smokeMesh != null)
            _smokeBaseScale = _smokeMesh.Scale;
    }

    public override void _Process(double delta)
    {
        if (_smokeMesh == null) return;

        _time += (float)delta * _smokeScaleSpeed;
        float scaleOffset = Mathf.Sin(_time) * _smokeScaleAmplitude;

        _smokeMesh.Scale = _smokeBaseScale * (1.0f + scaleOffset);
    }
}