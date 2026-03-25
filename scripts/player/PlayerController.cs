using Godot;
using System;
using ZombieSurvival.scripts.damage_system;

public partial class PlayerController : CharacterBody3D, IDamageable
{
	private Node3D _head;
	[Export] private VfxHud _vfxHud;
	[Export] private float _mouseSensitivity = 0.0020f;
	[Export] private float _moveSpeed = 6.0f;
	[Export] private float _accel = 14.0f;
	// [Export] private float _gravity = 24.0f;
	[Export] private float _gravity = 20.0f;
	[Export] private float _jumpVelocity = 6.0f;
	
	private float _yaw; // left and right
	private float _pitch; // up and down

	private float _maxHealth = 200f;
	private float _health = 200f;
	
	public void OnWeaponFired(
		float shakeDuration,
		float shakeStrength,
		float pitchKickDegrees,
		float yawKickDegrees,
		bool manualRecoil
	)
	{
		if (manualRecoil)
		{
			AddPitchRecoil(Mathf.DegToRad(pitchKickDegrees));
		}
	}	
	
	private void ApplyLookRotation()
	{
		_pitch = Mathf.Clamp(_pitch, -Mathf.Pi / 2f, Mathf.Pi / 2f);

		Rotation = new Vector3(0f, _yaw, 0f);
		_head.Rotation = new Vector3(_pitch, 0f, 0f);
	}

	private void AddPitchRecoil(float recoilRadians)
	{
		_pitch -= recoilRadians;
		ApplyLookRotation();
	}

	public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		
		CallDeferred(nameof(SetMouseCapture));

		_yaw = Rotation.Y;
		_pitch = _head.Rotation.X;
	}
	private void SetMouseCapture()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		// TODO: TEMPORARY - delete this if once a pause screen is set up
		if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
		{
			if (keyEvent.Keycode == Key.Escape)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}
		}

		if (Input.MouseMode == Input.MouseModeEnum.Visible && @event is InputEventMouseButton mouseButton)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}

		if (Input.MouseMode == Input.MouseModeEnum.Captured && @event is InputEventMouseMotion mouseMotion)
		{
			_yaw -= mouseMotion.Relative.X * _mouseSensitivity;
			_pitch -= mouseMotion.Relative.Y * _mouseSensitivity;
			ApplyLookRotation();
		}
		
	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;

		var input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");

		var localDir = new Vector3(input.X, 0f, input.Y);
		var wishDir = (Transform.Basis * localDir).Normalized();

		var horizontal = new Vector3(Velocity.X, 0f, Velocity.Z);
		var target = wishDir * _moveSpeed;

		horizontal = horizontal.Lerp(target, _accel * dt);

		var yVel = Velocity.Y;

		if (IsOnFloor())
		{
			if (Input.IsActionJustPressed("jump"))
				yVel = _jumpVelocity;
			else
				yVel = 0f;
		}
		else
		{
			yVel -= _gravity * dt;
		}

		Velocity = new Vector3(horizontal.X, yVel, horizontal.Z);
		MoveAndSlide();
	}

	public void TakeDamage(float damage, Vector3 hitGlobalPosition, Vector3 hitDir, float force)
	{
		_health -= damage;
		_vfxHud.ShowHitFlash();
		_vfxHud.UpdateHealth(_health, _maxHealth);
	}
}
