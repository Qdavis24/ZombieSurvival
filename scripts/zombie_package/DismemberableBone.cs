using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
using Vector3 = Godot.Vector3;

public partial class DismemberableBone : PhysicalBone3D
{
    [Signal]
    public delegate void DestroyedEventHandler(Godot.Collections.Array<PackedScene> packedScenes,
        Godot.Collections.Array<int> boneIdxs, Vector3 dir, float force, bool shouldDie);

    [Export] public PackedScene BodyPartPackedScene;

    [Export] public DismemberableBone ChildBone;

    [Export] private bool _shouldDie;

    private Godot.Collections.Array<PackedScene> _packedScenes;
    private Godot.Collections.Array<int> _boneIdxs;

    private float _health = 100f;
    
    

    private void Cleanup()
    {
        QueueFree();
        var currBone = ChildBone;
        while (currBone != null)
        {
            currBone.QueueFree();
            currBone = currBone.ChildBone;
        }
    }


    public void TakeDamage(float damage, Vector3 dir, float force)
    {
        _health -= damage;

        if (_health <= 0f)
        {
            CollectBoneChain();
            Die(dir, force);
        }
    }

    private void Die(Vector3 dir, float force)
    {
        EmitSignalDestroyed(_packedScenes, _boneIdxs, dir, force, _shouldDie);
        Cleanup();
    }

    private void CollectBoneChain()
    {
        var boneIdxs = new Godot.Collections.Array<int>();
        var packedScenes = new Godot.Collections.Array<PackedScene>();

        var currBone = this;
        while (currBone != null && IsInstanceValid(currBone))
        {
            boneIdxs.Add(currBone.GetBoneId());
            packedScenes.Add(currBone.BodyPartPackedScene);
            currBone = currBone.ChildBone;
        }

        _boneIdxs = boneIdxs;
        _packedScenes = packedScenes;

    }
}