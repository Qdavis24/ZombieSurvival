using Godot;
using System;

public partial class MuzzleFlash : GpuParticles3D
{
    [Export] private OmniLight3D _light;
    [Export] private Timer _lightTimer;
    public override void _Ready()
    {
        base._Ready();
        _lightTimer.WaitTime = Lifetime-.01f;
        Finished += Deactivate;
        _lightTimer.Timeout += Deactivate;
    }

    public void Activate()
    {
        
        _light.Visible = true;
        _lightTimer.Start();
        Restart();
        Emitting = true;
        
    }
    
    public void Deactivate()
    {
        _light.Visible = false;
    }
}
