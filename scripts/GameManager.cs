using Godot;
using System;
using ZombieSurvival.scripts.inventory_system;
using ZombieSurvival.scripts.spawn_system;
using ZombieSurvival.scripts.zombie_package;

namespace ZombieSurvival.scripts;

public partial class GameManager : Node
{
    [Export] private UiManager _uiManager;
    [Export] private PackedScene _game;
    [Export] private SpawnManager _spawnManager;

    [Export] private PlayerController _player;

    private bool _isPaused = false;
    private bool _isGamePlaying = false;

    private Game _gameInstance;

    private bool _test;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _uiManager.StartGame += OnStartGame;
        _uiManager.QuitGame += OnQuitGame;
    }

    private void HandlePlayerItemAdded(ItemType itemType, int amount)
    {
        _uiManager.ShowSuccessfulPickup($"{itemType.ToString()} + {amount}");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("pause") && _isGamePlaying)
        {
            _isPaused = !_isPaused;

            if (_isPaused)
            {
                Input.MouseMode = Input.MouseModeEnum.Visible;
                _uiManager.ShowPauseMenu();
                _gameInstance.ProcessMode = ProcessModeEnum.Disabled; // Pause game node time
            }
            else
            {
                Input.MouseMode = Input.MouseModeEnum.Captured;
                _uiManager.HidePauseMenu();
                _gameInstance.ProcessMode = ProcessModeEnum.Inherit; // Resume game node time
            }
        }

        // if (@event.IsActionPressed("interact") && _isGamePlaying)
        // {
        //     if (_test)
        //     {
        //         _uiManager.ShowSuccessfulPickup("d");
        //         _test = false;
        //     }
        //     else
        //     {
        //         _uiManager.ShowPickup("d");
        //         _test = true;
        //     }
        // }
    }

    public void SetRound(int round)
    {
        _uiManager.HudSetRound(round);
    }

    private void OnStartGame()
    {
        _isPaused = false;
        _gameInstance = _game.Instantiate<Game>();
        AddChild(_gameInstance);
        _player = _gameInstance.Player;
        _player.ItemAdded += HandlePlayerItemAdded;
        _isGamePlaying = true;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void OnQuitGame()
    {
        _player.ItemAdded -= HandlePlayerItemAdded;
        _isPaused = false;
        _gameInstance.QueueFree();
        _isGamePlaying = false;
    }
}