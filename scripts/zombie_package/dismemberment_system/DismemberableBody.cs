using Godot;
using System;
using System.Collections.Generic;
using ZombieSurvival.scripts.zombie_package;

namespace ZombieSurvival.scripts.zombie_package.dismemberment_system;

public partial class DismemberableBody : Skeleton3D
{
    [Signal]
    public delegate void DeadEventHandler();

    [Signal]
    public delegate void SimulationFinishedEventHandler();

    [Export] private float _dismemberedBodyPartImpulseScale = .25f;
    [Export] private int _maxDismemberments = 4;
    private int _currDismemberments;

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

    [ExportCategory("Miscellaneous")] [Export]
    private Timer _simulationRunTimer;
    [Export] private PackedScene _parasiticMaterialPackedScene;
    [Export] private PhysicalBoneSimulator3D _physicalBoneSimulator;
    [Export] private PackedScene _limbContainerPackedScene;
    [Export] private PackedScene _blood;

    private float _health;
    private bool _isDead;

    private List<BodyPart> _limbs = new();
    private Dictionary<BodyPart, Transform3D> _limbsOffsetFromSkeleton = new();
    private Dictionary<Limb, float> _damageMultipliers;
    private Dictionary<Limb, float> _healthRatios;
    private Node3D _pickupTarget;

    public void Init(float health, Node3D pickupTarget)
    {
        _pickupTarget = pickupTarget;
        _health = health;
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

        _simulationRunTimer.Timeout += EmitSignalSimulationFinished;

        foreach (var child in _physicalBoneSimulator.GetChildren())
        {
            if (child is not BodyPart bodyPart) continue;

            bodyPart.Destroyed += OnBodyPartDestroyed;
            bodyPart.TookDamage += OnBodyPartTookDamage;
            _limbs.Add(bodyPart);
        }

        foreach (var limb in _limbs)
        {
            limb.Init(_health * _healthRatios[limb.Type], _damageMultipliers[limb.Type]);
            var boneGlobalT = GlobalTransform * GetBoneGlobalPose(limb.BoneIdx);
            _limbsOffsetFromSkeleton[limb] = limb.GlobalTransform.Inverse() * boneGlobalT;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        _currDismemberments = 0;
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        _physicalBoneSimulator.PhysicalBonesStartSimulation();
        _simulationRunTimer.Start();
        EmitSignalDead();
    }

    private void CreateHinge(LimbContainer limbContainer, Transform3D hingeTransform, RigidBody3D bodyPartFirst,
        RigidBody3D bodyPartSecond)
    {
        var hinge = limbContainer.Hinge.Instantiate<HingeJoint3D>();
        limbContainer.AddChild(hinge);
        hinge.GlobalTransform = hingeTransform;
        hinge.NodeA = bodyPartFirst.GetPath();
        hinge.NodeB = bodyPartSecond.GetPath();
        hinge.Set("angular_limit/enable", true);
        hinge.Set("angular_limit/upper", Mathf.DegToRad(10));
        hinge.Set("angular_limit/lower", Mathf.DegToRad(-30));
    }

    private void OnBodyPartTookDamage(BodyPart bodyPart, Vector3 hitGlobalPos, Vector3 hitDir, float force,
        float amount)
    {
        var blood = _blood.Instantiate<GpuParticles3D>();
        var processMat = blood.ProcessMaterial as ParticleProcessMaterial;
        processMat.Direction = hitDir;
        Containers.Instance.VFX.AddChild(blood);
        blood.GlobalPosition = hitGlobalPos;

        _health -= amount;
        if (_health <= 0f && !_isDead)
            Die();
    }

    private void OnBodyPartDestroyed(BodyPart attachedBodyPart, Vector3 dir, float force, bool shouldDie,
        bool shouldDismember)
    {
        if (shouldDie && !_isDead)
            Die();

        if (shouldDismember)
        {
            var rootDetachedBodyPart = SpawnDetachedBodyParts(attachedBodyPart.CollectChain());
            if (rootDetachedBodyPart != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    var parasiticMaterial = SpawnParasiticMaterial(rootDetachedBodyPart.GlobalTransform);
                    parasiticMaterial.ApplyCentralImpulse((new Vector3(GD.RandRange(-1,1), 0, GD.RandRange(-1,1)) + Vector3.Up).Normalized() * 3f);

                    float spinAmount = 3f;

                    parasiticMaterial.ApplyTorqueImpulse(Vector3.Up * spinAmount);
                }
                rootDetachedBodyPart.ApplyImpulse(dir * force * _dismemberedBodyPartImpulseScale);
                attachedBodyPart.MarkDestroyed();
            }
        }
        else
        {
            attachedBodyPart.ApplyImpulse(dir * force);
            attachedBodyPart.MarkDestroyed();
        }
    }

    private RigidBody3D SpawnDetachedBodyParts(List<BodyPart> destroyedAttachedBodyParts)
    {
        if (_currDismemberments >= _maxDismemberments) return null;
        _currDismemberments++;

        var detachedBodyParts = new List<RigidBody3D>();

        var limbContainer = _limbContainerPackedScene.Instantiate<LimbContainer>();
        Containers.Instance.Limbs.AddChild(limbContainer);

        for (int i = 0;
             i < destroyedAttachedBodyParts.Count;
             i++) // add the limbs to the scene tree and hinge them
        {
            var boneGlobalTransform = _isDead
                ? destroyedAttachedBodyParts[i].GlobalTransform *
                  _limbsOffsetFromSkeleton[destroyedAttachedBodyParts[i]]
                : GlobalTransform * GetBoneGlobalPose(destroyedAttachedBodyParts[i].BoneIdx);

            var currDetachedBodyPart = destroyedAttachedBodyParts[i].BodyPartPackedScene.Instantiate<RigidBody3D>();
            limbContainer.AddChild(currDetachedBodyPart);
            currDetachedBodyPart.GlobalTransform = boneGlobalTransform;
            detachedBodyParts.Add(currDetachedBodyPart);

            if (i > 0)
                CreateHinge(limbContainer, boneGlobalTransform, detachedBodyParts[i - 1], currDetachedBodyPart);
        }
        
        SetBonePoseScale(destroyedAttachedBodyParts[0].BoneIdx,
            Vector3.One * 0.01f); // shrink armature at root bone to "remove" the mesh
        return detachedBodyParts[0];
    }

    private ParasiticMaterial SpawnParasiticMaterial(Transform3D transform)
    {
        var parasiticMat = _parasiticMaterialPackedScene.Instantiate<ParasiticMaterial>();
        parasiticMat.GlobalTransform = transform;
        Containers.Instance.VFX.AddChild(parasiticMat);
        return parasiticMat;
    }
}