using ZombieSurvival.scripts.damage_system;

using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
using ZombieSurvival.scripts.damage_system;
using Vector3 = Godot.Vector3;

namespace ZombieSurvival.scripts.zombie_package;

public partial class BodyPart : PhysicalBone3D, IDamageable
{
    [Signal]
    public delegate void DestroyedEventHandler(BodyPart bodyPart, Vector3 dir, float force, bool shouldDie);

    [Signal]
    public delegate void TookDamageEventHandler(BodyPart bodyPart, Vector3 hitPosition, Vector3 dir, float force, float amount);

    [Export] public Limb Type;
    [Export] private bool _shouldDie;

    public int BoneIdx;

    private float _health;

    private float _damageMultiplier;

    protected bool _destroyed;
    
    public override void _Ready()
    {
        BoneIdx = GetBoneId();
    }

    public void Init(float boneHealth, float damageMultiplier)
    {
        _health = boneHealth;
        _damageMultiplier = damageMultiplier;
    }


    public void TakeDamage(float damage, Vector3 hitGlobalPosition, Vector3 hitDir, float force)
    {
        _health -= damage;
        if (_health <= 0f)
        {
            OnDestroyed(hitDir, force);
        }
        EmitSignalTookDamage(this, hitGlobalPosition, hitDir, force, damage*_damageMultiplier);
    }

    protected virtual void OnDestroyed(Vector3 dir, float force)
    {
        if (_destroyed) return;
        _destroyed = true;
        EmitSignalDestroyed(this, dir, force, _shouldDie);
    }
    
}