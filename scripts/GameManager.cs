using Godot;
using System;
using ZombieSurvival.scripts.spawn_system;
using ZombieSurvival.scripts.zombie_package;

namespace ZombieSurvival.scripts;

public partial class GameManager : Node
{
    [Export] private SpawnManager _spawnManager;

    [Export] private Node3D _player;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

}