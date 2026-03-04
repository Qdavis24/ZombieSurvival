using Godot;
using System;

public partial class HitResolver : Node
{
    [Export] private PackedScene _bulletHoleScene;

    public void HandleHit(HitInfo hit)
    {
        
        Node firsHit = hit.Collider;
        
        if (hit.Collider.IsInGroup("damage"))
        {
            var bone = (DismemberableBone) hit.Collider;
            if (bone != null)
            {
                bone.TakeDamage(hit.Damage, hit.Direction, hit.Force);
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

        Basis basis = Basis.LookingAt(-hit.Normal, Vector3.Up);

        hole.GlobalTransform = new Transform3D(basis, pos);

        AddChild(hole);
    }
}
