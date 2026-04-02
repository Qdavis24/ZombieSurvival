using Godot;
using System;

public partial class HealthIndicator : ColorRect
{
	private Tween _hitTween;
	private ShaderMaterial _vignetteMaterial;

	public override void _Ready()
	{
		_vignetteMaterial = (ShaderMaterial) Material;
	}

	public void ShowHitFlash()
	{
		_hitTween?.Kill();
		_hitTween = CreateTween();
		_vignetteMaterial.SetShaderParameter("hit_flash", 1.0f);
		_hitTween.TweenMethod(
			Callable.From((float v) => _vignetteMaterial.SetShaderParameter("hit_flash", v)),
			1.0f, 0.0f, 0.3f
		);
	}

	public void SetIntensity(float intensity)
	{
		_vignetteMaterial.SetShaderParameter("intensity", intensity);
	}
}
