using Godot;
using System;

public partial class RpgWeapon : WeaponBase
{
    [Export] private Node3D _rocket;
    [Export] private float _rocketShowDelay = 0.5f;
    [Export] private float _rocketHideDelay = 0.1f;

    protected override async void ResolveShot(Vector3 from, Vector3 direction)
    {
        base.ResolveShot(from, direction);

        // Delay before hiding rocket to better match recoil
        if (_rocket != null)
        {
            await ToSignal(GetTree().CreateTimer(_rocketHideDelay), SceneTreeTimer.SignalName.Timeout);
            _rocket.Visible = false;
        }
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
