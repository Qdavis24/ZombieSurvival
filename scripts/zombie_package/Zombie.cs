using Godot;
using System;
using System.Collections.Generic;
using ZombieSurvival.scripts.zombie_package.dismemberment_system;

namespace ZombieSurvival.scripts.zombie_package;

public partial class Zombie : CharacterBody3D
{
    [Signal]
    public delegate void DeadEventHandler();
    
    [ExportCategory("Miscellaneous")]
    [Export] DismemberableBody _dismemberableBody;
    [Export] private NavigationAgent3D _navAgent;
    [Export] private CollisionShape3D _collisionShape;
    [Export] private float _attackRange = 3f;
    
    private Node3D _target;
    private float _bodyHealth;
    private float _speed;
    
    private enum State
    {
        Chase,
        Attack,
        Dead
    }

    private State _state;
    

    public void Init(Node3D target, ZombieStats stats)
    {
        _target = target;
        _speed = stats.Speed;
        _bodyHealth = stats.Health;
    }

    public override void _Ready()
    {
        _state = State.Chase;
        _dismemberableBody.Init(_bodyHealth);
        _dismemberableBody.Dead += Die;
        _dismemberableBody.SimulationFinished += QueueFree;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_target == null || _state == State.Dead)
            return;
        
        var targetsPosition = _target.GetPosition();
        
        if ((targetsPosition - GlobalPosition).Length() < _attackRange)
            _state = State.Attack;
        else
            _state = State.Chase;

        switch (_state)
        {
            case State.Chase:
                MoveTowardTarget(targetsPosition);
                break;
            case State.Attack:
                AttackTarget(targetsPosition);
                break;
        }
        
    }

    private void MoveTowardTarget(Vector3 targetsPosition)
    {
        _navAgent.SetTargetPosition(targetsPosition);
        var nextPoint = _navAgent.GetNextPathPosition();
        var dir = (nextPoint - GlobalTransform.Origin).Normalized();
        if (Mathf.Abs(Basis.Z.Dot(dir)) < .99f)
            LookAt(GlobalTransform.Origin + dir * 3f, Vector3.Up, useModelFront: true);
        Velocity = dir * _speed;
        MoveAndSlide();
    }

    private void AttackTarget(Vector3 targetsPosition)
    {
        var dir = (targetsPosition - GlobalTransform.Origin).Normalized();
        if (Mathf.Abs(Basis.Z.Dot(dir)) < .99f)
            LookAt(GlobalTransform.Origin + dir * 3f, Vector3.Up, useModelFront: true);
    }

    private void Die()
    {
        _collisionShape.QueueFree();
        _state = State.Dead;
        EmitSignalDead();
    }
}