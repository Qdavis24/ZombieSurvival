using Godot;
using System;

public partial class ShotgunWeapon : WeaponBase
{
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
		Anim.Play("hip_reload_prep", AnimBlendTime);
	}

	protected override void RequestReloadCancel()
	{
		_reloadCancelRequested = true;
	}

	protected override bool HandleReloadAnimationFinished(StringName animName)
	{
		if (animName == "hip_reload_prep")
		{
			if (CurrentAmmo >= MagazineSize || ReserveAmmo <= 0)
			{
				Anim.Play("hip_reload_end", AnimBlendTime);
			}
			else
			{
				Anim.Play("hip_reload", AnimBlendTime);
			}

			return true;
		}

		if (animName == "hip_reload")
		{
			TryLoadOneRoundIntoMagazine();

			if (_reloadCancelRequested || CurrentAmmo >= MagazineSize || ReserveAmmo <= 0)
			{
				Anim.Play("hip_reload_end", AnimBlendTime);
			}
			else
			{
				Anim.Play("hip_reload", AnimBlendTime);
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
