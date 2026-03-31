using Godot;

public partial class VfxHud : CanvasLayer
{
    [Export] private ColorRect _vignette;
    [Export] private TextureRect _bloodSplatter;

    private ShaderMaterial _vignetteMaterial;
    private Tween _hitTween;
    private Tween _bloodTween;

    public override void _Ready()
    {
        _vignetteMaterial = (ShaderMaterial)_vignette.Material;
        _bloodSplatter.Modulate = new Color(1, 1, 1, 0);
    }

    public void UpdateHealth(float current, float max)
    {
        float intensity = 1.0f - (current / max);
        _vignetteMaterial.SetShaderParameter("intensity", intensity);
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

    public void ShowBloodSplatter()
    {
        _bloodTween?.Kill();
        _bloodTween = CreateTween();
        _bloodSplatter.Modulate = new Color(1, 1, 1, 1);
        _bloodTween.TweenProperty(_bloodSplatter, "modulate:a", 0.0f, 1.5f)
            .SetTrans(Tween.TransitionType.Expo)
            .SetEase(Tween.EaseType.In);
    }
}