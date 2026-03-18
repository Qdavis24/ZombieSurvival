using Godot;
using System;
using System.Collections.Generic;

namespace ZombieSurvival.scripts.zombie_package;

public partial class Zombie : CharacterBody3D
{
    [Signal] public delegate void DeadEventHandler();
    
    [Export] private NavigationAgent3D _navAgent;
    [Export] private Timer _simulationRunTimer;
    [Export] private Skeleton3D _skeleton;
    [Export] private PhysicalBoneSimulator3D _physicalBoneSimulator;
    [Export] private PackedScene _limbContainerPackedScene;
    [Export] private CollisionShape3D _collisionShape;
    [Export] private PackedScene _blood;

    private Node3D _target;
    private bool _isDead;

    private float _speed;
    private float _health;
    
    public void Init(Node3D target, ZombieStats stats)
    {
        _target = target;
        _health = stats.Health;
        _speed = stats.Speed;
    }

    public override void _Ready()
    {
        _simulationRunTimer.Timeout += QueueFree;

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
        if (_target == null || _isDead)
        {
 
            return;
        }

        _navAgent.SetTargetPosition(_target.GetPosition());
        var targetPos = _navAgent.GetNextPathPosition();
        var dir = (targetPos - GlobalTransform.Origin).Normalized();
        if (Mathf.Abs(Basis.Z.Dot(dir)) < .99f)
            LookAt(GlobalTransform.Origin + dir * 3f, Vector3.Up, useModelFront: true);
        Velocity = dir * _speed;
        MoveAndSlide();
    }

    private void DismemberBone(Godot.Collections.Array<DismemberableBone> destroyedDismemberableBones,
        Vector3 dir, float force, bool shouldDie)
    {
        var bodyParts = new List<RigidBody3D>();

        var limbContainer = _limbContainerPackedScene.Instantiate<LimbContainer>();
        GetTree().Root.AddChild(limbContainer);
        Vector3 bloodPosition = Vector3.Zero;
        for (int i = 0; i < destroyedDismemberableBones.Count; i++) // add the limbs to the scene tree and hinge them
        {
            var boneGlobalTransform = _isDead
                ? destroyedDismemberableBones[i].GlobalTransform
                : _skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(destroyedDismemberableBones[i].BoneIdx);
            if (i == 0)
                bloodPosition = boneGlobalTransform.Origin;
            var bodyPart = destroyedDismemberableBones[i].BodyPartPackedScene.Instantiate<RigidBody3D>();
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

        _skeleton.SetBonePoseScale(destroyedDismemberableBones[0].BoneIdx,
            Vector3.One * 0.01f); // shrink armature at root bone to "remove" the mesh

        bodyParts[0].ApplyImpulse(new Vector3(dir.X, .5f, dir.Z).Normalized() * force); // apply impulse to the root of the limb
        var blood = _blood.Instantiate<GpuParticles3D>();
        
        GetTree().Root.AddChild(blood);
        blood.GlobalPosition = bloodPosition;

        if (shouldDie && !_isDead)
        {
            _collisionShape.QueueFree();
            _isDead = true;
            _physicalBoneSimulator.PhysicalBonesStartSimulation();
            _simulationRunTimer.Start();
            EmitSignalDead();
        }
    }
}