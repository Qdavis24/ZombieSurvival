using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
	private Node3D _head;

	[Export] private float _mouseSensitivity = 0.0025f;
	[Export] private float _moveSpeed = 6.0f;
	[Export] private float _accel = 14.0f;
	[Export] private float _gravity = 24.0f;
	
	private float _yaw; // left and right
	private float _pitch; // up and down
	
	public override void _Ready()
	{
		_head = GetNode<Node3D>("Head");
		
		Input.MouseMode = Input.MouseModeEnum.Captured;

		_yaw = Rotation.Y;
		_pitch = _head.Rotation.X;
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

		if (Input.MouseMode == Input.MouseModeEnum.Captured && @event is InputEventMouseMotion mouseMotion)
		{
			_yaw -= mouseMotion.Relative.X * _mouseSensitivity;
			_pitch -= mouseMotion.Relative.Y * _mouseSensitivity;
			
			_pitch = Mathf.Clamp(_pitch, -Mathf.Pi / 2f, Mathf.Pi / 2f);
			
			Rotation = new Vector3(0f, _yaw, 0f);
			_head.Rotation = new Vector3(_pitch, 0f, 0f);
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
		if (!IsOnFloor())
			yVel -= _gravity * dt;
		else
			yVel = 0f;

		Velocity = new Vector3(horizontal.X, yVel, horizontal.Z);
		MoveAndSlide();
	}
}
