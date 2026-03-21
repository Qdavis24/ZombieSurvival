namespace ZombieSurvival.scripts.zombie_package;

using Godot;
using System;
using System.Collections.Generic;
using System.Numerics;
using ZombieSurvival.scripts.zombie_package;
using Vector3 = Godot.Vector3;

public partial class BodyPart : PhysicalBone3D, IDamageable
{
    [Signal]
    public delegate void DestroyedEventHandler(Vector3 dir, float force, bool shouldDie);

    [Signal]
    public delegate void TookDamageEventHandler(Vector3 hitPosition, Vector3 dir, float force, float amount);

    [Export] public Limb Type;
    [Export] private bool _shouldDie;

    public int BoneIdx;

    private float _health;

    private float _damageMultiplier;
    
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
        EmitSignalTookDamage(hitGlobalPosition, hitDir, force, damage*_damageMultiplier);
        
        if (_health <= 0f)
        {
            OnDestroyed(hitDir, force);
        }
    }

    protected virtual void OnDestroyed(Vector3 dir, float force)
    {
        EmitSignalDestroyed(dir, force, _shouldDie);
    }
    
}