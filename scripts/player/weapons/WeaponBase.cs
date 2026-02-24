using Godot;

public partial class WeaponBase : Node3D
{
    [Export] private float _roundsPerMinute = 600f;
    [Export] private float _hipSpreadDegrees = 2.0f;

    private double _cooldown;

    private Camera3D _camera;

    public void Initialize(Camera3D camera)
    {
        _camera = camera;
    }

    public override void _Process(double delta)
    {
        _cooldown -= delta;
    }

    public void TryFire(bool triggerPressed)
    {
        if (!triggerPressed) return;
        if (_cooldown > 0) return;

        Fire();
        _cooldown = 60.0 / _roundsPerMinute;
    }

    private void Fire()
    {
        if (_camera == null) return;

        var from = _camera.GlobalTransform.Origin;
        var direction = -_camera.GlobalTransform.Basis.Z;

        // Apply hip spread
        direction = ApplySpread(direction);

        var to = from + direction * 1000f;

        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            GD.Print("Hit: ", result["collider"]);
        }
    }

    private Vector3 ApplySpread(Vector3 dir)
    {
        float spreadRad = Mathf.DegToRad(_hipSpreadDegrees);

        var randomYaw = (float)GD.RandRange(-spreadRad, spreadRad);
        var randomPitch = (float)GD.RandRange(-spreadRad, spreadRad);

        var basis = new Basis(Vector3.Up, randomYaw)
                  * new Basis(Vector3.Right, randomPitch);

        return (basis * dir).Normalized();
    }
}