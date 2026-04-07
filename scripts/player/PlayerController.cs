using Godot;
using System;
using ZombieSurvival.scripts.damage_system;
using ZombieSurvival.scripts.inventory_system;
using ZombieSurvival.scripts.player.weapons;

public partial class PlayerController : CharacterBody3D, IInventoryOwner, IDamageable
{
	[Signal] public delegate void PlayerDiedEventHandler();
	
	private Node3D _head;
	[Export] private float _healthRegenRate;
	[Export] private PlayerHud _playerHud;
	[Export] private WeaponManager _weaponManager;
	[Export] private float _mouseSensitivity = 0.0020f;
	[Export] private float _moveSpeed = 6.0f;
	[Export] private float _accel = 14.0f;
	// [Export] private float _gravity = 24.0f;
	[Export] private float _gravity = 20.0f;
	[Export] private float _jumpVelocity = 6.0f;

	[ExportGroup("Sounds")] 
	[Export] private AudioStream _footstepSound;
	[Export] private AudioStream _jumpSound;
	[Export] private AudioStream _hitSound;
	[Export] private AudioStream _deathSound;

	private float _yaw; // left and right
	private float _pitch; // up and down

	[ExportGroup("Player")] 
	[Export] public Inventory Inventory { get; private set; }
	
	[Export] private float _maxHealth = 200f;
	[Export] private float _health = 200f;
	private float Health
	{
		get => _health;
		set
		{
			var clamped = Mathf.Clamp(value, 0f, _maxHealth);
			if (clamped < _health)
				_playerHud.ShowHitFlash();
			_health = clamped;
			_playerHud.UpdateHealthIndicator(_health, _maxHealth);
		}
	}

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
		
		_yaw = Rotation.Y;
		_pitch = _head.Rotation.X;
		Inventory.ItemAdded += OnItemAdded;
		Inventory.ItemRemoved += OnItemRemoved;
		_weaponManager.AmmoChanged += _playerHud.SetAmmo;
		_weaponManager.GrenadesChanged += _playerHud.SetGrenades;
		_weaponManager.ReloadFailed += _playerHud.ReloadFailed;
		_weaponManager.GrenadeThrowFailed += _playerHud.GrenadeThrowFailed;
		_health = _maxHealth;
	}

	private void OnItemRemoved(ItemType type, int amount)
	{
		if (type is ItemType.ParasiticMaterial)
		{
			_playerHud.ShowPickup(type, -amount);
			_playerHud.SetParasiticMaterial(Inventory.GetAmount(ItemType.ParasiticMaterial));
		}
	}

	private void OnItemAdded(ItemType type, int amount)
	{
		_playerHud.ShowPickup(type, amount);
		if (type is ItemType.ParasiticMaterial)
		{
			_playerHud.SetParasiticMaterial(Inventory.GetAmount(ItemType.ParasiticMaterial));
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
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
		
		Health += _healthRegenRate * _maxHealth * dt;
		

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
			{
				yVel = _jumpVelocity;
			
				AudioManager.I.Play3D(_jumpSound, GlobalPosition, -15f);	
			}
			else
			{
				yVel = 0f;
			
				// Only play footstep when moving on the ground
				if (wishDir.Length() > 0.1f)
				{
					AudioManager.I.PlayFootstep(_footstepSound, GlobalPosition);
				}
			}
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
		Health -= damage;
		AudioManager.I.Play3D(_hitSound, GlobalPosition, -15f);

		if (Health <= 0)
		{
			CallDeferred(nameof(HandleDeath));
		}
	}

	private void HandleDeath()
	{
		// Stop movement
		SetPhysicsProcess(false);
		Velocity = Vector3.Zero;
		
		AudioManager.I.PlayUi(_deathSound, -12f);

		var tween = CreateTween();
		tween.TweenProperty(this, "rotation", new Vector3(Mathf.DegToRad(90f), Rotation.Y, Rotation.Z), 0.5f)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);

		tween.Finished += () => EmitSignal(SignalName.PlayerDied);
	}
}
