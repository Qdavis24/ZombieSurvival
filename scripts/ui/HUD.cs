using Godot;

public partial class Hud : CanvasLayer
{
    [ExportGroup("Nodes")]
    [Export] private Label _currentAmmoLabel;
    [Export] private Label _reserveAmmoLabel;

    public void SetAmmo(int currentAmmo, int reserveAmmo)
    {
        _currentAmmoLabel.Text = currentAmmo.ToString();
        _reserveAmmoLabel.Text = reserveAmmo.ToString();
    }
}
