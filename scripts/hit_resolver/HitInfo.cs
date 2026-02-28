using Godot;

public readonly struct HitInfo
{
    public readonly Node Collider;
    public readonly Vector3 Point;
    public readonly Vector3 Normal;
    public readonly Vector3 Direction;
    public readonly float Damage;
    public readonly float Force;

    public HitInfo(Node collider, Vector3 point, Vector3 normal, Vector3 direction, float damage, float force)
    {
        Collider = collider;
        Point = point;
        Normal = normal;
        Direction = direction;
        Damage = damage;
        Force = force;
    }
}