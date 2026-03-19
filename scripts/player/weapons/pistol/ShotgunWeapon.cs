using Godot;
using System;

public partial class ShotgunWeapon : WeaponBase
{
	[Export] private int _pelletCount = 8;

	protected override void ResolveShot(Vector3 from, Vector3 direction)
	{
		for (int i = 0; i < _pelletCount; i++)
		{
			var pelletDir = ApplySpread(direction);
			base.ResolveShot(from, pelletDir);
		}
	}
}