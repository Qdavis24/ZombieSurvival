using Godot;
using System;
using System.Numerics;
using ZombieSurvival.scripts.zombie_package;
using Vector3 = Godot.Vector3;

public partial class HitResolver : Node
{
    [Export] private PackedScene _bulletHoleScene;

    public void HandleHit(HitInfo hit)
    {
        Node firsHit = hit.Collider;
        GD.Print($"Hit collider = {hit.Collider.Name}");
        if (hit.Collider.IsInGroup("damage"))
        {
            var damageObject = (IDamageable)hit.Collider;
            if (damageObject != null)
            {
                damageObject.TakeDamage(hit.Damage, hit.Point, hit.Direction, hit.Force);
            }

            return;
        }

        SpawnBulletHole(hit);
    }

    private void SpawnBulletHole(HitInfo hit)
    {
        if (_bulletHoleScene == null)
            return;

        //MeshInstance3D hole = _bulletHoleScene.Instantiate<MeshInstance3D>();
        BulletHole hole = _bulletHoleScene.Instantiate<BulletHole>();

        Vector3 pos = hit.Point + hit.Normal * 0.01f;

        Vector3 up = Mathf.Abs(hit.Normal.Dot(Vector3.Up)) > 0.9999f // figure out rotation axis by determining which axis is not co linear with normal
            ? Vector3.Forward
            : Vector3.Up;
        Basis basis = Basis.LookingAt(-hit.Normal, up);
        
        hole.GlobalTransform = new Transform3D(basis, pos);

        AddChild(hole);
    }
}