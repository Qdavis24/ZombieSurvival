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
	
	
	public override void _Ready()
	{
		_startMenu.Visible = true;
		AudioManager.I.PlayMusic(_startMenuMusic);
		_startMenu.StartGamePressed += OnStartGame;
		
		_pauseMenu.QuitButtonPressed += OnQuitGame;
		_deathMenu.QuitButtonPressed += OnQuitGame;
		_ammoVendingMenu.OptionPressed += OnAmmoVendingOptionPressed;
		_ammoVendingMenu.Closed += OnAmmoVendingClosed;
		_perkVendingMenu.OptionPressed += OnPerkVendingOptionPressed;
		_perkVendingMenu.Closed += OnPerkVendingClosed;
	}
	
	public void OnQuitGame()
	{
		_popup.Visible = false;
		_hud.Visible = false;
		_pauseMenu.Visible = false;
		_deathMenu.Visible = false;
		_startMenu.Visible = true;
		AudioManager.I.PlayMusic(_startMenuMusic);
		EmitSignal(SignalName.QuitGame);
	}

	public void OnStartGame()
	{
		_startMenu.Visible = false;
		_pauseMenu.Visible = false;
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
