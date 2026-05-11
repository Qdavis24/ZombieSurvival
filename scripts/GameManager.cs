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
    
    [Export] private AudioStream _layer1;
    [Export] private AudioStream _layer2;
    [Export] private AudioStream _layer3;

    private bool _isPaused = false;
    private bool _isGamePlaying = false;
    private bool _isAmmoVendingMenuOpen = false;

    private Game _gameInstance;
    private AmmoVendingMachine _activeAmmoVendingMachine;
    private Node3D _activeAmmoVendingPlayer;

    private bool _test;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _uiManager.StartGame += OnStartGame;
        _uiManager.QuitGame += OnQuitGame;
        _uiManager.AmmoVendingOptionPressed += OnAmmoVendingOptionPressed;
        _uiManager.AmmoVendingClosed += OnAmmoVendingClosed;
        EventBus.Instance.AmmoVendingMenuRequested += OnAmmoVendingMenuRequested;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("pause") && _isGamePlaying)
        {
            if (_isAmmoVendingMenuOpen)
            {
                CloseAmmoVendingMenu();
                GetViewport().SetInputAsHandled();
                return;
            }

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
        AudioManager.I.RandomizeSong();
        AudioManager.I.StartCurrentSong();
        _isPaused = false;
        _isAmmoVendingMenuOpen = false;
        _activeAmmoVendingMachine = null;
        _activeAmmoVendingPlayer = null;
        _gameInstance = _game.Instantiate<Game>();
        AddChild(_gameInstance);
        _isGamePlaying = true;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _uiManager.HudSetRound(1);
    }

    private void OnQuitGame()
    {
        AudioManager.I.MuteCurrentSong();
        _isPaused = false;
        _isAmmoVendingMenuOpen = false;
        _activeAmmoVendingMachine = null;
        _activeAmmoVendingPlayer = null;
        _uiManager.HideAmmoVendingMenu();
        _gameInstance.QueueFree();
        _isGamePlaying = false;
    }
    
    public void PlayerDied()
    {
        _isGamePlaying = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _uiManager.PlayerDied();
        _gameInstance.CallDeferred(Node.MethodName.SetProcessMode, (int)ProcessModeEnum.Disabled);
    }

    private void OnAmmoVendingMenuRequested(AmmoVendingMachine machine, Node3D player)
    {
        if (!_isGamePlaying || _isPaused || _isAmmoVendingMenuOpen || machine == null || player == null)
            return;

        _activeAmmoVendingMachine = machine;
        _activeAmmoVendingPlayer = player;
        _isAmmoVendingMenuOpen = true;

        Input.MouseMode = Input.MouseModeEnum.Visible;
        _gameInstance.ProcessMode = ProcessModeEnum.Disabled;
        _uiManager.ShowAmmoVendingMenu(machine);
    }

    private void OnAmmoVendingOptionPressed(int optionIndex)
    {
        if (!_isAmmoVendingMenuOpen || _activeAmmoVendingMachine == null || _activeAmmoVendingPlayer == null)
            return;

        _activeAmmoVendingMachine.TryPurchase(optionIndex, _activeAmmoVendingPlayer);
    }

    private void OnAmmoVendingClosed()
    {
        CloseAmmoVendingMenu();
    }

    private void CloseAmmoVendingMenu()
    {
        if (!_isAmmoVendingMenuOpen)
            return;

        _isAmmoVendingMenuOpen = false;
        _activeAmmoVendingMachine = null;
        _activeAmmoVendingPlayer = null;

        _uiManager.HideAmmoVendingMenu();

        if (_isGamePlaying)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            _gameInstance.ProcessMode = ProcessModeEnum.Inherit;
        }
    }
}
