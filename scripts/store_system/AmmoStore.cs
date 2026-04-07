using Godot;
using ZombieSurvival.scripts.inventory_system;

public partial class AmmoStore : Node3D
{
    [Export] private InteractNotifier _interactNotifier;
    [Export] private PackedScene _ammoScene;
    [Export] private int _storeSellAmount;
    [Export] private ItemType _storeBuyItemType;
    [Export] private int _storeBuyAmount;

    
    [Export] private AudioStream _failedBuySound;
    [Export] private AudioStream _successfulBuySound;
    [Export] private Marker3D _marker;


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
        if (inventoryOwner.Inventory.ConsumeItem(_storeBuyItemType, _storeBuyAmount))
        {
            AudioManager.I.Play3D(_successfulBuySound, GlobalPosition, -20f);
            for (int i = 0; i < _storeSellAmount; i++)
                SpawnAmmo();
      
        }
        else
            AudioManager.I.Play3D(_failedBuySound, GlobalPosition);
    }

    private void SpawnAmmo()
    {
        var ammo = _ammoScene.Instantiate<Pickup>();
        Containers.Instance.VFX.AddChild(ammo);
        ammo.GlobalPosition = _marker.GlobalPosition;
        ammo.ApplyImpulse(_marker.Basis.Z  * 1f);
    }
}