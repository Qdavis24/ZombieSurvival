using Godot;

public partial class VendingMachineLight : SpotLight3D
{
    [Export] public float BaseEnergy = 3.0f;
    [Export] public float IdleJitter = 0.08f;

    [Export] public float MinStepTime = 0.015f;
    [Export] public float MaxStepTime = 0.09f;

    [Export] public float GlitchChancePerSecond = 1.35f;
    [Export] public float MinGlitchDuration = 0.06f;
    [Export] public float MaxGlitchDuration = 0.24f;

    [Export] public float MinGlitchEnergyMultiplier = 0.0f;
    [Export] public float MaxGlitchEnergyMultiplier = 0.45f;

    private readonly RandomNumberGenerator _rng = new();

    private float _stepTimer;
    private float _glitchTimer;
    private bool _isGlitching;

    public override void _Ready()
    {
        _rng.Randomize();
        LightEnergy = BaseEnergy;
        _stepTimer = 0.0f;
        _glitchTimer = 0.0f;
        _isGlitching = false;
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        if (!_isGlitching && _rng.Randf() < GlitchChancePerSecond * dt)
        {
            _isGlitching = true;
            _glitchTimer = _rng.RandfRange(MinGlitchDuration, MaxGlitchDuration);
        }

        if (_isGlitching)
        {
            _glitchTimer -= dt;
            if (_glitchTimer <= 0.0f)
            {
                _isGlitching = false;
            }
        }

        _stepTimer -= dt;
        if (_stepTimer <= 0.0f)
        {
            _stepTimer = _rng.RandfRange(MinStepTime, MaxStepTime);

            float multiplier;
            if (_isGlitching)
            {
                // Hard flicker: deep, abrupt drops and occasional spikes.
                if (_rng.Randf() < 0.15f)
                {
                    multiplier = _rng.RandfRange(1.05f, 1.35f);
                }
                else
                {
                    multiplier = _rng.RandfRange(MinGlitchEnergyMultiplier, MaxGlitchEnergyMultiplier);
                }
            }
            else
            {
                multiplier = _rng.RandfRange(1.0f - IdleJitter, 1.0f + IdleJitter);
            }

            LightEnergy = BaseEnergy * multiplier;
        }
    }
}
