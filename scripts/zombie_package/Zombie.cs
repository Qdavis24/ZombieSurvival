using Godot;
using System;
using System.Collections.Generic;

namespace ZombieSurvival.scripts.zombie_package;

public partial class Zombie : CharacterBody3D
{
    [Signal]
    public delegate void DeadEventHandler();

    [ExportCategory("Limb Health Ratios")] [Export]
    private float _headHealthRatio;

    [Export] private float _upperArmHealthRatio;
    [Export] private float _lowerArmHealthRatio;
    [Export] private float _upperLegHealthRatio;
    [Export] private float _lowerLegHealthRatio;
    [Export] private float _torsoHealthRatio;

    [ExportCategory("Limb Damage Multipliers")] [Export]
    private float _headDamageMultiplier;

    [Export] private float _upperArmDamageMultiplier;
    [Export] private float _lowerArmDamageMultiplier;
    [Export] private float _upperLegDamageMultiplier;
    [Export] private float _lowerLegDamageMultiplier;
    [Export] private float _torsoDamageMultiplier;

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

    private List<BodyPart> _limbs = new();
    private Dictionary<Limb, float> _damageMultipliers;
    private Dictionary<Limb, float> _healthRatios;

    public void Init(Node3D target, ZombieStats stats)
    {
        _target = target;
        _health = stats.Health;
        _speed = stats.Speed;
    }

    public override void _Ready()
    {
        _damageMultipliers = new()
        {
            { Limb.Head, _headDamageMultiplier },
            { Limb.UpperArm, _upperArmDamageMultiplier },
            { Limb.LowerArm, _lowerArmDamageMultiplier },
            { Limb.UpperLeg, _upperLegDamageMultiplier },
            { Limb.LowerLeg, _lowerLegDamageMultiplier },
            { Limb.Torso, _torsoDamageMultiplier }
        };

        _healthRatios = new()
        {
            { Limb.Head, _headHealthRatio },
            { Limb.UpperArm, _upperArmHealthRatio },
            { Limb.LowerArm, _lowerArmHealthRatio },
            { Limb.UpperLeg, _upperLegHealthRatio },
            { Limb.LowerLeg, _lowerLegHealthRatio },
            { Limb.Torso, _torsoHealthRatio }
        };

        _simulationRunTimer.Timeout += QueueFree;

        foreach (var child in _physicalBoneSimulator.GetChildren())
        {
            if (child is not BodyPart bodyPart) continue;

            bodyPart.Destroyed += OnBodyPartDestroyed;
            bodyPart.TookDamage += OnBodyPartTookDamage;
            _limbs.Add(bodyPart);

            if (bodyPart is DismemberableBodyPart dismemberableBodyPart)
                dismemberableBodyPart.Dismember += OnDismemberBodyPart;
        }

        foreach (var limb in _limbs)
        {
            limb.Init(_health * _healthRatios[limb.Type], _damageMultipliers[limb.Type]);
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

    private void OnBodyPartTookDamage(BodyPart bodyPart, Vector3 hitGlobalPos, Vector3 hitDir, float force,
        float amount)
    {
        var blood = _blood.Instantiate<GpuParticles3D>();

        Containers.Instance.VFX.AddChild(blood);
        blood.GlobalPosition = hitGlobalPos;

        _health -= amount;
        if (_health <= 0f && !_isDead)
            Die();
        
    }

    private void OnBodyPartDestroyed(BodyPart bodyPart, Vector3 dir, float force, bool shouldDie)
    {
        if (shouldDie && !_isDead)
            Die();

        if (bodyPart is not DismemberableBodyPart && _isDead)
        {
            bodyPart.ApplyImpulse(dir * force);
        }
    }

    private void OnDismemberBodyPart(Godot.Collections.Array<DismemberableBodyPart> destroyedDismemberableBodyParts,
        Vector3 dir, float force)
    {
        var bodyParts = new List<RigidBody3D>();
        var limbContainer = _limbContainerPackedScene.Instantiate<LimbContainer>();
        Containers.Instance.Limbs.AddChild(limbContainer);

        for (int i = 0;
             i < destroyedDismemberableBodyParts.Count;
             i++) // add the limbs to the scene tree and hinge them
        {
            var boneGlobalTransform = _isDead
                ? destroyedDismemberableBodyParts[i].GlobalTransform
                : _skeleton.GlobalTransform * _skeleton.GetBoneGlobalPose(destroyedDismemberableBodyParts[i].BoneIdx);

            var bodyPart = destroyedDismemberableBodyParts[i].BodyPartPackedScene.Instantiate<RigidBody3D>();
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
                hinge.Set("angular_limit/enable", true);
                hinge.Set("angular_limit/upper", Mathf.DegToRad(10));
                hinge.Set("angular_limit/lower", Mathf.DegToRad(-30));
            }
        }

        _skeleton.SetBonePoseScale(destroyedDismemberableBodyParts[0].BoneIdx,
            Vector3.One * 0.01f); // shrink armature at root bone to "remove" the mesh

        bodyParts[0]
            .ApplyImpulse(new Vector3(dir.X, .5f, dir.Z).Normalized() * force); // apply impulse to the root of the limb
    }

    private void Die()
    {
        _collisionShape.QueueFree();
        _isDead = true;
        _physicalBoneSimulator.PhysicalBonesStartSimulation();
        _simulationRunTimer.Start();
        EmitSignalDead();
    }
}