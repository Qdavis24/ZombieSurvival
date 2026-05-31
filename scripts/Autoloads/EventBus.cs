using Godot;
using ZombieSurvival.scripts.shared;

public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }

    [Signal]
    public delegate void PlayerEnteredInteractableRangeEventHandler(InteractNotifier interactable);

    [Signal]
    public delegate void PlayerExitedInteractableRangeEventHandler(InteractNotifier interactable);

    [Signal]
    public delegate void MapAreaUnlockedEventHandler(int mapArea);

    [Signal]
    public delegate void EnemyHitEventHandler(bool headshot);

    [Signal]
    public delegate void ZombieKilledEventHandler(bool headshot);

    [Signal]
    public delegate void AmmoVendingMenuRequestedEventHandler(AmmoVendingMachine machine, Node3D player);

    [Signal]
    public delegate void PerkVendingMenuRequestedEventHandler(PerkVendingMachine machine, Node3D player);

    [Signal]
    public delegate void WeaponUpgradeMenuRequestedEventHandler(WeaponUpgradeBench bench, Node3D player);

    public override void _Ready()
    {
        Instance = this;
    }
}
