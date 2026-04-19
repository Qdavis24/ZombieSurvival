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
    
    private float _damage;
    private float _force;

    private int _totalBodiesHit;
    private bool _havePlayedSpecialGore;

    private float _time;
    private float _duration;

    // Called when the node enters the scene tree for the first time.
    public void Init(float damage, float force)
    {
        _damage = damage;
        _force = force;
    }
    
    public override async void _Ready()
    {
        BodyEntered += OnBodyEntered;
        _duration = (float) _flash.Lifetime;
        _flash.Emitting = true;
        _sparks.Emitting = true;
        _smoke.Emitting = true;
        
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        AudioManager.I.PlayExplosion(_explosionSound, GlobalPosition);
        
        _flash.Finished += () =>
        {
            BodyEntered -= OnBodyEntered;
        };
        _smoke.Finished += QueueFree;
    }

    public override void _PhysicsProcess(double delta)
    {
        _time += (float) delta;
        _light.LightEnergy = Mathf.Lerp(2.0f, 0f, Mathf.Clamp(_time / _duration, 0f, 1f));
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