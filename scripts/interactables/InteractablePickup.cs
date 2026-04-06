using Godot;
using ZombieSurvival.scripts.inventory_system;

public partial class InteractablePickup : Node3D
{
    [Export] private InteractNotifier _interactNotifier;
    [Export] private ItemType _item;
    [Export] private MeshInstance3D _pickupMesh;
    private Material _pickupMaterial;
    private Material _highlightMaterial;

    public override void _Ready()
    {
        _interactNotifier.PlayerEnteredRange += OnPlayerEnteredRange;
        _interactNotifier.PlayerExitedRange += OnPlayerExitedRange;
        _interactNotifier.Interacted += OnInteracted;
        _pickupMaterial = _pickupMesh.GetActiveMaterial(0);
        _highlightMaterial = _pickupMaterial.NextPass;
        _pickupMaterial.NextPass = null;
    }

    private void OnPlayerEnteredRange()
    {
        _pickupMaterial.NextPass = _highlightMaterial;
        EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerEnteredInteractableRange, _interactNotifier);
    }

    private void OnPlayerExitedRange()
    {
        _pickupMaterial.NextPass = null;
        EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerExitedInteractableRange, _interactNotifier);
    }

    private void OnInteracted(Node3D player)
    {
        if (player is not IInventoryOwner inventoryOwner) return;
        inventoryOwner.Inventory.AddItem(_item, 1);
        _interactNotifier.Disable();
        QueueFree();
    }
}
