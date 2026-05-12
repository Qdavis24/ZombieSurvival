using Godot;
using ZombieSurvival.scripts.inventory_system;
using ZombieSurvival.scripts.zombie_package.dismemberment_system;

namespace ZombieSurvival.scripts.zombie_package;

public partial class Zombie : CharacterBody3D
{
    [Signal]
    public delegate void DeadEventHandler();

    [ExportCategory("Miscellaneous")] 
    [Export] private int _deathMoneyReward = 10;
    [Export] private int _dismemberMoneyRewared = 5;
    [Export] private float _rotationLerpSpeed = 10f;
    [Export] private DismemberableBody _dismemberableBody;
    [Export] private NavigationAgent3D _navAgent;
    [Export] private CollisionShape3D _collisionShape;
    [Export] private float _attackRange = 1f;
    [Export] private float _gravity = 20f;
    [Export] private float _floorStickVelocity = 0.5f;
    [Export] private float _floorSnapLength = 0.35f;

    [ExportGroup("Sounds")] 
    [Export] private AudioStream _groanSound;
    private Timer _groanTimer;
    
    [Export] private AnimationPlayer _animationPlayer;
    private Quaternion _targetRotation = Quaternion.Identity;
    private Node3D _target;
    private float _bodyHealth;
    private float _speed;

    public enum State { Chase, Attack, Dead }
    private State _state;
    private State _previousState;
    
    public State CurrentState => _state;

    public void Init(Node3D target, ZombieStats stats)
    {
        _target = target;
        _speed = stats.Speed;
        _bodyHealth = stats.Health;
    }

    public override void _Ready()
    {
        _dismemberableBody.Init(_bodyHealth, _target);
        _dismemberableBody.Dead += Die;
        _dismemberableBody.SimulationFinished += QueueFree;
        _dismemberableBody.Dismembered += OnDismembered;
        FloorSnapLength = _floorSnapLength;
        SetState(State.Chase);
        SetupGroanTimer(); // audio
    }

    private void OnDismembered()
    {
        RewardPlayerWithMoney(_dismemberMoneyRewared);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_target == null || _state == State.Dead)
            return;

        Quaternion = Quaternion.Slerp(_targetRotation, (float)(delta * _rotationLerpSpeed));

        var targetPosition = _target.GlobalPosition;
        var distanceToTarget = (targetPosition - GlobalPosition).Length();

        SetState(distanceToTarget < _attackRange ? State.Attack : State.Chase);

        switch (_state)
        {
            case State.Chase:
                MoveTowardTarget(targetPosition, (float)delta);
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
                _animationPlayer.Play("walk", .2f);
                break;
            case State.Attack:
                _animationPlayer.Play("attack-left", .2f);
                break;
            case State.Dead:
                _collisionShape.QueueFree();
                _animationPlayer.Stop();
                RewardPlayerWithMoney(_deathMoneyReward);
                EmitSignalDead();
                break;
        }
    }

    private void MoveTowardTarget(Vector3 targetPosition, float delta)
    {
        _navAgent.SetTargetPosition(targetPosition);
        var nextPoint = _navAgent.GetNextPathPosition();
        var toNextPoint = nextPoint - GlobalTransform.Origin;
        var horizontal = new Vector3(toNextPoint.X, 0f, toNextPoint.Z);
        var horizontalDir = horizontal.LengthSquared() > 0.001f ? horizontal.Normalized() : Vector3.Zero;

        if (horizontalDir.LengthSquared() > 0.001f)
            FaceDirection(horizontalDir);

        var yVelocity = Velocity.Y;
        yVelocity = IsOnFloor() ? -_floorStickVelocity : yVelocity - (_gravity * delta);

        Velocity = new Vector3(horizontalDir.X * _speed, yVelocity, horizontalDir.Z * _speed);
        MoveAndSlide();
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        var dir = targetPosition - GlobalTransform.Origin;
        FaceDirection(new Vector3(dir.X, .1f, dir.Z).Normalized());
    }

    private void FaceDirection(Vector3 dir)
    {
        if (Mathf.Abs(Basis.Z.Dot(dir)) < .99f)
            _targetRotation = Transform3D.Identity.LookingAt(-dir, Vector3.Up).Basis.GetRotationQuaternion();
    }

    private void Die()
    {
        
        _groanTimer?.Stop(); // audio
        SetState(State.Dead);
    }

    private void RewardPlayerWithMoney(int amount)
    {
        if (_target is IInventoryOwner invOwner)
        {
            invOwner.Inventory.AddItem(ItemType.Money, amount);
        }
    }

    // Audio
    private void SetupGroanTimer()
    {
        _groanTimer = new Timer
        {
            OneShot = true
        };

        AddChild(_groanTimer);
        _groanTimer.Timeout += OnGroanTimerTimeout;
        ScheduleNextGroan();
    }

    private void OnGroanTimerTimeout()
    {
        if (_state == State.Dead || _groanSound == null)
            return;

        AudioManager.I.PlayFollowing(_groanSound, this, -10f, (float)GD.RandRange(0.9f, 1.1f));
        ScheduleNextGroan();
    }

    private void ScheduleNextGroan()
    {
        if (_state == State.Dead)
            return;

        var delay = (float)GD.RandRange(3f, 8f);
        _groanTimer.Start(delay);
    }
}
