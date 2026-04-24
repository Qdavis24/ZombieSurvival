using System;
using Godot;
using ZombieSurvival.scripts.inventory_system;

public partial class WeaponBase : Node3D
{
    [Signal] public delegate void FiredEventHandler(
        float shakeDuration,
        float shakeStrength,
        float pitchKickDegrees,
        float yawKickDegrees,
        bool manualRecoil
    );
    [Signal] public delegate void AmmoChangedEventHandler(int currentAmmo);
    [Signal] public delegate void ReloadFailedEventHandler();
    
    // Default values for pistol
    [ExportGroup("Stats")]
    [Export] private float _roundsPerMinute = 200f;
    [Export] private float _hipSpreadDegrees = 2.0f;
    [Export] protected float _damage = 100f;
    [Export] protected float _force = 6f;
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
    [Export] private float _reloadFailedCooldown = 0.25f;
    [Export] public ItemType AmmoType;
    [Export] public ItemType WeaponType;
    [Export] private MuzzleFlash _muzzleFlash;
    
    [ExportGroup("Sound")]
    [Export] AudioStream _gunshotSound;
    [Export] AudioStream _reloadSound;
    
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
    private bool _isReloading;
    private bool _canEmitReloadFailed = true;
    private int _currentAmmo;
    
    private Func<int, int> _consumeAmmo;
    private Func<int> _getAvailableAmmo;

    public void Initialize(Camera3D camera, HitResolver hitResolver, int currentAmmo)
    {
        _camera = camera;
        _hitResolver = hitResolver;
        _currentAmmo = currentAmmo;
        NotifyAmmoChanged();
    }

    public void SetAmmoSource(Func<int, int> consumeAmmo, Func<int> getAvailableAmmo)
    {
        _consumeAmmo = consumeAmmo;
        _getAvailableAmmo = getAvailableAmmo;
    }

    public int CurrentAmmo => _currentAmmo;
    public int MagazineSize => _magazineSize;
    public float ReloadSpeedMultiplier { get; set; } = 1f;

    protected AnimationPlayer Anim => _anim;
    protected float AnimBlendTime => _animBlendTime;
    public bool IsAiming => _isAiming;
    public bool IsReloading => _isReloading;

    protected void BeginReloadState()
    {
        _isReloading = true;
        _isTransitioning = false;
    }

    protected bool TryLoadOneRoundIntoMagazine()
    {
        if (_currentAmmo >= _magazineSize)
            return false;

        int granted = _consumeAmmo?.Invoke(1) ?? 0;
        if (granted == 0)
            return false;

        _currentAmmo++;
        NotifyAmmoChanged();
        return true;
    }

    protected void FinishReloadState()
    {
        _isReloading = false;

        if (_aimStateChangeQueued)
        {
            _aimStateChangeQueued = false;
            _isAiming = _queuedAimState;
            PlayTransitionForAimState();
            return;
        }

        PlayIdleForAimState();
    }

    protected void NotifyAmmoChanged()
    {
        EmitSignal(SignalName.AmmoChanged, _currentAmmo);
    }

