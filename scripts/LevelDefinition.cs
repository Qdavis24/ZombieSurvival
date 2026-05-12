using Godot;

[GlobalClass]
public partial class LevelDefinition : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export] public PackedScene GameScene { get; set; }
}
