using Godot;
using System;
using ZombieSurvival.scripts.damage_system;
using ZombieSurvival.scripts.inventory_system;
using ZombieSurvival.scripts.player.weapons;
using ZombieSurvival.scripts.zombie_package;

public partial class PlayerController : CharacterBody3D, IInventoryOwner, IDamageable
{
	[Signal] public delegate void PlayerDiedEventHandler();

	private Node3D _head;
	[Export] private float _healthRegenRate;
	[Export] private PlayerHud _playerHud;
	[Export] private WeaponManager _weaponManager;
	[Export] private GrenadeManager _grenadeManager;
	[Export] private CollisionShape3D _collisionShape3D;
	[Export] private float _mouseSensitivity = 0.0020f;
	[Export] private float _aimSensitivityMultiplier = 0.5f;
	[Export] private float _controllerSensitivityHorizontal = 5f;
	[Export] private float _controllerSensitivityVertical = 2.5f;
	[Export] private float _moveSpeed = 6.0f;
	[Export] private float _sprintMultiplier = 1.6f;
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
	[Export] private int _startingMoney = 500;

	[Export] private float _maxHealth = 75f;
	[Export] private float _health = 75f;
	[Export] private float _damageReductionDuration = 0.75f;
	[Export(PropertyHint.Range, "0,1,0.05")] private float _damageReductionMultiplier = 0.60f;
	private float _damageReductionTimer;
	private bool _isDead;
	private bool _sprintEnabled;
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
		Inventory.AddItem(ItemType.Money, _startingMoney);
		Inventory.ItemAdded += OnItemAdded;
		Inventory.ItemRemoved += OnItemRemoved;
		_weaponManager.AmmoChanged += _playerHud.SetAmmo;
		_weaponManager.ReloadFailed += _playerHud.ReloadFailed;
		_grenadeManager.GrenadesChanged += _playerHud.SetGrenades;
		_grenadeManager.GrenadeThrowFailed += _playerHud.GrenadeThrowFailed;
		_health = _maxHealth;
		_playerHud.UpdateHealthIndicator(_health, _maxHealth);
		_playerHud.SetParasiticMaterial(Inventory.GetAmount(ItemType.Money));
	}

	public void SetMaxHealth(float maxHealth, bool healGainedAmount = true)
	{
		var oldMaxHealth = _maxHealth;
		_maxHealth = Mathf.Max(1f, maxHealth);

		if (healGainedAmount && _maxHealth > oldMaxHealth)
			Health += _maxHealth - oldMaxHealth;
		else
			Health = Health;
	}

	public void SetSprintEnabled(bool enabled)
	{
		_sprintEnabled = enabled;
	}

	public void ShowCrosshair()
	{
		_playerHud.ShowCrosshair();
	}

	public void HideCrosshair()
	{
		_playerHud.HideCrosshair();
	}

	private void OnItemRemoved(ItemType type, int amount)
	{
		if (amount == 0)
			return;
		if (type is ItemType.Money)
		{
			_playerHud.ShowPickup(type, -amount);
			_playerHud.SetParasiticMaterial(Inventory.GetAmount(ItemType.Money));
		}
	}

	private void OnItemAdded(ItemType type, int amount)
	{
		if (amount == 0)
			return;
		if (type is ItemType.Money)
		{
			_playerHud.SetParasiticMaterial(Inventory.GetAmount(ItemType.Money));
			_playerHud.ShowMoneyPickup(amount);
			return;
		}
		_playerHud.ShowPickup(type, amount);
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

	public void ControllerLook(float dt)
	{
		// Controller look
		var look = Input.GetVector("look_left", "look_right", "look_up", "look_down");
		if (look.LengthSquared() > 0.01f)
		{
			var multiplier = Input.IsActionPressed("aim") ? _aimSensitivityMultiplier : 1;
			_yaw -= look.X * _controllerSensitivityHorizontal * multiplier * dt;
			_pitch -= look.Y * _controllerSensitivityVertical * multiplier * dt;
		}

		ApplyLookRotation();

	}

	public override void _PhysicsProcess(double delta)
	{
		var dt = (float)delta;
		ControllerLook(dt);

		if (_damageReductionTimer > 0f)
			_damageReductionTimer = Mathf.Max(0f, _damageReductionTimer - dt);

		Health += _healthRegenRate * _maxHealth * dt;


		var input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");

		var localDir = new Vector3(input.X, 0f, input.Y);
		var wishDir = (Transform.Basis * localDir).Normalized();

		var horizontal = new Vector3(Velocity.X, 0f, Velocity.Z);
		var moveSpeed = GetMoveSpeed(input);
		var target = wishDir * moveSpeed;

		horizontal = horizontal.Lerp(target, _accel * dt);

		var yVel = Velocity.Y;

		if (IsOnFloor())
		{
			if (Input.IsActionJustPressed("jump") && !IsStandingOnZombie())
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

	private bool IsStandingOnZombie()
	{
		for (var i = 0; i < GetSlideCollisionCount(); i++)
		{
			var collision = GetSlideCollision(i);
			if (collision.GetCollider() is Zombie && collision.GetNormal().Dot(Vector3.Up) > 0.7f)
				return true;
		}

		return false;
	}

	private float GetMoveSpeed(Vector2 input)
	{
		if (!_sprintEnabled)
			return _moveSpeed;

		if (!InputMap.HasAction("sprint") || !Input.IsActionPressed("sprint"))
			return _moveSpeed;

		bool movingForward = input.Y < 0f;
		return movingForward ? _moveSpeed * _sprintMultiplier : _moveSpeed;
	}

	public void TakeDamage(float damage, Vector3 hitGlobalPosition, Vector3 hitDir, float force)
	{
		if (_isDead || damage > 100f)
			return;

		var appliedDamage = _damageReductionTimer > 0f
			? damage * _damageReductionMultiplier
			: damage;

		Health -= appliedDamage;
		if (_damageReductionTimer <= 0f)
			_damageReductionTimer = _damageReductionDuration;

		AudioManager.I.Play3D(_hitSound, GlobalPosition, -15f);

		if (Health <= 0)
		{
			CallDeferred(nameof(HandleDeath));
		}
	}

	private void HandleDeath()
	{
		if (_isDead)
			return;

		_isDead = true;

		// Stop movement
		_collisionShape3D.Disabled = true;
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
