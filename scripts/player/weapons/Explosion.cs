using Godot;
using System;
using ZombieSurvival.scripts.damage_system;
using ZombieSurvival.scripts.zombie_package;

public partial class Explosion : Area3D
{
    [Export] private AudioStream _explosionSound;
    [Export] private AudioStream _specialGoreSound;
    [Export] private GpuParticles3D _flash;
    private float _damage;
    private float _force;

    private int _totalBodiesHit;
    private bool _havePlayedSpecialGore = false;

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
            
            _totalBodiesHit++;
            if (!_havePlayedSpecialGore && _totalBodiesHit >= 50)
            {
                _havePlayedSpecialGore = true;

                AudioManager.I.Play3D(_specialGoreSound, bodyGlobalPosition);
            }
        }
    }
}