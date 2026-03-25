using Godot;
using System;
using ZombieSurvival.scripts.damage_system;

public partial class Attack : Area3D
{
    [Export] private float _damage;

    [Export] private float _force;

    [Export] private Timer _hitDuration;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
        _hitDuration.Timeout += Disable;
        _hitDuration.Start();
    }

    public void Enable()
    {
        _hitDuration.Start();
        SetDeferred(Area3D.PropertyName.Monitorable, true);
        SetDeferred(Area3D.PropertyName.Monitoring, true);
    }

    public void Disable()
    {
        SetDeferred(Area3D.PropertyName.Monitorable, false);
        SetDeferred(Area3D.PropertyName.Monitoring, false);
    }

    private void OnBodyEntered(Node body)
    {
        if (body is IDamageable damageable)
        {
            damageable.TakeDamage(_damage, GlobalPosition, ((Node3D)body).GlobalPosition - GlobalPosition, _force);
        }
        Disable();
    }
}