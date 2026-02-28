using Godot;
using System;

public partial class LimbContainer : Node3D
{
	[Export] private Timer _timer;
	[Export] public PackedScene Hinge;
	public override void _Ready()
	{
		_timer.Timeout += QueueFree;
		_timer.Start();
	}
	
	public void Init(float lifetime)
	{
		_timer.WaitTime = lifetime;
	}
}
