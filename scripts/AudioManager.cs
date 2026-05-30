using Godot;

public partial class AudioManager : Node
{
	public static AudioManager I; // optional static access

	public override void _EnterTree() => I = this;

	private sealed class MusicSong
	{
		public AudioStream Layer1;
		public AudioStream Layer2;
		public AudioStream Layer3;
		public float VolumeOffsetDb;

		public AudioStream[] Layers => [Layer1, Layer2, Layer3];
	}

	private bool _footstepPlaying = false;
	private float _footstepCooldownTime = 0.5f;
	private static bool _explosionCooldown = false;
	private const float ExplosionCooldownTime = 0.12f; // 120 ms

	private const string SfxBus = "SFX";
	private const string MusicBus = "Music";
	private const string ExplosionBus = "Explosion";

	private int _maxZombieHitSounds = 1;
	private int _currentZombieHitSounds = 0;

	private float _tween = 0.5f; // fades for layers

	private AudioStream _uiClickStream = GD.Load<AudioStream>("res://assets/sound/ui_click.wav");

	private MusicSong[] _citySongs;
	private MusicSong[] _forestSongs;
	private MusicSong[] _bunkerSongs;
	private MusicSong[] _songs = [];
	private int  _currentSong = -1;
	private float  _musicVol = -8f;
	private float _currentSongVolumeDb = -8f;
	[Export] private float _combatSongAutoRotateSeconds = 300f;
	[Export] private float _combatSongFadeSeconds = 2f;
	[Export] private float _combatSongFadeOutSeconds = 3f;
	private double _combatSongStartedAtSeconds = -1.0;

	private AudioStreamPlayer _musicPlayer;

	private AudioStreamPlayer _layer1;
	private AudioStreamPlayer _layer2;
	private AudioStreamPlayer _layer3;
	private Tween _musicLayerTween;
	private int _currentMusicLayer;

	private static MusicSong LoadSong(string layer1Path, string layer2Path, string layer3Path, float volumeOffsetDb = 0f)
	{
		return new MusicSong
		{
			Layer1 = GD.Load<AudioStream>(layer1Path),
			Layer2 = GD.Load<AudioStream>(layer2Path),
			Layer3 = GD.Load<AudioStream>(layer3Path),
			VolumeOffsetDb = volumeOffsetDb
		};
	}

	private static MusicSong LoadCitySong(string folderName, float volumeOffsetDb = 0f)
	{
		return LoadSong(
			$"res://assets/sound/music/{folderName}/layer1.ogg",
			$"res://assets/sound/music/{folderName}/layer2.ogg",
			$"res://assets/sound/music/{folderName}/layer3.ogg",
			volumeOffsetDb);
	}

	private static MusicSong LoadForestSong(string songName, float volumeOffsetDb = 0f)
	{
		return LoadSong(
			$"res://assets/sound/music/forest_beats/{songName}Layer1.ogg",
			$"res://assets/sound/music/forest_beats/{songName}Layer2.ogg",
			$"res://assets/sound/music/forest_beats/{songName}Layer3.ogg",
			volumeOffsetDb);
	}

	private static MusicSong LoadForestSongWithFirstTwoLayersSwapped(string songName, float volumeOffsetDb = 0f)
	{
		return LoadSong(
			$"res://assets/sound/music/forest_beats/{songName}Layer2.ogg",
			$"res://assets/sound/music/forest_beats/{songName}Layer1.ogg",
			$"res://assets/sound/music/forest_beats/{songName}Layer3.ogg",
			volumeOffsetDb);
	}

	private static MusicSong LoadBunkerSong(string songName, float volumeOffsetDb = 0f)
	{
		return LoadSong(
			$"res://assets/sound/music/bunker_beats/{songName}Layer1.ogg",
			$"res://assets/sound/music/bunker_beats/{songName}Layer2.ogg",
			$"res://assets/sound/music/bunker_beats/{songName}Layer3.ogg",
			volumeOffsetDb);
	}

	public bool HasCombatSongs()
	{
		return _songs != null && _songs.Length > 0;
	}

	public void NextSong()
	{
		if (!HasCombatSongs())
			return;

		_currentSong = (_currentSong + 1) % _songs.Length;
		StartCurrentSong();
	}

	public void PrevSong()
	{
		if (!HasCombatSongs())
			return;

		_currentSong--;
		if (_currentSong < 0)
			_currentSong = _songs.Length - 1;

		StartCurrentSong();
	}

	public int GetCurrentSong()
	{
		if (!HasCombatSongs())
			return -1;

		return _currentSong;
	}

