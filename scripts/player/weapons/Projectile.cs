using Godot;

namespace ZombieSurvival.scripts.player.weapons;

public  abstract partial class Projectile: RigidBody3D
{
    protected float _damage;

    protected float _force;
    
    public void Init(float damage, float force)
    {
        _damage = damage;
        _force = force;
    }
}