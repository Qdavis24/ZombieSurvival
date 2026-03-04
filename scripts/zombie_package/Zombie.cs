using Godot;
using System;
using System.Collections.Generic;

public partial class Zombie : CharacterBody3D
{
    [Export] private Node3D _target;
    [Export] private float _speed = 2f;
    [Export] private Skeleton3D _skeleton;
    [Export] private PhysicalBoneSimulator3D _physicalBoneSimulator;
    [Export] private PackedScene _limbContainerPackedScene;

    public override void _Ready()
    {
        foreach (Node child in _physicalBoneSimulator.GetChildren())
        {
            if (child is DismemberableBone physicalBone)
            {
                physicalBone.Destroyed += DismemberBone;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        var dir = Vector3.Zero;
        if (_target != null)
        {
            dir = _target.GlobalPosition - _physicalBoneSimulator.GlobalPosition;
            
        }

        Velocity = dir * _speed;
        MoveAndSlide();
    }

    public void DismemberBone(Godot.Collections.Array<PackedScene> packedScenes, Godot.Collections.Array<int> boneIdxs,
        Vector3 dir, float force, bool shouldDie)
    {
        var bodyParts = new List<RigidBody3D>();

        var limbContainer = _limbContainerPackedScene.Instantiate<LimbContainer>();
        GetTree().Root.AddChild(limbContainer);

        for (int i = 0; i < boneIdxs.Count; i++) // add the limbs to the scene tree and hinge them
        {
            var boneGlobalTransform = _skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(boneIdxs[i]);

            var bodyPart = packedScenes[i].Instantiate<RigidBody3D>();
            limbContainer.AddChild(bodyPart);
            bodyPart.GlobalTransform = boneGlobalTransform;

            bodyParts.Add(bodyPart);

            if (i > 0)
            {
                var hinge = limbContainer.Hinge.Instantiate<HingeJoint3D>();
                limbContainer.AddChild(hinge);
                hinge.GlobalTransform = boneGlobalTransform;
                hinge.NodeA = bodyParts[i - 1].GetPath();
                hinge.NodeB = bodyPart.GetPath();
            }
        }

       
        _skeleton.SetBonePoseScale(boneIdxs[0], Vector3.One * 0.01f); // shrink armature bone to "remove" the mesh
        

        bodyParts[0].ApplyImpulse(dir * force); // apply impulse to the root of the limb

        if (shouldDie)
        {
            _physicalBoneSimulator.PhysicalBonesStartSimulation();
        }
    }
}