	public void MuteCurrentSong()
	{
		if (_layer1 == null && _layer2 == null && _layer3 == null)
			return;

		var tween = CreateTween();
		tween.SetParallel(true);
		if (_layer1 != null)
			tween.TweenProperty(_layer1, "volume_db", -80f, 1f);
		if (_layer2 != null)
			tween.TweenProperty(_layer2, "volume_db", -80f, 1f);
		if (_layer3 != null)
			tween.TweenProperty(_layer3, "volume_db", -80f, 1f);
	}

	public void RandomizeSong()
	{
		if (!HasCombatSongs())
		{
			_currentSong = -1;
			return;
		}

		GD.Randomize();
		_currentSong = GD.RandRange(0, _songs.Length-1);
	}

	public async void StartCombatMusic(CombatMusicSet musicSet)
	{
		_songs = GetSongsForSet(musicSet);

		if (!HasCombatSongs())
		{
			_currentSong = -1;
			_combatSongStartedAtSeconds = -1.0;
			await FadeOutCurrentCombatSong();
			return;
		}

		RandomizeSong();
		StartCurrentSong();
	}

	public async void StartCurrentSong()
	{
		if (!HasCombatSongs())
			return;

		if (_currentSong < 0 || _currentSong >= _songs.Length)
			_currentSong = 0;

		await FadeOutCurrentCombatSong();

		var song = _songs[_currentSong];
		if (song == null)
			return;

		_currentSongVolumeDb = _musicVol + song.VolumeOffsetDb;
		InitLayer1(song.Layer1);
		InitLayer2(song.Layer2);
		InitLayer3(song.Layer3);

		_currentMusicLayer = 0;
		SetMusicLayer(1, _combatSongFadeSeconds);
		_combatSongStartedAtSeconds = Time.GetTicksMsec() / 1000.0;
	}

	public void RotateCombatSongIfReady()
	{
		if (!IsCombatSongReadyToRotate())
			return;

		SelectRandomDifferentSong();
		StartCurrentSong();
	}

	private bool IsCombatSongReadyToRotate()
	{
		if (!HasCombatSongs() || _songs.Length <= 1 || _combatSongStartedAtSeconds < 0.0)
			return false;

		var elapsedSeconds = Time.GetTicksMsec() / 1000.0 - _combatSongStartedAtSeconds;
		return elapsedSeconds >= _combatSongAutoRotateSeconds;
	}

	private void SelectRandomDifferentSong()
	{
		if (!HasCombatSongs() || _songs.Length <= 1)
			return;

		var nextSong = GD.RandRange(0, _songs.Length - 2);
		if (nextSong >= _currentSong)
			nextSong++;

		_currentSong = nextSong;
	}

	private MusicSong[] GetSongsForSet(CombatMusicSet musicSet)
	{
		return musicSet switch
		{
			CombatMusicSet.City => _citySongs,
			CombatMusicSet.Forest => _forestSongs,
			CombatMusicSet.Bunker => _bunkerSongs,
			_ => []
		};
	}

	private async System.Threading.Tasks.Task FadeOutCurrentCombatSong()
	{
		if (_layer1 == null && _layer2 == null && _layer3 == null)
			return;

		if (_musicLayerTween != null && GodotObject.IsInstanceValid(_musicLayerTween))
			_musicLayerTween.Kill();

		if (_combatSongFadeOutSeconds > 0f)
		{
			var fadeTween = CreateTween();
			fadeTween.SetParallel(true);

			if (_layer1 != null)
				fadeTween.TweenProperty(_layer1, "volume_db", -80f, _combatSongFadeOutSeconds);
			if (_layer2 != null)
				fadeTween.TweenProperty(_layer2, "volume_db", -80f, _combatSongFadeOutSeconds);
			if (_layer3 != null)
				fadeTween.TweenProperty(_layer3, "volume_db", -80f, _combatSongFadeOutSeconds);

			await ToSignal(fadeTween, Tween.SignalName.Finished);
		}

		StopMusicLayer(ref _layer1);
		StopMusicLayer(ref _layer2);
		StopMusicLayer(ref _layer3);
		_currentMusicLayer = 0;
	}

	private void StopMusicLayer(ref AudioStreamPlayer layer)
	{
		if (layer == null)
			return;

		layer.Stop();
		layer.QueueFree();
		layer = null;
	}

	public void PlayFootstep(AudioStream stream, Vector3 pos)
	{
		// Skip if still in cooldown or missing sound
		if (_footstepPlaying || stream == null)
			return;

		var p = new AudioStreamPlayer3D
		{
			Stream = stream,
			VolumeDb = -23f,
			PitchScale = (float)GD.RandRange(0.9, 1.1),
			Bus = SfxBus
		};

		AddChild(p);
		p.GlobalPosition = pos;
		_footstepPlaying = true;
		p.Play();
		p.Finished += () => p.QueueFree();

		var timer = GetTree().CreateTimer(_footstepCooldownTime);
		timer.Timeout += () =>
		{
			_footstepPlaying = false;
		};
	}

