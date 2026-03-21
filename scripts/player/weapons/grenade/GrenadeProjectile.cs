using Godot;
using System;

public partial class GrenadeProjectile : RigidBody3D
{
	[Export] private float _throwSpeed;
	[Export] private float _fuseTime;

	public void Throw(Vector3 throwDirection)
	{
		LinearVelocity = throwDirection.Normalized() * _throwSpeed;
		
		// Add random spin
		Vector3 randomSpin = new Vector3(
			(float)GD.RandRange(-7f, 7f),
			(float)GD.RandRange(-7f, 7f),
			(float)GD.RandRange(-7f, 7f)
		);

		AngularVelocity = randomSpin;
	}
	
	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		await ToSignal(GetTree().CreateTimer(_fuseTime), SceneTreeTimer.SignalName.Timeout);
		Explode();
	}

	private void Explode()
	{
		GD.Print("BOOOOM");
		QueueFree();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
