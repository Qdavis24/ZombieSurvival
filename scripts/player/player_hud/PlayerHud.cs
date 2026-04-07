using System;
using System.Collections.Generic;
using Godot;
using ZombieSurvival.scripts.inventory_system;

public partial class PlayerHud : CanvasLayer
{
    private struct PickupNotification
    {
        public ItemType ItemType;
        public int ItemAmount;
    };

    [Export] private HealthIndicator _healthIndicator;
    [Export] private BloodSplatter _bloodSplatter;
    [Export] private Label _currentAmmoLabel;
    [Export] private Label _reserveAmmoLabel;
    [Export] private Label _currentGrenadesLabel;
    [Export] private Label _parasiticMaterialLabel;
    [Export] private PlayerPopup _interactPopup;
    [Export] private PlayerPopup _itemPopup;

    private Dictionary<ItemGroup, Color> _itemGroupColorMap = new()
    {
        { ItemGroup.Ammo, Colors.DarkGoldenrod },
        { ItemGroup.Currency, new Color(255, 255, 0) },
        { ItemGroup.Weapon, Colors.Purple },
        { ItemGroup.Key, Colors.Gold },
        { ItemGroup.Consumable, Colors.Green }
    };

    private Queue<PickupNotification> _itemNotifications = new();
    private bool _notificationsBusy;


    public override void _Ready()
    {
        if (EventBus.Instance == null)
        {
            GD.PrintErr($"{nameof(PlayerHud)}: EventBus autoload is missing. Add it in Project > Autoloads.");
            return;
        }

        EventBus.Instance.PlayerEnteredInteractableRange += OnPlayerEnteredInteractableRange;
        EventBus.Instance.PlayerExitedInteractableRange += OnPlayerExitedInteractableRange;
        _itemPopup.PopupFree += OnPopupFree;
    }
    

    public override void _ExitTree()
    {
        if (EventBus.Instance == null) return;

        EventBus.Instance.PlayerEnteredInteractableRange -= OnPlayerEnteredInteractableRange;
        EventBus.Instance.PlayerExitedInteractableRange -= OnPlayerExitedInteractableRange;
    }

    private void OnPopupFree()
    {
        _notificationsBusy = false;
        PickupNotification currNotification;
        if (_itemNotifications.TryDequeue(out currNotification))
        {
            var itemType = currNotification.ItemType;
            var amount = currNotification.ItemAmount;
            while (_itemNotifications.TryDequeue(out currNotification) && currNotification.ItemType == itemType)
                amount += currNotification.ItemAmount;
            ShowPickup(itemType, amount);
        }
    }

    private void OnPlayerEnteredInteractableRange(InteractNotifier interactable)
    {
        _interactPopup.ShowMessage(interactable.InteractPrompt);
    }

    private void OnPlayerExitedInteractableRange(InteractNotifier interactable)
    {
        _interactPopup.HideMessage();
    }

    public void UpdateHealthIndicator(float currHealth, float maxHealth)
    {
        _healthIndicator.SetIntensity(1 - currHealth / maxHealth);
    }

    public void ShowHitFlash()
    {
        _healthIndicator.ShowHitFlash();
    }

    public void SetAmmo(int currentAmmo, int reserveAmmo)
    {
        _currentAmmoLabel.Text = currentAmmo.ToString();
        _reserveAmmoLabel.Text = reserveAmmo.ToString();
    }

    public void SetGrenades(int currentGrenades)
    {
        _currentGrenadesLabel.Text = currentGrenades.ToString();
    }

    public void SetParasiticMaterial(int amount)
    {
        _parasiticMaterialLabel.Text = amount.ToString();
    }

    public async void ShowPickup(ItemType itemType, int amount)
    {
        if (_notificationsBusy)
        {
            _itemNotifications.Enqueue(
                new PickupNotification { ItemType = itemType, ItemAmount = amount });
        }
        else
        {
            _notificationsBusy = true;
            var message = "";
            var color = Colors.Red;
            
            if (amount > 0)
            {
                message = $"Picked up {amount} {itemType}";
                color = _itemGroupColorMap[itemType.GetGroup()];
            }
            else
            {
                message = $"Consumed {amount} {itemType}";
            }
            
            await _itemPopup.ShowNotification(message, color);
        }
    }

    public void ReloadFailed()
    {
        if (_currentAmmoLabel == null || _reserveAmmoLabel == null)
            return;
        
        AudioManager.I.PlayUiClick();

        // Immediately flash red
        _currentAmmoLabel.Modulate = Colors.Red;
        _reserveAmmoLabel.Modulate = Colors.Red;

        // Tween back to white
        var tween = CreateTween();
        tween.TweenProperty(_currentAmmoLabel, "modulate", Colors.White, 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        tween.Parallel().TweenProperty(_reserveAmmoLabel, "modulate", Colors.White, 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }
    
    public void GrenadeThrowFailed()
    {
        if (_currentGrenadesLabel == null)
            return;
        
        AudioManager.I.PlayUiClick();

        // Immediately flash red
        _currentGrenadesLabel.Modulate = Colors.Red;

        // Tween back to white
        var tween = CreateTween();
        tween.TweenProperty(_currentGrenadesLabel, "modulate", Colors.White, 0.4f)
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);
    }
}