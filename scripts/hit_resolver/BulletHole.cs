using Godot;
using System;

public partial class BulletHole : MeshInstance3D
{
    [Export] private float _lifetimeSeconds = 3.0f;
    [Export] private float _fadeDuration = 0.5f;

    public override void _Ready()
    {
        var meshInstance = this;

        StandardMaterial3D material = null;

        if (meshInstance.MaterialOverride is StandardMaterial3D mo)
        {
            material = mo;
        }
        else
        {
            var active = meshInstance.GetActiveMaterial(0);
            material = active as StandardMaterial3D;
        }

        if (material == null)
        {
            GD.PushError("BulletHole requires a StandardMaterial3D either as MaterialOverride or on Mesh surface 0.");
            return;
        }

        // Duplicate material so we don't modify a shared resource.
        var duplicated = (StandardMaterial3D)material.Duplicate();

        // Re-assign to the same slot it came from.
        if (meshInstance.MaterialOverride is StandardMaterial3D)
            meshInstance.MaterialOverride = duplicated;
        else
            meshInstance.SetSurfaceOverrideMaterial(0, duplicated);

        material = duplicated;

        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;

        float delay = Mathf.Max(0f, _lifetimeSeconds - _fadeDuration);

        var tween = CreateTween();
        tween.TweenInterval(delay);
        tween.TweenProperty(material, "albedo_color", material.AlbedoColor with { A = 0.0f }, _fadeDuration);
        tween.TweenCallback(Callable.From(QueueFree));
    }
}
