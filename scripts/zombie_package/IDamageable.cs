using Godot;

namespace ZombieSurvival.scripts.zombie_package;

public interface IDamageable
{
    public void TakeDamage(float damage, Vector3 hitGlobalPosition, Vector3 hitDir, float force){}
}