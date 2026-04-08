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

    public bool Unlocked;
    private Node3D _zombTarget;
    private  ZombieStats _zombStats;
    
    public void SpawnZombie()
    {
        var zomb = _zombiePackedScene.Instantiate<Zombie>();
        zomb.Dead += EmitSignalZombieDied;
        zomb.Init(_zombTarget, _zombStats);
        AddChild(zomb);
        zomb.GlobalTransform = GlobalTransform * new Transform3D(
            Basis,
            new Vector3(GD.Randf() * _offset, 0, GD.Randf() * _offset)
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
}