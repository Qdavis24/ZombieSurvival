using Godot;

[GlobalClass]
public partial class PerkVendingOption : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export] public PerkManager.PerkType PerkType { get; set; }
    [Export] public int Price { get; set; } = 2500;
}
