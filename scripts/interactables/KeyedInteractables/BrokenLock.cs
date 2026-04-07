using Godot;
using System;

public partial class BrokenLock : RigidBody3D
{
	[Export] private Timer _freeTimer;
	[Export] private GpuParticles3D _effect;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_effect.Emitting = true;
		_freeTimer.Timeout += QueueFree;
		_freeTimer.Start();
	}

}
