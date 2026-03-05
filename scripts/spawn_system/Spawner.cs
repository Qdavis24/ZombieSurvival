using Godot;
using System;

public partial class Spawner : Node3D
{
	[Export] private PackedScene _zombiePackedScene;

	[Export] private Timer _spawnTimer;

	[Export] private Node3D _target;

	[Export] private int _numPerWave = 5;
	
	[Export] private float _offset = 3f;
	
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_spawnTimer.Timeout += SpawnWave;
		_spawnTimer.Start();
	}

	private void SpawnWave()
	{
		for (int i = 0; i < _numPerWave; i++)
		{
			InstantiateZomb();
		}
	}

	private void InstantiateZomb()
	{
		var zomb = _zombiePackedScene.Instantiate<Zombie>();
		zomb.Init(_target);
		GetTree().Root.AddChild(zomb);
		zomb.GlobalTransform = GlobalTransform * new Transform3D(
			Basis,
			new Vector3(GD.Randf() * _offset, 0, GD.Randf() * _offset)
			);
	}
}
