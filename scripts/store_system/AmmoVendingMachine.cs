using Godot;
using ZombieSurvival.scripts.inventory_system;

public partial class AmmoVendingMachine : StaticBody3D
{
    [Export] private InteractNotifier _interactNotifier;
    [Export] private Marker3D _spawnMarker;
    [Export] private AudioStream _failedBuySound;
    [Export] private AudioStream _successfulBuySound;
    [Export] private Godot.Collections.Array<AmmoVendingOption> _options = new();

    public Godot.Collections.Array<AmmoVendingOption> Options => _options;

    public override void _Ready()
    {
        if (_interactNotifier == null)
        {
            GD.PrintErr($"{nameof(AmmoVendingMachine)}: missing interact notifier.");
            return;
        }

        _interactNotifier.PlayerEnteredRange += OnPlayerEnteredRange;
        _interactNotifier.PlayerExitedRange += OnPlayerExitedRange;
        _interactNotifier.Interacted += OnInteracted;
        _interactNotifier.InteractPrompt = "press e to use ammo vending machine";
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
        EventBus.Instance.EmitSignal(EventBus.SignalName.AmmoVendingMenuRequested, this, player);
    }

    public bool TryPurchase(int optionIndex, Node3D player)
    {
        if (optionIndex < 0 || optionIndex >= _options.Count)
            return false;

        if (player is not IInventoryOwner inventoryOwner)
            return false;

        var option = _options[optionIndex];
        if (option == null || option.AmmoScene == null)
            return false;

        if (!inventoryOwner.Inventory.ConsumeItem(ItemType.Money, option.Price))
        {
            AudioManager.I.Play3D(_failedBuySound, GlobalPosition);
            return false;
        }

        AudioManager.I.Play3D(_successfulBuySound, GlobalPosition);
        for (int i = 0; i < option.SpawnCount; i++)
            SpawnAmmo(option.AmmoScene);

        return true;
    }

    private void SpawnAmmo(PackedScene ammoScene)
    {
        var ammo = ammoScene.Instantiate<Pickup>();
        Containers.Instance.VFX.AddChild(ammo);

        var spawnTransform = _spawnMarker?.GlobalTransform ?? GlobalTransform;
        ammo.GlobalPosition = spawnTransform.Origin;
        ammo.ApplyImpulse(spawnTransform.Basis.Z * 1f);
    }
}
