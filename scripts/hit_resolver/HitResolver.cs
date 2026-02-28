using Godot;
using System;

public partial class HitResolver : Node
{
    [Export] private PackedScene _bulletHoleScene;

    public void HandleHit(HitInfo hit)
    {
        
        Node zombieRoot = FindZombieRoot(hit.Collider);
        
        if (zombieRoot != null)
        {
            // SET UP TO WORK WITH REAL ZOMBIE
            
            // var health = zombieRoot.GetNodeOrNull<Health>("Health");
            // if (health != null)
            // {
            //     health.TakeDamage(hit.Damage);
            // }
            //
            // return;
        }

        SpawnBulletHole(hit);
    }

    private Node FindZombieRoot(Node start)
    {
        Node current = start;

        while (current != null)
        {
            if (current.IsInGroup("zombie"))
                return current;

            current = current.GetParent();
        }

        return null;
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
