using Godot;
using ZombieSurvival.scripts.inventory_system;

public partial class ShotgunPickup : Node3D
{
    [Export] private InteractNotifier _interactNotifier;
    [Export] private ItemType _item;
    [Export] private int _amount;

    public override void _Ready()
    {
        _interactNotifier.PlayerEnteredRange += OnPlayerEnteredRange;
        _interactNotifier.PlayerExitedRange += OnPlayerExitedRange;
        _interactNotifier.Interacted += OnInteracted;
    }

    private void OnPlayerEnteredRange()
    {
        EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerEnteredInteractableRange, _interactNotifier);
    }

    private void OnPlayerExitedRange()
    {
        EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerExitedInteractableRange, _interactNotifier);
    }

    private void OnInteracted(Node3D player)
    {
        if (player is not IInventoryOwner inventoryOwner) return;
        inventoryOwner.Inventory.AddItem(_item, _amount);
        _interactNotifier.Disable();
        QueueFree();
    }
}
