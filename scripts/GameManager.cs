using Godot;
using System;
using ZombieSurvival.scripts.inventory_system;
using ZombieSurvival.scripts.spawn_system;
using ZombieSurvival.scripts.zombie_package;

namespace ZombieSurvival.scripts;

public partial class GameManager : Node
{
    [Export] private UiManager _uiManager;
    [Export] private Godot.Collections.Array<LevelDefinition> _levels = new();

    private bool _isPaused = false;
    private bool _isGamePlaying = false;
    private bool _isAmmoVendingMenuOpen = false;
    private bool _isPerkVendingMenuOpen = false;
    private bool _isWeaponUpgradeMenuOpen = false;

    private Game _gameInstance;
    private AmmoVendingMachine _activeAmmoVendingMachine;
    private Node3D _activeAmmoVendingPlayer;
    private PerkVendingMachine _activePerkVendingMachine;
    private Node3D _activePerkVendingPlayer;
    private WeaponUpgradeBench _activeWeaponUpgradeBench;
    private Node3D _activeWeaponUpgradePlayer;

    private bool _test;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        _uiManager.StartGame += OnStartGame;
        _uiManager.QuitGame += OnQuitGame;
        _uiManager.AmmoVendingOptionPressed += OnAmmoVendingOptionPressed;
        _uiManager.AmmoVendingClosed += OnAmmoVendingClosed;
        _uiManager.PerkVendingOptionPressed += OnPerkVendingOptionPressed;
        _uiManager.PerkVendingClosed += OnPerkVendingClosed;
        _uiManager.WeaponUpgradeOptionPressed += OnWeaponUpgradeOptionPressed;
        _uiManager.WeaponUpgradeClosed += OnWeaponUpgradeClosed;
        EventBus.Instance.AmmoVendingMenuRequested += OnAmmoVendingMenuRequested;
        EventBus.Instance.PerkVendingMenuRequested += OnPerkVendingMenuRequested;
        EventBus.Instance.WeaponUpgradeMenuRequested += OnWeaponUpgradeMenuRequested;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("pause") && _isGamePlaying)
        {
            if (_uiManager.IsOptionsMenuVisible())
            {
                _uiManager.CloseOptionsMenu();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (_uiManager.IsControlsMenuVisible())
            {
                _uiManager.CloseControlsMenu();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (_isAmmoVendingMenuOpen)
            {
                CloseAmmoVendingMenu();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (_isPerkVendingMenuOpen)
            {
                ClosePerkVendingMenu();
                GetViewport().SetInputAsHandled();
                return;
            }

            if (_isWeaponUpgradeMenuOpen)
            {
                CloseWeaponUpgradeMenu();
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

    private void OnStartGame(int levelIndex)
    {
        var selectedLevel = GetLevelDefinition(levelIndex);
        if (selectedLevel?.GameScene == null)
        {
            GD.PrintErr($"{nameof(GameManager)}: invalid level selection '{levelIndex}'.");
            return;
        }

        AudioManager.I.StartCombatMusic(selectedLevel.MusicSet);
        _isPaused = false;
        _isAmmoVendingMenuOpen = false;
        _isPerkVendingMenuOpen = false;
        _isWeaponUpgradeMenuOpen = false;
        _activeAmmoVendingMachine = null;
        _activeAmmoVendingPlayer = null;
        _activePerkVendingMachine = null;
        _activePerkVendingPlayer = null;
        _activeWeaponUpgradeBench = null;
        _activeWeaponUpgradePlayer = null;
        _gameInstance = selectedLevel.GameScene.Instantiate<Game>();
        AddChild(_gameInstance);
        _isGamePlaying = true;
        Input.MouseMode = Input.MouseModeEnum.Captured;
        _uiManager.HudSetRound(1);
    }

    private LevelDefinition GetLevelDefinition(int levelIndex)
    {
        if (_levels == null || _levels.Count == 0)
            return null;

        if (levelIndex < 0 || levelIndex >= _levels.Count)
            return null;

        return _levels[levelIndex];
    }

    private void OnQuitGame()
    {
        AudioManager.I.MuteCurrentSong();
        _isPaused = false;
        _isAmmoVendingMenuOpen = false;
        _isPerkVendingMenuOpen = false;
        _isWeaponUpgradeMenuOpen = false;
        _activeAmmoVendingMachine = null;
        _activeAmmoVendingPlayer = null;
        _activePerkVendingMachine = null;
        _activePerkVendingPlayer = null;
        _activeWeaponUpgradeBench = null;
        _activeWeaponUpgradePlayer = null;
        _uiManager.HideAmmoVendingMenu();
        _uiManager.HidePerkVendingMenu();
        _uiManager.HideWeaponUpgradeMenu();
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
        if (!_isGamePlaying || _isPaused || _isAmmoVendingMenuOpen || _isPerkVendingMenuOpen || _isWeaponUpgradeMenuOpen || machine == null || player == null)
            return;

        _activeAmmoVendingMachine = machine;
        _activeAmmoVendingPlayer = player;
        _isAmmoVendingMenuOpen = true;

        Input.MouseMode = Input.MouseModeEnum.Visible;
        _gameInstance.ProcessMode = ProcessModeEnum.Disabled;
        _uiManager.ShowAmmoVendingMenu(machine, player);
    }

    private void OnAmmoVendingOptionPressed(int optionIndex)
    {
        if (!_isAmmoVendingMenuOpen || _activeAmmoVendingMachine == null || _activeAmmoVendingPlayer == null)
            return;

        if (_activeAmmoVendingMachine.TryPurchase(optionIndex, _activeAmmoVendingPlayer))
            _uiManager.ShowAmmoVendingMenu(_activeAmmoVendingMachine, _activeAmmoVendingPlayer);
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

    private void OnPerkVendingMenuRequested(PerkVendingMachine machine, Node3D player)
    {
        if (!_isGamePlaying || _isPaused || _isAmmoVendingMenuOpen || _isPerkVendingMenuOpen || _isWeaponUpgradeMenuOpen || machine == null || player == null)
            return;

        _activePerkVendingMachine = machine;
        _activePerkVendingPlayer = player;
        _isPerkVendingMenuOpen = true;

        Input.MouseMode = Input.MouseModeEnum.Visible;
        _gameInstance.ProcessMode = ProcessModeEnum.Disabled;
        _uiManager.ShowPerkVendingMenu(machine, player);
    }

    private async void OnPerkVendingOptionPressed(int optionIndex)
    {
        if (!_isPerkVendingMenuOpen || _activePerkVendingMachine == null || _activePerkVendingPlayer == null)
            return;

        var machine = _activePerkVendingMachine;
        var player = _activePerkVendingPlayer;

        ClosePerkVendingMenu();
        await machine.TryPurchase(optionIndex, player);
    }

    private void OnPerkVendingClosed()
    {
        ClosePerkVendingMenu();
    }

    private void ClosePerkVendingMenu()
    {
        if (!_isPerkVendingMenuOpen)
            return;

        _isPerkVendingMenuOpen = false;
        _activePerkVendingMachine = null;
        _activePerkVendingPlayer = null;

        _uiManager.HidePerkVendingMenu();

        if (_isGamePlaying)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            _gameInstance.ProcessMode = ProcessModeEnum.Inherit;
        }
    }

    private void OnWeaponUpgradeMenuRequested(WeaponUpgradeBench bench, Node3D player)
    {
        if (!_isGamePlaying || _isPaused || _isAmmoVendingMenuOpen || _isPerkVendingMenuOpen || _isWeaponUpgradeMenuOpen || bench == null || player == null)
            return;

        _activeWeaponUpgradeBench = bench;
        _activeWeaponUpgradePlayer = player;
        _isWeaponUpgradeMenuOpen = true;

        Input.MouseMode = Input.MouseModeEnum.Visible;
        _gameInstance.ProcessMode = ProcessModeEnum.Disabled;
        _uiManager.ShowWeaponUpgradeMenu(bench, player);
    }

    private void OnWeaponUpgradeOptionPressed(int optionIndex)
    {
        if (!_isWeaponUpgradeMenuOpen || _activeWeaponUpgradeBench == null || _activeWeaponUpgradePlayer == null)
            return;

        if (_activeWeaponUpgradeBench.TryPurchase(optionIndex, _activeWeaponUpgradePlayer))
            _uiManager.ShowWeaponUpgradeMenu(_activeWeaponUpgradeBench, _activeWeaponUpgradePlayer);
    }

    private void OnWeaponUpgradeClosed()
    {
        CloseWeaponUpgradeMenu();
    }

    private void CloseWeaponUpgradeMenu()
    {
        if (!_isWeaponUpgradeMenuOpen)
            return;

        _isWeaponUpgradeMenuOpen = false;
        _activeWeaponUpgradeBench = null;
        _activeWeaponUpgradePlayer = null;

        _uiManager.HideWeaponUpgradeMenu();

        if (_isGamePlaying)
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            _gameInstance.ProcessMode = ProcessModeEnum.Inherit;
        }
    }
}
