using Godot;

public partial class VfxHud : CanvasLayer
{
    [Export] private ColorRect _vignette;
    [Export] private TextureRect _bloodSplatter;

    private ShaderMaterial _vignetteMaterial;
    private Tween _hitTween;
    private Tween _bloodTween;
    private PlayerController _player;

    public void Init(PlayerController player)
    {
        _player = player;
    }

    public override void _Ready()
    {
        _vignetteMaterial = (ShaderMaterial)_vignette.Material;
        _bloodSplatter.Modulate = new Color(1, 1, 1, 0);
    }

    public void UpdateHealth()
    {
        float intensity = 1.0f - (_player.Health / _player.MaxHealth);
        _vignetteMaterial.SetShaderParameter("intensity", intensity);
    }

    public override void _PhysicsProcess(double delta)
    {
        UpdateHealth();
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