using ZombieSurvival.scripts.damage_system;

using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
using ZombieSurvival.scripts.damage_system;
using Vector3 = Godot.Vector3;

namespace ZombieSurvival.scripts.zombie_package.dismemberment_system;

public partial class BodyPart : PhysicalBone3D, IDamageable
{
    [Signal]
    public delegate void DestroyedEventHandler(BodyPart bodyPart, Vector3 hitPosition, Vector3 dir, float force, bool shouldDie,
        bool shouldDismember);

    [Signal]
    public delegate void TookDamageEventHandler(BodyPart bodyPart, Vector3 hitPosition, Vector3 dir, float force,
        float amount);

    [Export] public AudioStream ShotHitSound;

    [Export] public Limb Type;
    [Export] private bool _shouldDie;
    [Export] public bool ShouldDismember;
    [Export] public PackedScene BodyPartPackedScene;
    [Export] public BodyPart ChildBodyPart;

    public int BoneIdx;

    private float _health;

    private float _damageMultiplier;

    public bool IsDestroyed;

    public override void _Ready()
    {
        BoneIdx = GetBoneId();
    }

    public void Init(float boneHealth, float damageMultiplier)
    {
        _health = boneHealth;
        _damageMultiplier = damageMultiplier;
    }

    public List<BodyPart> CollectChain()
    {
        var attachedBodyParts = new List<BodyPart>();

        var currAttachedBodyPart = this;
        while (currAttachedBodyPart != null && IsInstanceValid(currAttachedBodyPart) && !currAttachedBodyPart.IsDestroyed)
        {
            attachedBodyParts.Add(currAttachedBodyPart);
            currAttachedBodyPart = currAttachedBodyPart.ChildBodyPart;
        }

        return attachedBodyParts;
    }

    public void MarkDestroyed()
    {
        var currDBodyPart = this;
        while (currDBodyPart != null && IsInstanceValid(currDBodyPart) && !currDBodyPart.IsDestroyed)
        {
            currDBodyPart.IsDestroyed = true;
            if (currDBodyPart.ShouldDismember) currDBodyPart.QueueFree();
            currDBodyPart = currDBodyPart.ChildBodyPart;
        }
    }

    public void TakeDamage(float damage, Vector3 hitGlobalPosition, Vector3 hitDir, float force)
    {
        if (IsDestroyed) return;
        
        EmitSignalTookDamage(this, hitGlobalPosition, hitDir, force, damage*_damageMultiplier);
        
        _health -= damage;
        if (_health <= 0f)
        {
            EmitSignalDestroyed(this, hitGlobalPosition, hitDir, force, _shouldDie, ShouldDismember);
        }
        
    }
    
}