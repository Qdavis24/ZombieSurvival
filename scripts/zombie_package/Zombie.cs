using Godot;
using ZombieSurvival.scripts.zombie_package.dismemberment_system;

namespace ZombieSurvival.scripts.zombie_package;

public partial class Zombie : CharacterBody3D
{
    [Signal]
    public delegate void DeadEventHandler();

    [ExportCategory("Miscellaneous")]
    [Export] private AnimationTree _animationTree;
    [Export] private float _rotationLerpSpeed = 10f;
    [Export] private DismemberableBody _dismemberableBody;
    [Export] private NavigationAgent3D _navAgent;
    [Export] private CollisionShape3D _collisionShape;
    [Export] private float _attackRange = 1f;

    private AnimationNodeStateMachinePlayback _stateMachine;
    private Quaternion _targetRotation = Quaternion.Identity;
    private Node3D _target;
    private float _bodyHealth;
    private float _speed;

    private enum State { Chase, Attack, Dead }
    private State _state;
    private State _previousState;

    public void Init(Node3D target, ZombieStats stats)
    {
        _target = target;
        _speed = stats.Speed;
        _bodyHealth = stats.Health;
    }

    public override void _Ready()
    {
        _stateMachine = (AnimationNodeStateMachinePlayback)_animationTree.Get("parameters/playback");
        _dismemberableBody.Init(_bodyHealth);
        _dismemberableBody.Dead += Die;
        _dismemberableBody.SimulationFinished += QueueFree;
        SetState(State.Chase);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_target == null || _state == State.Dead)
            return;

        Quaternion = Quaternion.Slerp(_targetRotation, (float)(delta * _rotationLerpSpeed));

        var targetPosition = _target.GetPosition();
        var distanceToTarget = (targetPosition - GlobalPosition).Length();

        SetState(distanceToTarget < _attackRange ? State.Attack : State.Chase);

        switch (_state)
        {
            case State.Chase:
                MoveTowardTarget(targetPosition);
                break;
            case State.Attack:
                FaceTarget(targetPosition);
                break;
        }
    }

    private void SetState(State newState)
    {
        if (newState == _state) return;
        _previousState = _state;
        _state = newState;
        OnStateEntered(newState);
    }

    private void OnStateEntered(State state)
    {
        switch (state)
        {
            case State.Chase:
                _stateMachine.Travel("walk");
                break;
            case State.Attack:
                _stateMachine.Travel("attack-left");
                break;
            case State.Dead:
                _collisionShape.QueueFree();
                EmitSignalDead();
                break;
        }
    }

    private void MoveTowardTarget(Vector3 targetPosition)
    {
        _navAgent.SetTargetPosition(targetPosition);
        var nextPoint = _navAgent.GetNextPathPosition();
        var dir = (nextPoint - GlobalTransform.Origin).Normalized();
        FaceDirection(dir);
        Velocity = dir * _speed;
        MoveAndSlide();
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        var dir = (targetPosition - GlobalTransform.Origin).Normalized();
        FaceDirection(dir);
    }

    private void FaceDirection(Vector3 dir)
    {
        if (Mathf.Abs(Basis.Z.Dot(dir)) < .99f)
            _targetRotation = Transform3D.Identity.LookingAt(-dir, Vector3.Up).Basis.GetRotationQuaternion();
    }

    private void Die()
    {
        SetState(State.Dead);
    }
}