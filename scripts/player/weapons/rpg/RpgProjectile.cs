using Godot;
using System;

namespace ZombieSurvival.scripts.player.weapons;
public partial class RpgProjectile : Projectile
{
	[Export] private PackedScene _explodeScene;
	
	public override void _Ready()
	{
		BodyEntered += Explode;
	}

	private void Explode(Node body)
	{
		var explosion = _explodeScene.Instantiate<Explosion>();
		explosion.Init(_damage, _force);
		Containers.Instance.VFX.AddChild(explosion);
		explosion.GlobalPosition = GlobalPosition;
		QueueFree();
	}
}
