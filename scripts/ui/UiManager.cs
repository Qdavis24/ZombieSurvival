using Godot;
using System;

public partial class UiManager : Node
{
	[Signal] public delegate void StartGameEventHandler();
	[Signal] public delegate void QuitGameEventHandler();
	
	[Export] private AudioStream _startMenuMusic;
	
	[ExportGroup("Nodes")]
	[Export] private StartMenu _startMenu;
	[Export] private PauseMenu _pauseMenu;
	[Export] private DeathMenu _deathMenu;
	[Export] private Hud _hud;
	[Export] private Popup _popup;
	
	public override void _Ready()
	{
		_startMenu.Visible = true;
		AudioManager.I.PlayMusic(_startMenuMusic);
		_startMenu.StartGamePressed += OnStartGame;
		
		_pauseMenu.QuitButtonPressed += OnQuitGame;
		_deathMenu.QuitButtonPressed += OnQuitGame;
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
		_hud.Visible = true;
		AudioManager.I.StopMusic();
		EmitSignal(SignalName.StartGame);
	}

	public void ShowPickup()
	{
		_popup.SetPickupText("Shotgun");
		_popup.Visible = true;
	}
	public void HidePickup()
	{
		_popup.Visible = false;
	}

	public async void ShowSuccessfulPickup()
	{
		_popup.Visible = true;
		await _popup.SuccessfullyPickedUp("Shotgun");
		_popup.Visible = false;
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
	
	public void HidePauseMenu()
	{
		_pauseMenu.Visible = false;
		AudioManager.I.PlayUiClick();
	}

	public void HudSetAmmo(int currentAmmo, int reserveAmmo)
	{
		_hud.SetAmmo(currentAmmo, reserveAmmo);
	}
	
	public void HudSetRound(int round)
	{
		_hud.SetRound(round);
	}
}
