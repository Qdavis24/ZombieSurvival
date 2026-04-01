using Godot;
using System;
using ZombieSurvival.scripts.inventory_system;

public partial class AmmoStore : Node3D
{
    [Signal]
    public delegate void InventoryOwnerEnteredStoreRangeEventHandler();

    [Signal]
    public delegate void InventoryOwnerExitedStoreRangeEventHandler();

    [Export] private Area3D _buyRange;
    [Export] private ItemType _storeSellItemType;
    [Export] private int _storeSellAmount;
    [Export] private ItemType _storeBuyItemType;
    [Export] private int _storeBuyAmount;
    [Export] private OmniLight3D _light;


    private bool _inRange;
    private IInventoryOwner _inventoryOwner;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _light.Visible = false;
        _buyRange.BodyEntered += OnBodyEntered;
        _buyRange.BodyExited += OnBodyExited;
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("interact") && _inRange)
        {
            TryBuy();
        }
    }

    private void TryBuy()
    {
        if (_inventoryOwner == null) return;
        if (_inventoryOwner.Inventory.ConsumeItem(_storeBuyItemType, _storeBuyAmount))
        {
            _inventoryOwner.Inventory.AddItem(_storeSellItemType, _storeSellAmount);
        }
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is IInventoryOwner inventoryOwner)
        {
            _light.Visible = false;
            _inventoryOwner = null;
            EmitSignalInventoryOwnerExitedStoreRange();
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is IInventoryOwner inventoryOwner)
        {
            _light.Visible = true;
            _inRange = true;
            _inventoryOwner = inventoryOwner;
            EmitSignalInventoryOwnerExitedStoreRange();
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}