using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
using ZombieSurvival.scripts.zombie_package;
using Vector3 = Godot.Vector3;

namespace ZombieSurvival.scripts.zombie_package;

public partial class DismemberableBodyPart : BodyPart
{
    [Signal]
    public delegate void DismemberEventHandler(
        Godot.Collections.Array<DismemberableBodyPart> destroyedDismemberableBodyParts, Vector3 dir, float force);

    [Export] public PackedScene BodyPartPackedScene;

    [Export] public DismemberableBodyPart ChildBodyPart;

    protected override void OnDestroyed(Vector3 dir, float force)
    {
        if (_destroyed) return;
        var destroyedDismemberableBodyParts = CollectBoneChain();
        EmitSignalDismember(destroyedDismemberableBodyParts, dir, force);
        Cleanup(destroyedDismemberableBodyParts);
        base.OnDestroyed(dir, force);
    }

    private void Cleanup(Godot.Collections.Array<DismemberableBodyPart> destroyedDismemberableBones)
    {
        foreach (var dBone in destroyedDismemberableBones) dBone.QueueFree();
    }


    private Godot.Collections.Array<DismemberableBodyPart> CollectBoneChain()
    {
        var destroyedDismemberableBones = new Godot.Collections.Array<DismemberableBodyPart>();

        var currBone = this;
        while (currBone != null && IsInstanceValid(currBone))
        {
            destroyedDismemberableBones.Add(currBone);
            currBone = currBone.ChildBodyPart;
        }

        return destroyedDismemberableBones;
    }
}