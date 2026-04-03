using System;
using System.Collections.Generic;
using Godot;

public partial class PlayerHud : CanvasLayer
{
    [Export] private HealthIndicator _healthIndicator;
    [Export] private BloodSplatter _bloodSplatter;
    [Export] private Label _currentAmmoLabel;
    [Export] private Label _reserveAmmoLabel;
    [Export] private PlayerPopup _interactPopup;
    [Export] private PlayerPopup _itemPopup;

    private Queue<String> _itemNotifications = new();
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

    private void OnPopupFree()
    {
        _notificationsBusy = false;
        if (_itemNotifications.TryDequeue(out string msg))
        {
            ShowNotification(msg);
        }
    }

    private void OnPlayerEnteredInteractableRange(Node3D interactable)
    {
        if (interactable is IInteractable i)
            _interactPopup.ShowMessage(i.InteractPrompt);
    }

    private void OnPlayerExitedInteractableRange(Node3D interactable)
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

    public async void ShowNotification(string message)
    {
        if (_notificationsBusy)
        {
            _itemNotifications.Enqueue(message);
        }
        else
        {
            _notificationsBusy = true;
            await _itemPopup.ShowNotification(message);
        }
        
    }
}
