using Godot;
using System;
using ZombieSurvival.scripts.player.weapons;


public partial class RpgWeapon : WeaponBase
{
    [Export] private Node3D _rocket;
    [Export] private float _rocketShowDelay = 0.5f;
    [Export] private float _rocketHideDelay = 0.1f;
    [Export] private PackedScene _projectile;
    [Export] private Marker3D _barrelMarker;

    protected override async void ResolveShot(Vector3 from, Vector3 direction)
    {
        //base.ResolveShot(from, direction);

        // Delay before hiding rocket to better match recoil
        if (_rocket != null)
        {
            await ToSignal(GetTree().CreateTimer(_rocketHideDelay), SceneTreeTimer.SignalName.Timeout);
            _rocket.Visible = false;
        }
        var proj = _projectile.Instantiate<RpgProjectile>();
        proj.Init(_damage, _force);
        Containers.Instance.Projectiles.AddChild(proj);
        proj.GlobalTransform = _barrelMarker.GlobalTransform;
        proj.LinearVelocity = direction * 30f;
    }

    protected override async void StartReload()
    {
        base.StartReload();

        // Delay before showing rocket
        if (_rocket != null)
        {
            await ToSignal(GetTree().CreateTimer(_rocketShowDelay), SceneTreeTimer.SignalName.Timeout);
            _rocket.Visible = true;
        }
    }
}
