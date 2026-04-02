using Godot;

public partial class AudioManager : Node
{
	public static AudioManager I; // optional static access

	public override void _EnterTree() => I = this;

	private bool _footstepPlaying = false;
	private float _footstepCooldownTime = 0.5f;
	private static bool _explosionCooldown = false;
	private const float ExplosionCooldownTime = 0.12f; // 120 ms

	private const string SfxBus = "SFX";
	private const string MusicBus = "Music";
	private const string ExplosionBus = "Explosion";
	
	private int _maxZombieHitSounds = 4;
	private int _currentZombieHitSounds = 0;
	
	private float _tween = 3.0f; // fades for layers
	
	private AudioStream _uiClickStream = GD.Load<AudioStream>("res://assets/sound/ui_click.wav");

	private AudioStreamPlayer _musicPlayer;
	
	private AudioStreamPlayer _layer1;
	private AudioStreamPlayer _layer2;
	private AudioStreamPlayer _layer3;

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

		GD.Print("Played");
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
	
	
	public AudioStreamPlayer? PlayMusicLayer(AudioStream stream, float volumeDb = -6f)
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
		if (_layer1 == null) return;

		var tween = CreateTween();
		tween.TweenProperty(_layer1, "volume_db", -6f, _tween);
		tween.TweenProperty(_layer2, "volume_db", -80f, _tween);
		tween.TweenProperty(_layer3, "volume_db", -80f, _tween);
	}
	public void PlayLayer2()
	{
		if (_layer2 == null) return;

		var tween = CreateTween();
		tween.TweenProperty(_layer1, "volume_db", -6f, _tween);
		tween.TweenProperty(_layer2, "volume_db", -6f, _tween);
		tween.TweenProperty(_layer3, "volume_db", -80f, _tween);
	}
	public void PlayLayer3()
	{
		if (_layer3 == null) return;

		var tween = CreateTween();
		tween.TweenProperty(_layer1, "volume_db", -6f, _tween);
		tween.TweenProperty(_layer2, "volume_db", -6f, _tween);
		tween.TweenProperty(_layer3, "volume_db", -6f, _tween);
	}

	public override void _Ready()
	{
		ProcessMode = Node.ProcessModeEnum.Always;
	}
}
