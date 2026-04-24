using Godot;
using System;

public partial class ShotgunWeapon : WeaponBase
{
	[Export] private AudioStream _reloadSound;
	[Export] private AudioStream _reloadShellSound;
	[Export] private int _pelletCount = 8;
	private bool _reloadCancelRequested;

	protected override void ResolveShot(Vector3 from, Vector3 direction)
	{
		for (int i = 0; i < _pelletCount; i++)
		{
			var pelletDir = ApplySpread(direction);
			base.ResolveShot(from, pelletDir);
		}
	}
	protected override void StartReload()
	{
		BeginReloadState();
		_reloadCancelRequested = false;
		PlayReloadAnimation("hip_reload_prep");
	}

	protected override void RequestReloadCancel()
	{
		_reloadCancelRequested = true;
	}

	protected override bool HandleReloadAnimationFinished(StringName animName)
	{
		if (animName == "hip_reload_prep")
		{
			if (CurrentAmmo >= MagazineSize)
			{
				PlayReloadAnimation("hip_reload_end");
				AudioManager.I.Play3D(_reloadSound, GlobalPosition, -8f);
			}
			else
			{
				PlayReloadAnimation("hip_reload");
				AudioManager.I.Play3D(_reloadShellSound, GlobalPosition, -16f);
			}

			return true;
		}

		if (animName == "hip_reload")
		{
			bool loaded = TryLoadOneRoundIntoMagazine();

			if (_reloadCancelRequested || CurrentAmmo >= MagazineSize || !loaded)
			{
				PlayReloadAnimation("hip_reload_end");
				AudioManager.I.Play3D(_reloadSound, GlobalPosition, -8f);
			}
			else
			{
				PlayReloadAnimation("hip_reload");
				AudioManager.I.Play3D(_reloadShellSound, GlobalPosition, -16f);
			}

			return true;
		}

		if (animName == "hip_reload_end")
		{
			_reloadCancelRequested = false;
			FinishReloadState();
			return true;
		}

		return base.HandleReloadAnimationFinished(animName);
	}
}
