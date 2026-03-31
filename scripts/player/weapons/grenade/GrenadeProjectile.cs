using Godot;
using System;

namespace ZombieSurvival.scripts.player.weapons.grenade;

public partial class GrenadeProjectile : Projectile
{
    [Export] private float _throwSpeed;
    [Export] private float _fuseTime;

    [Export] private PackedScene _explodeScene;

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
}