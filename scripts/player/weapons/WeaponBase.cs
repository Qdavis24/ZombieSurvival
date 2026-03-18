using System;
using Godot;

public partial class WeaponBase : Node3D
{
    [Signal] public delegate void FiredEventHandler(
        float shakeDuration,
        float shakeStrength,
        float pitchKickDegrees,
        float yawKickDegrees,
        bool manualRecoil
    );
    [Signal] public delegate void AmmoChangedEventHandler(int currentAmmo, int reserveAmmo);
    
    // Default values for pistol
    [ExportGroup("Stats")]
    [Export] private float _roundsPerMinute = 200f;
    [Export] private float _hipSpreadDegrees = 2.0f;
    [Export] private float _damage = 100f;
    [Export] private float _force = 6f;
    [Export] private int _magazineSize = 12;
    
    [ExportGroup("Camera And Recoil")]
    [Export] private float _aimFov = 75f;
    [Export] private float _hipFov = 90f;
    [Export] private bool _manualRecoil = false;
    [Export] private float _cameraShakeDuration = 0.05f;
    [Export] private float _cameraShakeStrength = 0.05f;
    [Export] private float _cameraPitchKickDegrees = -1f;
    [Export] private float _cameraYawKickDegrees = 0.08f;
    
    [ExportGroup("Misc")]
    [Export] private float _animBlendTime = 0.3f;
    [Export] private NodePath _muzzleFlashPath;
    
    private GpuParticles3D _muzzleFlash;
    private Camera3D _camera;
    private AnimationPlayer _anim;
    private HitResolver _hitResolver;

    private double _cooldown;
    private bool _isAiming = false;
    private bool _isShooting;
    private bool _isTransitioning;
    private bool _aimStateChangeQueued; // true if changing from ads to hip or hip to ads
    private bool _queuedAimState; // true = ads, false = hip
    private bool _isWalkingForward;
    private int _currentAmmo;
    private int _reserveAmmo;

    public void Initialize(Camera3D camera, HitResolver hitResolver, int currentAmmo, int reserveAmmo)
    {
        _camera = camera;
        _hitResolver = hitResolver;
        _currentAmmo = currentAmmo;
        _reserveAmmo = reserveAmmo;
        NotifyAmmoChanged();
    }

    public int CurrentAmmo => _currentAmmo;
    public int ReserveAmmo => _reserveAmmo;
    public int MagazineSize => _magazineSize;

    private void NotifyAmmoChanged()
    {
        EmitSignal(SignalName.AmmoChanged, _currentAmmo, _reserveAmmo);
    }

    public override void _Ready()
    {
        // Required node naming (see Pistol.tscn)
        _anim = GetNode<AnimationPlayer>("Rig/AnimationPlayer");
        
        _muzzleFlash = GetNode<GpuParticles3D>(_muzzleFlashPath);

        // Required animations for our weapon rigs
        RequireAnimation("hip_idle");
        RequireAnimation("ads_idle");
        RequireAnimation("hip_shoot");
        RequireAnimation("ads_shoot");
        RequireAnimation("transition_hiptoads");
        RequireAnimation("walk");

        _anim.AnimationFinished += OnAnimationFinished;

        PlayIdleForAimState();
    }

    public override void _Process(double delta)
    {
        _cooldown -= delta;
    }

    public void SetAimState(bool aimHeld)
    {
        if (_isAiming == aimHeld)
            return;

        // If we're in the middle of a shoot animation, queue the aim change and apply it right after shooting finishes.
        if (_isShooting)
        {
            _aimStateChangeQueued = true;
            _queuedAimState = aimHeld;
            return;
        }

        _isAiming = aimHeld;

        PlayTransitionForAimState();
    }

    public float GetTargetFov()
    {
        return _isAiming ? _aimFov : _hipFov;
    }

    private float GetShotInterval()
    {
        return 60.0f / _roundsPerMinute;
    }

    public void SetMovementState(bool isMoving)
    {
        if (_isWalkingForward == isMoving)
            return;

        _isWalkingForward = isMoving;

        // Don't interrupt shooting or transitions.
        if (_isShooting || _isTransitioning)
            return;

        PlayIdleForAimState();
    }

