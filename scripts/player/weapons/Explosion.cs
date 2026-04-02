using Godot;
using System;
using ZombieSurvival.scripts.damage_system;
using ZombieSurvival.scripts.zombie_package;

public partial class Explosion : Area3D
{
    [Export] private AudioStream _explosionSound;
    [Export] private GpuParticles3D _flash;
    private float _damage;
    private float _force;

    // Called when the node enters the scene tree for the first time.
    public void Init(float damage, float force)
    {
        _damage = damage;
        _force = force;
    }
    
    public override async void _Ready()
    {
        BodyEntered += OnBodyEntered;
        _flash.Emitting = true;
        
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        GD.Print("POS", GlobalPosition);
        AudioManager.I.PlayExplosion(_explosionSound, GlobalPosition);
        _flash.Finished += QueueFree;
    }


    private void OnBodyEntered(Node body)
    {
        if (body is IDamageable damageableBody)
        {
            var bodyGlobalPosition = ((Node3D) damageableBody).GlobalPosition;
            damageableBody.TakeDamage(_damage, bodyGlobalPosition, 
                (bodyGlobalPosition - GlobalPosition).Normalized(),
                _force);
        }
    }
}