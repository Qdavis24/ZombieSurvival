using Godot;
using System;

public partial class BloodSplatter : TextureRect
{
	private Tween _bloodTween;
	public void ShowBloodSplatter()
	{
		_bloodTween?.Kill();
		_bloodTween = CreateTween();
		Modulate = new Color(1, 1, 1, 1);
		_bloodTween.TweenProperty(this, "modulate:a", 0.0f, 1.5f)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.In);
	}
}
