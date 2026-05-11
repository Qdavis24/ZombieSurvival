using Godot;
using ZombieSurvival.scripts.inventory_system;
using ZombieSurvival.scripts.player.weapons;

public partial class WeaponUpgradeBench : StaticBody3D
{
    [Export] private InteractNotifier _interactNotifier;
    [Export] private AudioStream _failedBuySound;
    [Export] private AudioStream _successfulBuySound;
    [Export] private Godot.Collections.Array<WeaponUpgradeOption> _options = new();

    public Godot.Collections.Array<WeaponUpgradeOption> Options => _options;

    public override void _Ready()
    {
        if (_interactNotifier == null)
        {
            GD.PrintErr($"{nameof(WeaponUpgradeBench)}: missing interact notifier.");
            return;
        }

        _interactNotifier.PlayerEnteredRange += OnPlayerEnteredRange;
        _interactNotifier.PlayerExitedRange += OnPlayerExitedRange;
        _interactNotifier.Interacted += OnInteracted;
        _interactNotifier.InteractPrompt = "press e to use weapon upgrade bench";
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
        EventBus.Instance.EmitSignal(EventBus.SignalName.WeaponUpgradeMenuRequested, this, player);
    }

    public WeaponManager.WeaponUpgradeStatus GetPurchaseStatus(int optionIndex, Node3D player)
    {
        if (optionIndex < 0 || optionIndex >= _options.Count)
            return WeaponManager.WeaponUpgradeStatus.Invalid;

        var option = _options[optionIndex];
        if (option == null)
            return WeaponManager.WeaponUpgradeStatus.Invalid;

        if (player is not IInventoryOwner inventoryOwner)
            return WeaponManager.WeaponUpgradeStatus.Invalid;

        var weaponManager = GetWeaponManager(player);
        if (weaponManager == null)
            return WeaponManager.WeaponUpgradeStatus.Invalid;

        var status = weaponManager.GetUpgradeStatus(option.Target);
        if (status != WeaponManager.WeaponUpgradeStatus.Available)
            return status;

        return inventoryOwner.Inventory.GetAmount(ItemType.Money) >= option.Price
            ? WeaponManager.WeaponUpgradeStatus.Available
            : WeaponManager.WeaponUpgradeStatus.NotEnoughPoints;
    }

    public bool TryPurchase(int optionIndex, Node3D player)
    {
        if (GetPurchaseStatus(optionIndex, player) != WeaponManager.WeaponUpgradeStatus.Available)
        {
            AudioManager.I.Play3D(_failedBuySound, GlobalPosition);
            return false;
        }

        var option = _options[optionIndex];
        var inventoryOwner = (IInventoryOwner)player;
        var weaponManager = GetWeaponManager(player);

        if (!inventoryOwner.Inventory.ConsumeItem(ItemType.Money, option.Price))
        {
            AudioManager.I.Play3D(_failedBuySound, GlobalPosition);
            return false;
        }

        if (!weaponManager.TryUpgradeWeapon(option.Target))
        {
            inventoryOwner.Inventory.AddItem(ItemType.Money, option.Price);
            AudioManager.I.Play3D(_failedBuySound, GlobalPosition);
            return false;
        }

        AudioManager.I.Play3D(_successfulBuySound, GlobalPosition);
        return true;
    }

    private static WeaponManager GetWeaponManager(Node3D player)
    {
        return player.GetNodeOrNull<WeaponManager>("WeaponManager");
    }
}
