using Godot;

// A code-drawn "tick cross" hit marker: four short diagonal segments around the
// crosshair that punch + fade on a hit. White for body hits, red for headshots.
public partial class HitMarker : Control
{
    [Export] private float _size = 48f;
    [Export] private float _innerRadius = 5f;
    [Export] private float _outerRadius = 13f;
    [Export] private float _thickness = 2.5f;
    [Export] private float _duration = 0.18f;
    [Export] private Color _bodyColor = new(1f, 1f, 1f);
    [Export] private Color _headshotColor = new(1f, 0.23f, 0.19f);

    private static readonly Vector2[] Diagonals =
    {
        new Vector2(1, 1).Normalized(),
        new Vector2(1, -1).Normalized(),
        new Vector2(-1, 1).Normalized(),
        new Vector2(-1, -1).Normalized(),
    };

    private Color _drawColor = Colors.White;
    private Tween _tween;
    private bool _flashQueued;
    private bool _queuedHeadshot;

    public override void _Ready()
    {
        // Anchor to screen center and offset by half size so the control is truly centered.
        AnchorLeft = AnchorTop = AnchorRight = AnchorBottom = 0.5f;
        OffsetLeft = -_size / 2f;
        OffsetTop = -_size / 2f;
        OffsetRight = _size / 2f;
        OffsetBottom = _size / 2f;
        PivotOffset = new Vector2(_size, _size) / 2f;
        MouseFilter = MouseFilterEnum.Ignore;
        Modulate = new Color(1, 1, 1, 0); // start hidden
    }

    public override void _Draw()
    {
        var center = Size / 2f;
        foreach (var dir in Diagonals)
            DrawLine(center + dir * _innerRadius, center + dir * _outerRadius, _drawColor, _thickness, true);
    }

    // Called once per hit. Pierce/shotgun can land several hits in one frame, so we
    // coalesce them into a single flash and let a headshot win the colour.
    public void Flash(bool headshot)
    {
        _queuedHeadshot = _queuedHeadshot || headshot;
        if (_flashQueued) return;

        _flashQueued = true;
        Callable.From(DoFlash).CallDeferred();
    }

    private void DoFlash()
    {
        _drawColor = _queuedHeadshot ? _headshotColor : _bodyColor;
        _flashQueued = false;
        _queuedHeadshot = false;
        QueueRedraw();

        _tween?.Kill();
        Scale = new Vector2(1.35f, 1.35f);
        Modulate = new Color(1, 1, 1, 1);

        _tween = CreateTween().SetParallel();
        _tween.TweenProperty(this, "scale", Vector2.One, _duration)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        _tween.TweenProperty(this, "modulate:a", 0f, _duration)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
    }
}
