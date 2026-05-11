using Godot;

[GlobalClass]
public partial class WeaponUpgradeOption : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export] public WeaponUpgradeTarget Target { get; set; }
    [Export] public int Price { get; set; } = 3000;
}
