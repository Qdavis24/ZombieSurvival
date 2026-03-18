using Godot;

public partial class Camera : Camera3D
{
    [Export] private float _kickReturnSpeed = 14.0f;
    [Export] private float _shakeReturnSpeed = 20.0f;
    [Export] private float _randomShakePitchMultiplier = 0.35f;
    [Export] private float _randomShakeYawMultiplier = 0.35f;
    
    [Export] private float _bobAmplitude = 0.05f;
    [Export] private float _bobSpeed = 10f;

    private float _bobTime = 0f;
    private bool _isMoving = false;

    private Vector2 _kickOffsetDegrees = Vector2.Zero;
    private Vector2 _shakeOffsetDegrees = Vector2.Zero;
    private float _shakeTimeRemaining = 0.0f;
    private float _shakeStrength = 0.0f;

    public void OnWeaponFired(
        float shakeDuration,
        float shakeStrength,
        float pitchKickDegrees,
        float yawKickDegrees,
        bool manualRecoil
    )
    {
        _shakeTimeRemaining = Mathf.Max(_shakeTimeRemaining, shakeDuration);
        _shakeStrength = shakeStrength;

        _kickOffsetDegrees.X -= pitchKickDegrees;
        _kickOffsetDegrees.Y += yawKickDegrees;
    }
    
    public void SetMovementState(bool moving)
    {
        _isMoving = moving;
    }
    
    public override void _Process(double delta)
    {
        float dt = (float)delta;

        _kickOffsetDegrees = _kickOffsetDegrees.MoveToward(Vector2.Zero, _kickReturnSpeed * dt);
        _shakeOffsetDegrees = _shakeOffsetDegrees.MoveToward(Vector2.Zero, _shakeReturnSpeed * dt);

        if (_shakeTimeRemaining > 0.0f)
        {
            _shakeTimeRemaining = Mathf.Max(0.0f, _shakeTimeRemaining - dt);

            float randomPitch = (float)GD.RandRange(-_shakeStrength, _shakeStrength) * _randomShakePitchMultiplier;
            float randomYaw = (float)GD.RandRange(-_shakeStrength, _shakeStrength) * _randomShakeYawMultiplier;
            _shakeOffsetDegrees = new Vector2(randomPitch, randomYaw);
        }

        RotationDegrees = new Vector3(
            _kickOffsetDegrees.X + _shakeOffsetDegrees.X,
            _kickOffsetDegrees.Y + _shakeOffsetDegrees.Y,
            0.0f
        );
        
        
        // walk bob
        if (_isMoving)
        {
            _bobTime += dt * _bobSpeed;
        }
        else
        {
            _bobTime = 0f;
        }

        float bob = Mathf.Sin(_bobTime) * _bobAmplitude;

        Position = new Vector3(
            0f,
            bob,
            0f
        );
    }
}
