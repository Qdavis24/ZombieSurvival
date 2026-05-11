using Godot;
using System;

public partial class UiManager : Node
{
	[Signal] public delegate void StartGameEventHandler();
	[Signal] public delegate void QuitGameEventHandler();
	[Signal] public delegate void AmmoVendingOptionPressedEventHandler(int optionIndex);
	[Signal] public delegate void AmmoVendingClosedEventHandler();
	[Signal] public delegate void PerkVendingOptionPressedEventHandler(int optionIndex);
	[Signal] public delegate void PerkVendingClosedEventHandler();
	
	[Export] private AudioStream _startMenuMusic;
	
	[ExportGroup("Nodes")]
	[Export] private StartMenu _startMenu;
	[Export] private PauseMenu _pauseMenu;
	[Export] private DeathMenu _deathMenu;
	[Export] private Hud _hud;
	[Export] private Popup _popup;
	[Export] private AmmoVendingMenu _ammoVendingMenu;
	[Export] private PerkVendingMenu _perkVendingMenu;
	[Export] private OptionsMenu _optionsMenu;

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
		
		_pauseMenu.QuitButtonPressed += OnQuitGame;
		_pauseMenu.OptionsButtonPressed += OnPauseMenuOptionsPressed;
		_deathMenu.QuitButtonPressed += OnQuitGame;
		_ammoVendingMenu.OptionPressed += OnAmmoVendingOptionPressed;
		_ammoVendingMenu.Closed += OnAmmoVendingClosed;
		_perkVendingMenu.OptionPressed += OnPerkVendingOptionPressed;
		_perkVendingMenu.Closed += OnPerkVendingClosed;
		_optionsMenu.Closed += OnOptionsMenuClosed;
	}
	
	public void OnQuitGame()
	{
		_popup.Visible = false;
		_hud.Visible = false;
		_pauseMenu.Visible = false;
		_deathMenu.Visible = false;
		_optionsMenu.Visible = false;
		_optionsMenuSource = OptionsMenuSource.None;
		_startMenu.Visible = true;
		AudioManager.I.PlayMusic(_startMenuMusic);
		EmitSignal(SignalName.QuitGame);
	}

	public void OnStartGame()
	{
		_startMenu.Visible = false;
		_pauseMenu.Visible = false;
		_optionsMenu.Visible = false;
		_optionsMenuSource = OptionsMenuSource.None;
		_hud.Init();
		_hud.Visible = true;
		AudioManager.I.StopMusic();
		EmitSignal(SignalName.StartGame);
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

	public bool IsOptionsMenuVisible()
	{
		return _optionsMenu.Visible;
	}

	public void CloseOptionsMenu()
	{
		_optionsMenu.Close();
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

	private void OnStartMenuOptionsPressed()
	{
		_optionsMenuSource = OptionsMenuSource.Start;
		_optionsMenu.Open();
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
