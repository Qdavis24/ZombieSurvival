using Godot;

namespace ZombieSurvival.scripts.damage_system;

public interface IDamageable
{
    public void TakeDamage(float damage, Vector3 hitGlobalPosition, Vector3 hitDir, float force){}
}