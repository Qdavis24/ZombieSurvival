using Godot;
using System;

public partial class Blood : GpuParticles3D
{
    public override void _Ready()
    {
        base._Ready();
        Finished += QueueFree;
        Emitting = true;
    }
}
