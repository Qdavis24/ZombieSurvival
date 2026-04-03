using Godot;
using ZombieSurvival.scripts.inventory_system;

public partial class AmmoStore : InteractableBase
{
    public override string InteractPrompt => "Press E to buy ammo";

    [Export] private ItemType _storeSellItemType;
    [Export] private int _storeSellAmount;
    [Export] private ItemType _storeBuyItemType;
    [Export] private int _storeBuyAmount;
    [Export] private OmniLight3D _light;

    public override void _Ready()
    {
        base._Ready();
        _light.Visible = false;
    }

    protected override void OnInteract()
    {
        if (InteractionRangeBody is IInventoryOwner inventoryOwner)
        {
            if (inventoryOwner.Inventory.ConsumeItem(_storeBuyItemType, _storeBuyAmount))
                inventoryOwner.Inventory.AddItem(_storeSellItemType, _storeSellAmount);
        }
    }

    protected override void OnPlayerEnteredRange(Node3D body)
    {
        _light.Visible = true;
    }

    protected override void OnPlayerExitedRange(Node3D body)
    {
        _light.Visible = false;
    }
}
