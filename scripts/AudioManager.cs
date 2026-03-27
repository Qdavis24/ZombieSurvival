using Godot;

public partial class AudioManager : Node
{
	public static AudioManager I; // optional static access

	public override void _EnterTree() => I = this;

	private bool _footstepPlaying = false;
	private static bool _explosionCooldown = false;
	private const float ExplosionCooldownTime = 0.12f; // 120 ms

	private const string SfxBus = "SFX";
	private const string MusicBus = "Music";
	private const string ExplosionBus = "Explosion";

	private AudioStreamPlayer _musicPlayer;

	public void PlayFootstep(AudioStream stream, Vector2 pos)
	{
		// Skip if the previous step hasn't finished
		if (_footstepPlaying || stream == null)
			return;

		var p = new AudioStreamPlayer2D
		{
			Stream = stream,
			GlobalPosition = pos,
			VolumeDb = -6f,
			PitchScale = (float)GD.RandRange(0.9, 1.1),
			Bus = SfxBus
		};

		AddChild(p);
		_footstepPlaying = true;

		// Reset flag when finished
		p.Finished += () =>
		{
			_footstepPlaying = false;
			p.QueueFree();
		};

		p.Play();
	}

	public void Play2D(AudioStream stream, Vector2 pos, float volumeDb = -6f, float pitch = 1f)
	{
		if (stream == null)
		{
			GD.PrintErr("SFX stream is null");
			return;
		}

		var p = new AudioStreamPlayer2D
		{
			Stream = stream,
			GlobalPosition = pos,
			VolumeDb = volumeDb,
			PitchScale = pitch,
			Bus = SfxBus
		};

		AddChild(p);
		p.Finished += () => p.QueueFree();
		p.Play();
	}

	public void PlayFollowing(AudioStream stream, Node2D target, float volumeDb = -6f, float pitch = 1f)
	{
		if (stream == null || target == null) return;

		var p = new AudioStreamPlayer2D
		{
			Stream = stream,
			VolumeDb = volumeDb,
			PitchScale = pitch,
			Bus = SfxBus
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

	public void PlayFireballExplosion(AudioStream stream, Vector2 pos, float volumeDb = -6f)
	{
		if (stream == null) return;

		// Skip if still in cooldown
		if (_explosionCooldown)
			return;

		_explosionCooldown = true;

		var p = new AudioStreamPlayer
		{
			Stream = stream,
			VolumeDb = volumeDb,
			Bus = ExplosionBus
		};

		AddChild(p);

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

	public override void _Ready()
	{
		ProcessMode = Node.ProcessModeEnum.Always;
	}
}
