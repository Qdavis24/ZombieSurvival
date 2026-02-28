using Godot;

public partial class WeaponManager : Node
{
    [Export] private NodePath _weaponSocketPath;
    [Export] private PackedScene _startingWeapon;
    
    private const string HitResolverPath = "../../HitResolver";

    private Node3D _weaponSocket;
    private WeaponBase _current;
    private Camera3D _camera;
    private HitResolver _hitResolver;

    public override void _Ready()
    {
        _weaponSocket = GetNode<Node3D>(_weaponSocketPath);

        _camera = GetParent()
            .GetNode<Node3D>("Head")
            .GetNode<Camera3D>("Camera3D");

        _hitResolver = GetNode<HitResolver>(HitResolverPath);

        Equip(_startingWeapon);
    }

    private void Equip(PackedScene weaponScene)
    {
        _current?.QueueFree();

        _current = weaponScene.Instantiate<WeaponBase>();
        _weaponSocket.AddChild(_current);
        _current.Initialize(_camera, _hitResolver);
    }

    public override void _Process(double delta)
    {
        bool aimHeld = Input.IsActionPressed("aim");
        _current?.SetAimState(aimHeld);

        bool isMovingForward = Input.IsActionPressed("move_forward");
        _current?.SetMovementState(isMovingForward);

        bool triggerHeld = Input.IsActionPressed("fire");
        _current?.TryFire(triggerHeld);
    }
}