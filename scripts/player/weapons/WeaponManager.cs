using Godot;

public partial class WeaponManager : Node
{
    [Export] private NodePath _weaponSocketPath;
    [Export] private PackedScene _startingWeapon;
    
    [Export] private float _defaultHipFov = 90f;
    [Export] private float _fovLerpSpeed = 80f;
    
    private const string HitResolverPath = "../../HitResolver";

    private Node3D _weaponSocket;
    private WeaponBase _current;
    private Camera _camera;
    private HitResolver _hitResolver;

    public override void _Ready()
    {
        _weaponSocket = GetNode<Node3D>(_weaponSocketPath);

        _camera = GetParent()
            .GetNode<Node3D>("Head")
            .GetNode<Camera>("Camera3D");

        _hitResolver = GetNode<HitResolver>(HitResolverPath);

        Equip(_startingWeapon);
    }

    private void Equip(PackedScene weaponScene)
    {
        _current?.QueueFree();

        _current = weaponScene.Instantiate<WeaponBase>();
        _weaponSocket.AddChild(_current);
        _current.Initialize(_camera, _hitResolver);
        _current.Fired += _camera.OnWeaponFired; // Listen to shots for recoil
    }

    public override void _Process(double delta)
    {
        bool aimHeld = Input.IsActionPressed("aim");
        _current?.SetAimState(aimHeld);
        float targetFov = _current != null ? _current.GetTargetFov() : _defaultHipFov;
        _camera.Fov = Mathf.MoveToward(_camera.Fov, targetFov, (float)(_fovLerpSpeed * delta));

        bool isMovingForward = Input.IsActionPressed("move_forward");
        _current?.SetMovementState(isMovingForward);
        _camera.SetMovementState(isMovingForward);
        
        bool triggerHeld = Input.IsActionPressed("fire");
        _current?.TryFire(triggerHeld);
    }
}