    public void TryFire(bool triggerPressed)
    {
        if (!triggerPressed) return;
        if (_cooldown > 0) return;
        if (_isShooting) return;
        if (_currentAmmo <= 0) return;

        Fire();
        _cooldown = GetShotInterval();
    }

    private void Fire()
    {
        if (_camera == null) return;

        PlayShootForAimState();
        EmitSignal(
            SignalName.Fired,
            _cameraShakeDuration,
            _cameraShakeStrength,
            _cameraPitchKickDegrees,
            _cameraYawKickDegrees,
            _manualRecoil
        );
        ShowMuzzleFlash();

        _currentAmmo--;
        NotifyAmmoChanged();

        var from = _camera.GlobalTransform.Origin;
        var direction = -_camera.GlobalTransform.Basis.Z;

        // Apply hip spread or recoil spread or whatever
        direction = ApplySpread(direction);

        var to = from + direction * 1000f;

        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);

        var result = spaceState.IntersectRay(query);

        if (result.Count > 0)
        {
            if (_hitResolver == null)
                throw new InvalidOperationException($"HitResolver is null on weapon '{Name}'. Ensure WeaponManager passes it into Initialize().");

            var collider = (Node)result["collider"];
            var point = (Vector3)result["position"];
            var normal = (Vector3)result["normal"];

            var hitInfo = new HitInfo(collider, point, normal, direction, _damage, _force);
            _hitResolver.HandleHit(hitInfo);
        }
    }
    
    private async void ShowMuzzleFlash()
    {
        _muzzleFlash.Restart();
        _muzzleFlash.Emitting = true;
    }

    private void PlayIdleForAimState()
    {
        if (_isShooting || _isTransitioning)
            return;

        // Walk animation plays only in hip state for now
        if (_isWalkingForward && !_isAiming)
        {
            if (_anim.CurrentAnimation != "walk")
                _anim.Play("walk", _animBlendTime);
            return;
        }

        StringName idle = _isAiming ? "ads_idle" : "hip_idle";
        if (_anim.CurrentAnimation != idle)
            _anim.Play(idle, _animBlendTime);
    }

    private void PlayShootForAimState()
    {
        StringName shoot = _isAiming ? "ads_shoot" : "hip_shoot";
        if (_anim.HasAnimation(shoot))
        {
            var animation = _anim.GetAnimation(shoot);

            float shotInterval = GetShotInterval();
            float playbackSpeed = shotInterval > 0.0f
                ? (float)(animation.Length / shotInterval)
                : 1.0f;

            // Shooting should interrupt any transition visuals.
            _isTransitioning = false;
            _isShooting = true;
            _anim.Play(shoot, customSpeed: playbackSpeed);
        }
    }

    private void PlayTransitionForAimState()
    {
        _isTransitioning = true;

        if (_isAiming)
        {
            _anim.Play("transition_hiptoads");
        }
        else
        {
            _anim.PlayBackwards("transition_hiptoads");
        }
    }

    private void OnAnimationFinished(StringName animName)
    {
        if (animName == "transition_hiptoads")
        {
            _isTransitioning = false;
            PlayIdleForAimState();
            return;
        }

        if (animName == "hip_shoot" || animName == "ads_shoot")
        {
            _isShooting = false;

            // If an aim change happened during shooting, apply it now and play the transition.
            if (_aimStateChangeQueued)
            {
                _aimStateChangeQueued = false;
                _isAiming = _queuedAimState;

                PlayTransitionForAimState();

                return;
            }

            PlayIdleForAimState();
        }
    }

    private Vector3 ApplySpread(Vector3 dir)
    {
        if (_isAiming) return dir; // Apply no spread if aiming in

        float spreadRad = Mathf.DegToRad(_hipSpreadDegrees);

        var randomYaw = (float)GD.RandRange(-spreadRad, spreadRad);
        var randomPitch = (float)GD.RandRange(-spreadRad, spreadRad);

        var basis = new Basis(Vector3.Up, randomYaw)
                  * new Basis(Vector3.Right, randomPitch);

        return (basis * dir).Normalized();
    }

    private void RequireAnimation(StringName name)
    {
        if (_anim == null)
            throw new InvalidOperationException($"AnimationPlayer is null on weapon '{Name}'.");

        if (!_anim.HasAnimation(name))
            throw new InvalidOperationException($"Weapon '{Name}' is missing required animation '{name}'.");
    }
}