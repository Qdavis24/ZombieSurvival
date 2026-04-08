using Godot;
using System;

public partial class Skin : MeshInstance3D
{
    [Export] private Material[] _materials = Array.Empty<Material>();

    public override void _Ready()
    {
        if (_materials == null || _materials.Length == 0)
            return;

        int lastIndex = _materials.Length - 1;

        // Make last material rare (currently 10% chance) make millitary skin rarer
        int randomIndex;
        if (GD.Randf() < 0.1f)
        {
            randomIndex = lastIndex;
        }
        else
        {
            randomIndex = GD.RandRange(0, lastIndex - 1);
        }
        Material selectedMaterial = _materials[randomIndex];
        if (selectedMaterial == null)
            return;

        SetSurfaceOverrideMaterial(0, selectedMaterial);
    }
}
