using Godot;

public enum CombatMusicSet
{
    None,
    City,
    Forest
}

[GlobalClass]
public partial class LevelDefinition : Resource
{
    [Export] public string DisplayName { get; set; } = "";
    [Export] public CombatMusicSet MusicSet { get; set; } = CombatMusicSet.None;
    [Export] public PackedScene GameScene { get; set; }
}
