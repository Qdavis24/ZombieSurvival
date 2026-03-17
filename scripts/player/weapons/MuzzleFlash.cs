using Godot;
using System;

public partial class MuzzleFlash : GpuParticles3D
{
    [Export] private OmniLight3D _light;
    public override void _Ready()
    {
        base._Ready();
        Finished += Deactivate;
    }

    public void Activate()
    {
        Emitting = true;
        _light.Visible = true;
    }
    
    public void Deactivate()
    {
        _light.Visible = false;
    }
}
