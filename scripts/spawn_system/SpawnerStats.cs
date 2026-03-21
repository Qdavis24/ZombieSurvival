namespace ZombieSurvival.scripts.spawn_system;

public struct SpawnerStats
{
    public float ZombieSpawnTimerInterval;
    public int NumZombiesRoundLimit; // how many zombies is this spawner manager allowed to spawn
    public int NumZombiesAliveLimit; // how many zombies can this spawner manager have alive at once
}