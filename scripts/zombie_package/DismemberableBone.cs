using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
using Vector3 = Godot.Vector3;

public partial class DismemberableBone : PhysicalBone3D
{
    [Signal]
    public delegate void DestroyedEventHandler(Godot.Collections.Array<DismemberableBone> destroyedDismemberableBones,
        Vector3 dir, float force, bool shouldDie);

    [Export] public PackedScene BodyPartPackedScene;

    [Export] public DismemberableBone ChildBone;

    [Export] private bool _shouldDie;

    public int BoneIdx;
    
    private float _health = 100f;

    public override void _Ready()
    {
        BoneIdx = GetBoneId();
    }


    private void Cleanup(Godot.Collections.Array<DismemberableBone> destroyedDismemberableBones)
    {
        foreach (var dBone in destroyedDismemberableBones) dBone.QueueFree();
    }


    public void TakeDamage(float damage, Vector3 dir, float force)
    {
        _health -= damage;

        if (_health <= 0f)
        {
            Die(dir, force);
        }
    }

    private void Die(Vector3 dir, float force)
    {
        var destroyedDismemberableBones = CollectBoneChain();
        EmitSignalDestroyed(destroyedDismemberableBones, dir, force, _shouldDie);
        Cleanup(destroyedDismemberableBones);
    }

    public Godot.Collections.Array<DismemberableBone> CollectBoneChain()
    {
        var destroyedDismemberableBones = new Godot.Collections.Array<DismemberableBone>();

        var currBone = this;
        while (currBone != null && IsInstanceValid(currBone))
        {
            destroyedDismemberableBones.Add(currBone);
            currBone = currBone.ChildBone;
        }

        return destroyedDismemberableBones;
    }
}