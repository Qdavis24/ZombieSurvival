using Godot;
using System;

namespace ZombieSurvival.scripts.player.weapons.grenade;

public partial class GrenadeProjectile : Projectile
{
    [Export] private float _throwSpeed;
    [Export] private float _fuseTime;

    [Export] private PackedScene _explodeScene;
    
    [Export] private AudioStream _bounceSound;
    private bool _canBounceSound = true;

    public void Throw(Vector3 throwDirection)
    {
        LinearVelocity = throwDirection.Normalized() * _throwSpeed;

        // Add random spin
        Vector3 randomSpin = new Vector3(
            (float)GD.RandRange(-7f, 7f),
            (float)GD.RandRange(-7f, 7f),
            (float)GD.RandRange(-7f, 7f)
        );

        AngularVelocity = randomSpin;
    }

    // Called when the node enters the scene tree for the first time.
    public override async void _Ready()
    {
        BodyEntered += OnBodyEntered;
        
        await ToSignal(GetTree().CreateTimer(_fuseTime), SceneTreeTimer.SignalName.Timeout);
        Explode();
    }

    private void Explode()
    {
        var explosion = _explodeScene.Instantiate<Explosion>();
        explosion.Init(_damage, _force);
        Containers.Instance.VFX.AddChild(explosion);
        explosion.GlobalPosition = GlobalPosition;
        QueueFree();
    }
    

    private async void OnBodyEntered(Node body)
    {
        if (!_canBounceSound) return;
        _canBounceSound = false;

        AudioManager.I.Play3D(_bounceSound, GlobalPosition, -15f);

        await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
        _canBounceSound = true;
    }
}