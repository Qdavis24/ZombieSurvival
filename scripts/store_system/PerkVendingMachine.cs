using System.Threading.Tasks;
using Godot;
using ZombieSurvival.scripts.inventory_system;

public partial class PerkVendingMachine : StaticBody3D
{
    public enum PurchaseStatus
    {
        Available,
        Owned,
        NotEnoughPoints,
        Busy,
        Invalid
    }

    [Export] private InteractNotifier _interactNotifier;
    [Export] private AudioStream _failedBuySound;
    [Export] private AudioStream _successfulBuySound;
    [Export] private Godot.Collections.Array<PerkVendingOption> _options = new();

    public Godot.Collections.Array<PerkVendingOption> Options => _options;

    public override void _Ready()
    {
        if (_interactNotifier == null)
        {
            GD.PrintErr($"{nameof(PerkVendingMachine)}: missing interact notifier.");
            return;
        }

        _interactNotifier.PlayerEnteredRange += OnPlayerEnteredRange;
        _interactNotifier.PlayerExitedRange += OnPlayerExitedRange;
        _interactNotifier.Interacted += OnInteracted;
        _interactNotifier.InteractPrompt = "press e to use perk vending machine";
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
        EventBus.Instance.EmitSignal(EventBus.SignalName.PerkVendingMenuRequested, this, player);
    }

    public PurchaseStatus GetPurchaseStatus(int optionIndex, Node3D player)
    {
        if (optionIndex < 0 || optionIndex >= _options.Count)
            return PurchaseStatus.Invalid;

        var option = _options[optionIndex];
        if (option == null)
            return PurchaseStatus.Invalid;

        if (player is not IInventoryOwner inventoryOwner)
            return PurchaseStatus.Invalid;

        var perkManager = GetPerkManager(player);
        if (perkManager == null)
            return PurchaseStatus.Invalid;

        if (perkManager.HasPerk(option.PerkType))
            return PurchaseStatus.Owned;

        if (inventoryOwner.Inventory.GetAmount(ItemType.Money) < option.Price)
            return PurchaseStatus.NotEnoughPoints;

        return perkManager.CanUsePerk(option.PerkType) ? PurchaseStatus.Available : PurchaseStatus.Busy;
    }

    public async Task<bool> TryPurchase(int optionIndex, Node3D player)
    {
        if (GetPurchaseStatus(optionIndex, player) != PurchaseStatus.Available)
        {
            AudioManager.I.Play3D(_failedBuySound, GlobalPosition);
            return false;
        }

        var option = _options[optionIndex];
        var inventoryOwner = (IInventoryOwner)player;
        var perkManager = GetPerkManager(player);

        if (!inventoryOwner.Inventory.ConsumeItem(ItemType.Money, option.Price))
        {
            AudioManager.I.Play3D(_failedBuySound, GlobalPosition);
            return false;
        }

        bool perkApplied;
        try
        {
            perkApplied = await perkManager.TryUsePerk(option.PerkType);
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"{nameof(PerkVendingMachine)}: failed to apply perk '{option.DisplayName}': {ex.Message}");
            inventoryOwner.Inventory.AddItem(ItemType.Money, option.Price);
            AudioManager.I.Play3D(_failedBuySound, GlobalPosition);
            return false;
        }

        if (!perkApplied)
        {
            inventoryOwner.Inventory.AddItem(ItemType.Money, option.Price);
            AudioManager.I.Play3D(_failedBuySound, GlobalPosition);
            return false;
        }

        AudioManager.I.Play3D(_successfulBuySound, GlobalPosition);
        return true;
    }

    private static PerkManager GetPerkManager(Node3D player)
    {
        return player.GetNodeOrNull<PerkManager>("PerkManager");
    }
}
