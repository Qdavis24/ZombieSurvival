using Godot;
using System;
using System.Threading.Tasks;
using ZombieSurvival.scripts.inventory_system;
using ZombieSurvival.scripts.player.weapons;
using ZombieSurvival.scripts.player.weapons.grenade;

namespace ZombieSurvival.scripts.player.weapons;

public partial class GrenadeManager : Node
{
    [Signal]
    public delegate void GrenadesChangedEventHandler(int currentGrenades);

    [Signal]
    public delegate void GrenadeThrowFailedEventHandler();

    [Export] private PlayerController _playerController;
    [Export] private WeaponManager _weaponManager;
    [Export] private Node3D _weaponSocket;
    [Export] private PackedScene _grenadeThrowScene;
    [Export] private int _startingGrenadeCount = 8;

    public override void _Ready()
    {
        _playerController.Inventory.ItemAdded += OnPlayerInventoryItemAdded;
        _playerController.Inventory.ItemRemoved += OnPlayerInventoryItemRemoved;
        _playerController.Inventory.AddItem(ItemType.Grenades, _startingGrenadeCount);

        CallDeferred(nameof(RefreshHudGrenades));
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("throw_grenade"))
            TryStartGrenadeThrow();
    }

    private void OnPlayerInventoryItemAdded(ItemType itemType, int amount)
    {
        if (itemType == ItemType.Grenades)
            RefreshHudGrenades();
    }

    private void OnPlayerInventoryItemRemoved(ItemType itemType, int amount)
    {
        if (itemType == ItemType.Grenades)
            RefreshHudGrenades();
    }

    private void RefreshHudGrenades()
    {
        var count = _playerController.Inventory.GetAmount(ItemType.Grenades);
        EmitSignal(SignalName.GrenadesChanged, count);
    }

    private async void TryStartGrenadeThrow()
    {
        if (_weaponManager.IsHandsBusy)
            return;

        if (_playerController.Inventory.GetAmount(ItemType.Grenades) <= 0)
        {
            EmitSignal(SignalName.GrenadeThrowFailed);
            return;
        }

        if (_grenadeThrowScene == null)
            return;

        await _weaponManager.PlayTemporaryHandAction(
            WeaponManager.HandActionState.ThrowingGrenade,
            PlayGrenadeThrow
        );
    }

    private async Task PlayGrenadeThrow()
    {
        _playerController.Inventory.ConsumeItem(ItemType.Grenades, 1);

        var grenadeThrowNode = _grenadeThrowScene.Instantiate<Grenade>();
        _weaponSocket.AddChild(grenadeThrowNode);

        await grenadeThrowNode.ThrowGrenade();

        grenadeThrowNode.QueueFree();
    }
}
