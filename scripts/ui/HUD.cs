using Godot;

public partial class HUD : CanvasLayer
{
    private Label _currentAmmoLabel;
    private Label _reserveAmmoLabel;

    public override void _Ready()
    {
        _currentAmmoLabel = GetNode<Label>("AmmoUI/VBoxContainer/Ammo/HBoxContainer/CurrentAmmoLabel");
        _reserveAmmoLabel = GetNode<Label>("AmmoUI/VBoxContainer/Ammo/HBoxContainer/ReserveAmmoLabel");
    }

    public void SetAmmo(int currentAmmo, int reserveAmmo)
    {
        if (_currentAmmoLabel == null || _reserveAmmoLabel == null)
        {
            GD.PushWarning("HUD.SetAmmo called before ammo labels were ready.");
            return;
        }

        _currentAmmoLabel.Text = currentAmmo.ToString();
        _reserveAmmoLabel.Text = reserveAmmo.ToString();
    }
}
