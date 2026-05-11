using Godot;

[GlobalClass]
public partial class AmmoVendingOption : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export] public PackedScene AmmoScene { get; set; }
    [Export] public int Price { get; set; }
    [Export] public int SpawnCount { get; set; } = 1;
}
