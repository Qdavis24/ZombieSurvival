using Godot;
using System;

public partial class BulletHole : MeshInstance3D
{
    [Export] private float _lifetimeSeconds = 3.0f;
    [Export] private float _fadeDuration = 0.5f;

    public override void _Ready()
    {
        var meshInstance = this;

        if (meshInstance.MaterialOverride is not StandardMaterial3D material)
        {
            GD.PushError("BulletHole requires a StandardMaterial3D as MaterialOverride.");
            return;
        }

        // Duplicate material so we don't modify the shared resource
        material = (StandardMaterial3D)material.Duplicate();
        meshInstance.MaterialOverride = material;

        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

        float delay = Mathf.Max(0f, _lifetimeSeconds - _fadeDuration);

        var tween = CreateTween();
        tween.TweenInterval(delay);
        tween.TweenProperty(material, "albedo_color", material.AlbedoColor with { A = 0.0f }, _fadeDuration);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
