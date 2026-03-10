using System;
using Godot;

public partial class WeaponBase : Node3D
{
    [Export] private float _roundsPerMinute = 600f;
    [Export] private float _hipSpreadDegrees = 2.0f;
    [Export] private float _animBlendTime = 0.3f;
    [Export] private float _damage = 100f;
    [Export] private float _force = 6f;
    [Export] private float _aimFov = 75f;
    [Export] private float _hipFov = 90f;

    private double _cooldown;

    private Camera3D _camera;
    private AnimationPlayer _anim;
    private HitResolver _hitResolver;

    private bool _isAiming = false;
    private bool _isShooting;
    private bool _isTransitioning;
    private bool _aimStateChangeQueued; // true if changing from ads to hip or hip to ads
    private bool _queuedAimState; // true = ads, false = hip
    private bool _isWalkingForward;

    public void Initialize(Camera3D camera, HitResolver hitResolver)
    {
        _camera = camera;
        _hitResolver = hitResolver;
    }

    public override void _Ready()
    {
        // Required path naming (see Pistol.tscn)
        _anim = GetNode<AnimationPlayer>("Rig/AnimationPlayer");

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

        Fire();
        _cooldown = 60.0 / _roundsPerMinute;
    }

    private void Fire()
    {
        if (_camera == null) return;

        PlayShootForAimState();

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
            // Shooting should interrupt any transition visuals.
            _isTransitioning = false;
            _isShooting = true;
            _anim.Play(shoot);
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
            // NOTE: if I add a "transition_adstohip then I can just do a simple Play
            // so I have to do this for now
            
            _anim.PlayBackwards("transition_hiptoads");
            return;
            
            // BUT because the backwards has some awkward pacing I need to skip the animation a bit
            
            _anim.Play("transition_hiptoads", 0.0f, -1.0f, fromEnd: true);
            var anim = _anim.GetAnimation("transition_hiptoads");
            if (anim != null)
            {
                double len = anim.Length;
                double startPos = Math.Clamp(len - 0.3, 0.0, len);

                // Seek immediately so visuals update on this same frame.
                _anim.Seek(startPos, true);
            }
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