	public void PlayZombieHit(AudioStream stream, Vector3 pos, float volumeDb = -6f)
	{
		if (stream == null) return;

		// Limit how many zombie hit sounds can play at once
		if (_currentZombieHitSounds >= _maxZombieHitSounds)
			return;

		_currentZombieHitSounds++;

		var p = new AudioStreamPlayer3D
		{
			Stream = stream,
			VolumeDb = volumeDb,
			PitchScale = (float)GD.RandRange(0.9f, 1.1f),
			Bus = SfxBus
		};

		AddChild(p);
		p.GlobalPosition = pos;

		p.Finished += () =>
		{
			_currentZombieHitSounds--;
			p.QueueFree();
		};

		p.Play();
	}

	public void Play3D(AudioStream stream, Vector3 pos, float volumeDb = -6f, float pitch = 1f)
	{
		if (stream == null)
		{
			GD.PrintErr("SFX stream is null");
			return;
		}

		var p = new AudioStreamPlayer3D
		{
			Stream = stream,
			VolumeDb = volumeDb,
			PitchScale = pitch,
			Bus = SfxBus
		};

		AddChild(p);
		p.GlobalPosition = pos;
		p.Finished += () => p.QueueFree();
		p.Play();
	}

	public void PlaySfx(AudioStream stream, float volumeDb = -6f, float pitch = 1f)
	{
		if (stream == null) return;

		var p = new AudioStreamPlayer
		{
			Stream = stream,
			VolumeDb = volumeDb,
			PitchScale = pitch,
			Bus = SfxBus
		};

		AddChild(p);
		p.Finished += () => p.QueueFree();
		p.Play();
	}

	public void PlayFollowing(AudioStream stream, Node3D target, float volumeDb = -6f, float pitch = 1f)
	{
		if (stream == null || target == null) return;

		var p = new AudioStreamPlayer3D
		{
			Stream = stream,
			VolumeDb = volumeDb,
			PitchScale = pitch,
			Bus = SfxBus,
			Position = Vector3.Zero
		};

		// Parent to the target so it follows automatically
		target.AddChild(p);
		p.Finished += () => p.QueueFree();
		p.Play();
	}

	public void PlayUi(AudioStream stream, float volumeDb = -6f, float pitch = 1f)
	{
		if (stream == null) return;

		var p = new AudioStreamPlayer
		{
			Stream = stream,
			VolumeDb = volumeDb,
			PitchScale = pitch,
			Bus = SfxBus
		};

		AddChild(p);
		p.Finished += () => p.QueueFree();
		p.Play();
	}

	public void PlayUiClick(float volumeDb = -10f, float pitch = 1f)
	{
		if (_uiClickStream == null) return;

		var p = new AudioStreamPlayer
		{
			Stream = _uiClickStream,
			VolumeDb = volumeDb,
			PitchScale = (float)GD.RandRange(0.5f, 1.5f),
			Bus = SfxBus
		};

		AddChild(p);
		p.Finished += () => p.QueueFree();
		p.Play();
	}

	public void PlayExplosion(AudioStream stream, Vector3 pos, float volumeDb = -6f)
	{
		if (stream == null) return;

		// Skip if still in cooldown
		if (_explosionCooldown)
			return;

		_explosionCooldown = true;

		var p = new AudioStreamPlayer3D
		{
			Stream = stream,
			VolumeDb = volumeDb,
			Bus = ExplosionBus
		};

		AddChild(p);
		p.GlobalPosition = pos;

		// Let the audio free itself *after it finishes naturally*
		p.Finished += () => p.QueueFree();
		p.Play();

		// Create the cooldown timer (one-shot)
		var timer = GetTree().CreateTimer(ExplosionCooldownTime);
		timer.Timeout += () =>
		{
			_explosionCooldown = false;
		};
	}

	public void PlayMusic(AudioStream stream, float volumeDb = -6f)
	{
		if (stream == null) return;

		// Stop existing music immediately before starting the new track
		if (_musicPlayer != null)
		{
			StopMusicImmediate();
		}

		// Duplicate so enabling loop here does not modify the original resource everywhere else.
		var musicStream = stream.Duplicate() as AudioStream;
		if (musicStream == null) return;

		switch (musicStream)
		{
			case AudioStreamOggVorbis ogg:
				ogg.Loop = true;
				break;
			case AudioStreamMP3 mp3:
				mp3.Loop = true;
				break;
			case AudioStreamWav wav:
				wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
				break;
		}

		_musicPlayer = new AudioStreamPlayer
		{
			Stream = musicStream,
			VolumeDb = volumeDb,
			Bus = MusicBus
		};

		AddChild(_musicPlayer);
		_musicPlayer.Play();
	}

