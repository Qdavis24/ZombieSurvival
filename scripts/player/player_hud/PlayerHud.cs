using System;
using Godot;

public partial class PlayerHud : CanvasLayer
{
    [Export] private HealthIndicator _healthIndicator;
    [Export] private BloodSplatter _bloodSplatter;
    [Export] private Label _currentAmmoLabel;
    [Export] private Label _reserveAmmoLabel;
    [Export] private Popup _playerPopup;

    public void UpdateHealthIndicator(float currHealth, float maxHealth)
    {
        _healthIndicator.SetIntensity(1-currHealth/maxHealth);
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
    
    public void ShowPickup(String pickupText)
    {
        _playerPopup.SetPickupText(pickupText);
        _playerPopup.Visible = true;
    }
    public void HidePickup()
    {
        _playerPopup.Visible = false;
    }

    public async void ShowSuccessfulPickup(String pickupText)
    {
        _playerPopup.Visible = true;
        await _playerPopup.SuccessfullyPickedUp(pickupText);
        _playerPopup.Visible = false;
    }
}