    public override void _Ready()
    {
        // Required node naming (see Pistol.tscn)
        _anim = GetNode<AnimationPlayer>("Rig/AnimationPlayer");

        // Required animations for our weapon rigs
        RequireAnimation("hip_idle");
        RequireAnimation("ads_idle");
        RequireAnimation("hip_shoot");
        RequireAnimation("hip_reload");
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

        if (_isReloading)
        {
            _aimStateChangeQueued = true;
            _queuedAimState = aimHeld;
            RequestReloadCancel();
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

        // Don't interrupt shooting, transitions, or reloading.
        if (_isShooting || _isTransitioning || _isReloading)
            return;

        PlayIdleForAimState();
    }

    public void TryFire(bool triggerPressed)
    {
        if (!triggerPressed) return;
        if (_cooldown > 0) return;
        if (_isShooting) return;
        if (_currentAmmo <= 0)
        {
            TryReload(); 
            return;
        }

        if (_isReloading)
        {
            RequestReloadCancel();
            return;
        }

        Fire();
        _cooldown = GetShotInterval();
    }

    public void TryReload()
    {
        if (_isReloading) return;
        if (_isShooting) return;
        if (_currentAmmo >= _magazineSize) return;

        int available = _getAvailableAmmo?.Invoke() ?? 0;
        if (available <= 0)
        {
            TryEmitReloadFailed();
            return;
        }

        StartReload();
    }

    private async void TryEmitReloadFailed()
    {
        if (!_canEmitReloadFailed)
            return;

        _canEmitReloadFailed = false;
        EmitSignal(SignalName.ReloadFailed);

        await ToSignal(GetTree().CreateTimer(_reloadFailedCooldown), SceneTreeTimer.SignalName.Timeout);
        _canEmitReloadFailed = true;
    }

    protected virtual void StartReload()
    {
        BeginReloadState();
        PlayReloadAnimation("hip_reload");
        AudioManager.I.PlayUi(_reloadSound);
    }

    protected virtual void RequestReloadCancel()
    {
    }

    protected void PlayReloadAnimation(StringName animationName)
    {
        _anim.Play(animationName, _animBlendTime, Mathf.Max(0.01f, ReloadSpeedMultiplier));
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
        _muzzleFlash.Activate();

        _currentAmmo--;
        NotifyAmmoChanged();

        var from = _camera.GlobalTransform.Origin;
        var direction = -_camera.GlobalTransform.Basis.Z;

        AudioManager.I.Play3D(_gunshotSound, from);
        ResolveShot(from, direction);
    }

    protected virtual void ResolveShot(Vector3 from, Vector3 direction)
    {
        // Apply hip spread or recoil spread or whatever
        if (!_isAiming)
        {
            direction = ApplySpread(direction);
        }
        
        var to = from + direction * 1000f;

        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(from, to);
        query.CollisionMask = 8;
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
    
    private void PlayIdleForAimState()
    {
        if (_isShooting || _isTransitioning || _isReloading)
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

    protected virtual bool HandleReloadAnimationFinished(StringName animName)
    {
        if (animName != "hip_reload")
            return false;

        int ammoNeeded = _magazineSize - _currentAmmo;
        int granted = _consumeAmmo?.Invoke(ammoNeeded) ?? 0;
        _currentAmmo += granted;
        NotifyAmmoChanged();

        FinishReloadState();
        return true;
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
        if (HandleReloadAnimationFinished(animName))
            return;

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

    protected Vector3 ApplySpreadBROKEN(Vector3 dir)
    {
        float spreadRad = Mathf.DegToRad(_hipSpreadDegrees);

        var randomYaw = (float)GD.RandRange(-spreadRad, spreadRad);
        var randomPitch = (float)GD.RandRange(-spreadRad, spreadRad);

        var basis = new Basis(Vector3.Up, randomYaw)
                  * new Basis(Vector3.Right, randomPitch);

        return (basis * dir).Normalized();
    }
    
    protected Vector3 ApplySpread(Vector3 dir)
    {
        float spreadRad = Mathf.DegToRad(_hipSpreadDegrees);

        float randomYaw = (float)GD.RandRange(-spreadRad, spreadRad);
        float randomPitch = (float)GD.RandRange(-spreadRad, spreadRad);

        Vector3 forward = dir.Normalized();
        Vector3 right = forward.Cross(Vector3.Up).Normalized();
        Vector3 up = right.Cross(forward).Normalized();

        Basis yawBasis = new Basis(up, randomYaw);
        Basis pitchBasis = new Basis(right, randomPitch);

        return (yawBasis * pitchBasis * forward).Normalized();
    }

    private void RequireAnimation(StringName name)
    {
        if (_anim == null)
            throw new InvalidOperationException($"AnimationPlayer is null on weapon '{Name}'.");

        if (!_anim.HasAnimation(name))
            throw new InvalidOperationException($"Weapon '{Name}' is missing required animation '{name}'.");
    }
}
