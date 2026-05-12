using Godot;
using System;

public partial class UiManager : Node
{
	[Signal] public delegate void StartGameEventHandler(int levelIndex);
	[Signal] public delegate void QuitGameEventHandler();
	[Signal] public delegate void AmmoVendingOptionPressedEventHandler(int optionIndex);
	[Signal] public delegate void AmmoVendingClosedEventHandler();
	[Signal] public delegate void PerkVendingOptionPressedEventHandler(int optionIndex);
	[Signal] public delegate void PerkVendingClosedEventHandler();
	[Signal] public delegate void WeaponUpgradeOptionPressedEventHandler(int optionIndex);
	[Signal] public delegate void WeaponUpgradeClosedEventHandler();
	
	[Export] private AudioStream _startMenuMusic;
	
	[ExportGroup("Nodes")]
	[Export] private StartMenu _startMenu;
	[Export] private PauseMenu _pauseMenu;
	[Export] private DeathMenu _deathMenu;
	[Export] private Hud _hud;
	[Export] private Popup _popup;
	[Export] private AmmoVendingMenu _ammoVendingMenu;
	[Export] private PerkVendingMenu _perkVendingMenu;
	[Export] private WeaponUpgradeMenu _weaponUpgradeMenu;
	[Export] private OptionsMenu _optionsMenu;
	[Export] private ControlsMenu _controlsMenu;

	private enum OptionsMenuSource
	{
		None,
		Start,
		Pause
	}

	private OptionsMenuSource _optionsMenuSource = OptionsMenuSource.None;
	
	
	public override void _Ready()
	{
		_startMenu.Visible = true;
		AudioManager.I.PlayMusic(_startMenuMusic);
		_startMenu.StartGamePressed += OnStartGame;
		_startMenu.OptionsPressed += OnStartMenuOptionsPressed;
		_startMenu.ControlsPressed += OnStartMenuControlsPressed;
		
		_pauseMenu.QuitButtonPressed += OnQuitGame;
		_pauseMenu.OptionsButtonPressed += OnPauseMenuOptionsPressed;
		_deathMenu.QuitButtonPressed += OnQuitGame;
		_ammoVendingMenu.OptionPressed += OnAmmoVendingOptionPressed;
		_ammoVendingMenu.Closed += OnAmmoVendingClosed;
		_perkVendingMenu.OptionPressed += OnPerkVendingOptionPressed;
		_perkVendingMenu.Closed += OnPerkVendingClosed;
		_weaponUpgradeMenu.OptionPressed += OnWeaponUpgradeOptionPressed;
		_weaponUpgradeMenu.Closed += OnWeaponUpgradeClosed;
		_optionsMenu.Closed += OnOptionsMenuClosed;
	}
	
	public void OnQuitGame()
	{
		_popup.Visible = false;
		_hud.Visible = false;
		_pauseMenu.Visible = false;
		_deathMenu.Visible = false;
		_optionsMenu.Visible = false;
		_controlsMenu.Visible = false;
		_weaponUpgradeMenu.Visible = false;
		_optionsMenuSource = OptionsMenuSource.None;
		_startMenu.Visible = true;
		AudioManager.I.PlayMusic(_startMenuMusic);
		EmitSignal(SignalName.QuitGame);
	}

	public void OnStartGame(int levelIndex)
	{
		_startMenu.Visible = false;
		_pauseMenu.Visible = false;
		_optionsMenu.Visible = false;
		_controlsMenu.Visible = false;
		_weaponUpgradeMenu.Visible = false;
		_optionsMenuSource = OptionsMenuSource.None;
		_hud.Init();
		_hud.Visible = true;
		AudioManager.I.StopMusic();
		EmitSignal(SignalName.StartGame, levelIndex);
	}

	public void ShowPauseMenu()
	{
		_pauseMenu.Visible = true;
		AudioManager.I.PlayUiClick();
		_pauseMenu.SetCurrentSong();
	}

	public void ShowDeathMenu()
	{
		_deathMenu.Visible = true;
	}

	public void ShowAmmoVendingMenu(AmmoVendingMachine machine)
	{
		_ammoVendingMenu.ShowOptions(machine.Options);
	}

	public void HideAmmoVendingMenu()
	{
		_ammoVendingMenu.Visible = false;
	}

	public void ShowPerkVendingMenu(PerkVendingMachine machine, Node3D player)
	{
		_perkVendingMenu.ShowOptions(machine, player);
	}

	public void HidePerkVendingMenu()
	{
		_perkVendingMenu.Visible = false;
	}

	public void ShowWeaponUpgradeMenu(WeaponUpgradeBench bench, Node3D player)
	{
		_weaponUpgradeMenu.ShowOptions(bench, player);
	}

	public void HideWeaponUpgradeMenu()
	{
		_weaponUpgradeMenu.Visible = false;
	}

	public bool IsOptionsMenuVisible()
	{
		return _optionsMenu.Visible;
	}

	public void CloseOptionsMenu()
	{
		_optionsMenu.Close();
	}

	public bool IsControlsMenuVisible()
	{
		return _controlsMenu.Visible;
	}

	public void CloseControlsMenu()
	{
		_controlsMenu.Close();
	}

	private void OnAmmoVendingOptionPressed(int optionIndex)
	{
		EmitSignal(SignalName.AmmoVendingOptionPressed, optionIndex);
	}

	private void OnAmmoVendingClosed()
	{
		EmitSignal(SignalName.AmmoVendingClosed);
	}

	private void OnPerkVendingOptionPressed(int optionIndex)
	{
		EmitSignal(SignalName.PerkVendingOptionPressed, optionIndex);
	}

	private void OnPerkVendingClosed()
	{
		EmitSignal(SignalName.PerkVendingClosed);
	}

	private void OnWeaponUpgradeOptionPressed(int optionIndex)
	{
		EmitSignal(SignalName.WeaponUpgradeOptionPressed, optionIndex);
	}

	private void OnWeaponUpgradeClosed()
	{
		EmitSignal(SignalName.WeaponUpgradeClosed);
	}

	private void OnStartMenuOptionsPressed()
	{
		_optionsMenuSource = OptionsMenuSource.Start;
		_optionsMenu.Open();
	}

	private void OnStartMenuControlsPressed()
	{
		_controlsMenu.Open();
	}

	private void OnPauseMenuOptionsPressed()
	{
		_optionsMenuSource = OptionsMenuSource.Pause;
		_optionsMenu.Open();
	}

	private void OnOptionsMenuClosed()
	{
		_optionsMenuSource = OptionsMenuSource.None;
	}
	
	public void HidePauseMenu()
	{
		_pauseMenu.Visible = false;
		AudioManager.I.PlayUiClick();
	}
	
	public void HudSetRound(int round)
	{
		_hud.SetRound(round);
	}

	public void PlayerDied()
	{
		_pauseMenu.Visible = false;
		_deathMenu.Visible = true;
	}
}
