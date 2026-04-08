using Godot;

public partial class Hud : CanvasLayer
{
    [ExportGroup("Nodes")]
    [Export] private Label _round;

    [ExportGroup("Animation")]
    [Export] private float _moveToCenterDuration = 0.25f;
    [Export] private float _waitBeforeTextChangeDuration = 0.6f;
    [Export] private float _textColorFadeDuration = 0.6f;
    [Export] private float _waitAfterTextChangeDuration = 0.6f;
    [Export] private float _moveBackDuration = 0.25f;
    
    [ExportGroup("sound")]
    [Export] private AudioStream _roundChangeSound;

    private Vector2 _roundOriginalPosition;
    private Tween _roundTween;

    private readonly int[] _values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
    private readonly string[] _numerals = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };

    public override void _Ready()
    {
        _roundOriginalPosition = _round.Position;
    }

    public void Init()
    {
        _round.Text = "";
    }

    public async void SetRound(int round)
    {
        if (_roundTween != null && _roundTween.IsValid())
        {
            _roundTween.Kill();
        }

        _round.Position = _roundOriginalPosition;

        Vector2 centerPosition = GetCenteredPosition();

        _roundTween = CreateTween();
        _roundTween.TweenProperty(_round, "position", centerPosition, _moveToCenterDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        await ToSignal(_roundTween, Tween.SignalName.Finished);

        await ToSignal(GetTree().CreateTimer(_waitBeforeTextChangeDuration), SceneTreeTimer.SignalName.Timeout);

        AudioManager.I.PlayUi(_roundChangeSound, -8f);
        
        _round.Text = IntToRoman(round);
        _round.Position = GetCenteredPosition();
        _round.Modulate = Colors.Red;

        Tween colorTween = CreateTween();
        colorTween.TweenProperty(_round, "modulate", Colors.White, _textColorFadeDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        await ToSignal(GetTree().CreateTimer(_waitAfterTextChangeDuration), SceneTreeTimer.SignalName.Timeout);

        _roundTween = CreateTween();
        _roundTween.TweenProperty(_round, "position", _roundOriginalPosition, _moveBackDuration)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.In);
    }

    private Vector2 GetCenteredPosition()
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        return (viewportSize - _round.Size) / 2.0f;
    }

    private string IntToRoman(int num)
    {
        var result = "";

        for (int i = 0; i < _values.Length; i++)
        {
            while (num >= _values[i])
            {
                result += _numerals[i];
                num -= _values[i];
            }
        }

        return result;
    }
}
