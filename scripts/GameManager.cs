using Godot;
using System;
using ZombieSurvival.scripts.spawn_system;
using ZombieSurvival.scripts.zombie_package;

namespace ZombieSurvival.scripts;

public partial class GameManager : Node
{
    [Export] private UiManager _uiManager;
    [Export] private PackedScene _game;
    [Export] private SpawnManager _spawnManager;

    [Export] private Node3D _player;
    
    [Export] private AudioStream _layer1;
    [Export] private AudioStream _layer2;
    [Export] private AudioStream _layer3;

    private bool _isPaused = false;
    private bool _isGamePlaying = false;

    private Node _gameInstance;

    private bool _test;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _uiManager.StartGame += OnStartGame;
        _uiManager.QuitGame += OnQuitGame;
        
        AudioManager.I.InitLayer1(_layer1);
        AudioManager.I.InitLayer2(_layer2);
        AudioManager.I.InitLayer3(_layer3);
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

        if (@event.IsActionPressed("interact") && _isGamePlaying)
        {
            if (_test)
            {
                _uiManager.ShowSuccessfulPickup();
                _test = false;
            }
            else
            {
                _uiManager.ShowPickup();
                _test = true;
            }
        }
    }

    public void SetRound(int round)
    {
        _uiManager.HudSetRound(round);
    }

    private void OnStartGame()
    {
        _isPaused = false;
        _gameInstance = _game.Instantiate<Node>();
        AddChild(_gameInstance);
        _isGamePlaying = true;
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    private void OnQuitGame()
    {
        _isPaused = false;
        _gameInstance.QueueFree();
        _isGamePlaying = false;
    }
}