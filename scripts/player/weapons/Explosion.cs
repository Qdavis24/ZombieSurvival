using Godot;
using System;
using ZombieSurvival.scripts.damage_system;


public partial class Explosion : Area3D
{
    [Export] private AudioStream _explosionSound;
    [Export] private AudioStream _specialGoreSound;
    [Export] private GpuParticles3D _flash;
    [Export] private GpuParticles3D _sparks;
    [Export] private GpuParticles3D _smoke;
    [Export] private OmniLight3D _light;
    [Export] private Timer _timer;
    
    private float _damage;
    private float _force;

    private int _totalBodiesHit;
    private bool _havePlayedSpecialGore;
    
    

    // Called when the node enters the scene tree for the first time.
    public void Init(float damage, float force)
    {
        _damage = damage;
        _force = force;
    }
    
    public override async void _Ready()
    {
        _timer.Timeout += OnTimeout;
        _timer.Start();
        BodyEntered += OnBodyEntered;
        _flash.Emitting = true;
        _sparks.Emitting = true;
        _smoke.Emitting = true;
        _smoke.Finished += QueueFree;
        
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        AudioManager.I.PlayExplosion(_explosionSound, GlobalPosition, -5f);
    }

    private void OnTimeout()
    {
        BodyEntered -= OnBodyEntered;
        _light.LightEnergy = 0;
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