	public async void StopMusic(float fadeDuration = 4.0f)
	{
		if (_musicPlayer == null) return;

		// Keep a local reference in case _musicPlayer changes while fading.
		var player = _musicPlayer;

		if (fadeDuration <= 0f)
		{
			StopMusicImmediate();
			return;
		}

		var tween = CreateTween();
		tween.TweenProperty(player, "volume_db", -80f, fadeDuration);
		await ToSignal(tween, Tween.SignalName.Finished);

		// Only clear the field if this is still the active music player.
		if (player == _musicPlayer)
		{
			player.Stop();
			player.QueueFree();
			_musicPlayer = null;
		}
		else if (GodotObject.IsInstanceValid(player))
		{
			player.Stop();
			player.QueueFree();
		}
	}

	private void StopMusicImmediate()
	{
		if (_musicPlayer == null) return;

		_musicPlayer.Stop();
		_musicPlayer.QueueFree();
		_musicPlayer = null;
	}


	public AudioStreamPlayer PlayMusicLayer(AudioStream stream, float volumeDb = -6f)
	{
		if (stream == null) return null;

		// Duplicate so enabling loop here does not modify the original resource everywhere else.
		var musicStream = stream.Duplicate() as AudioStream;
		if (musicStream == null) return null;

		switch (musicStream)
		{
			case AudioStreamOggVorbis ogg:
				ogg.Loop = true;
				break;
			case AudioStreamMP3 mp3:
				mp3.Loop = true;
				break;
			case AudioStreamWav wav:
				wav.LoopMode = AudioStreamWav.LoopModeEnum.Forward;
				break;
		}

		var mmusicLayerPlayer = new AudioStreamPlayer
		{
			Stream = musicStream,
			VolumeDb = volumeDb,
			Bus = MusicBus
		};

		AddChild(mmusicLayerPlayer);
		mmusicLayerPlayer.Play();

		return mmusicLayerPlayer;
	}

	public void InitLayer1(AudioStream stream, float volumeDb = -6f)
	{
		_layer1 = PlayMusicLayer(stream, -80f);
	}
	public void InitLayer2(AudioStream stream, float volumeDb = -6f)
	{
		_layer2 = PlayMusicLayer(stream, -80f);
	}
	public void InitLayer3(AudioStream stream, float volumeDb = -6f)
	{
		_layer3 = PlayMusicLayer(stream, -80f);
	}

	public void PlayLayer1()
	{
		SetMusicLayer(1);
	}
	public void PlayLayer2()
	{
		SetMusicLayer(2);
	}
	public void PlayLayer3()
	{
		SetMusicLayer(3);
	}

	public void SetMusicLayer(int layer)
	{
		SetMusicLayer(layer, _tween);
	}

	private void SetMusicLayer(int layer, float fadeSeconds)
	{
		if (!HasCombatSongs() || _layer1 == null || _layer2 == null || _layer3 == null)
			return;

		layer = Mathf.Clamp(layer, 1, 3);
		if (layer == _currentMusicLayer)
			return;

		if (_musicLayerTween != null && GodotObject.IsInstanceValid(_musicLayerTween))
			_musicLayerTween.Kill();

		_musicLayerTween = CreateTween();
		_musicLayerTween.SetParallel(true);
		_musicLayerTween.TweenProperty(_layer1, "volume_db", _currentSongVolumeDb, fadeSeconds);
		_musicLayerTween.TweenProperty(_layer2, "volume_db", layer >= 2 ? _currentSongVolumeDb : -80f, fadeSeconds);
		_musicLayerTween.TweenProperty(_layer3, "volume_db", layer >= 3 ? _currentSongVolumeDb : -80f, fadeSeconds);
		_currentMusicLayer = layer;
	}

	public override void _Ready()
	{
		_citySongs =
		[
			LoadCitySong("beat1"),
			LoadCitySong("beat2"),
			LoadCitySong("beat3"),
			LoadCitySong("beat4"),
			LoadCitySong("beat5")
		];

		_forestSongs =
		[
			LoadForestSong("Lost", -5f),
			LoadForestSongWithFirstTwoLayersSwapped("Lurking", -5f),
			LoadForestSong("Plague", -3f),
			LoadForestSong("Surounded", -3f),
			LoadForestSong("Voodoo", -5f)
		];

		_bunkerSongs =
		[
			LoadBunkerSong("Barricade", -4f),
			LoadBunkerSong("Military", -4f),
			LoadBunkerSong("Outbreak"),
			LoadBunkerSong("Scrape", -2f),
			LoadBunkerSong("Undying", -4f)
		];

		_songs = [];
	}
}
