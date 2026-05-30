using Godot;

public partial class MusicNormalizationTest : Control
{
	private static readonly CombatMusicSet[] MusicSets =
	[
		CombatMusicSet.City,
		CombatMusicSet.Forest,
		CombatMusicSet.Bunker
	];

	private Label _mapLabel;
	private Label _songLabel;
	private Label _layerLabel;
	private Label _volumeLabel;
	private int _musicSetIndex;
	private int _layer = 1;

	public override void _Ready()
	{
		_mapLabel = GetNode<Label>("%MapValue");
		_songLabel = GetNode<Label>("%SongValue");
		_layerLabel = GetNode<Label>("%LayerValue");
		_volumeLabel = GetNode<Label>("%VolumeValue");

		GetNode<Button>("%PreviousMap").Pressed += PreviousMap;
		GetNode<Button>("%NextMap").Pressed += NextMap;
		GetNode<Button>("%PreviousSong").Pressed += PreviousSong;
		GetNode<Button>("%NextSong").Pressed += NextSong;
		GetNode<Button>("%Layer1").Pressed += () => SetLayer(1);
		GetNode<Button>("%Layer2").Pressed += () => SetLayer(2);
		GetNode<Button>("%Layer3").Pressed += () => SetLayer(3);
		GetNode<Button>("%Quit").Pressed += () => GetTree().Quit();

		AudioManager.I.SetInstantCombatMusicTransitions(true);
		StartSelectedMap();
	}

	public override void _ExitTree()
	{
		AudioManager.I.SetInstantCombatMusicTransitions(false);
	}

	private void PreviousMap()
	{
		_musicSetIndex = Wrap(_musicSetIndex - 1, MusicSets.Length);
		StartSelectedMap();
	}

	private void NextMap()
	{
		_musicSetIndex = Wrap(_musicSetIndex + 1, MusicSets.Length);
		StartSelectedMap();
	}

	private void PreviousSong()
	{
		AudioManager.I.PrevSong(_layer);
		UpdateLabels();
	}

	private void NextSong()
	{
		AudioManager.I.NextSong(_layer);
		UpdateLabels();
	}

	private void StartSelectedMap()
	{
		AudioManager.I.StartCombatMusicSong(MusicSets[_musicSetIndex], 0, _layer);
		UpdateLabels();
	}

	private void SetLayer(int layer)
	{
		_layer = layer;
		AudioManager.I.SetMusicLayer(layer);
		UpdateLabels();
	}

	private void UpdateLabels()
	{
		var currentSong = AudioManager.I.GetCurrentSong() + 1;
		var songCount = AudioManager.I.GetCombatSongCount();
		var offsetDb = AudioManager.I.GetCurrentSongVolumeOffsetDb();
		var effectiveDb = AudioManager.I.GetCurrentSongVolumeDb();

		_mapLabel.Text = MusicSets[_musicSetIndex].ToString();
		_songLabel.Text = $"#{currentSong} / {songCount}: {AudioManager.I.GetCurrentSongName()}";
		_layerLabel.Text = $"Layer {_layer}";
		_volumeLabel.Text = $"Offset: {offsetDb:+0;-0;0} dB    Effective: {effectiveDb:+0;-0;0} dB";
	}

	private static int Wrap(int value, int count)
	{
		return (value % count + count) % count;
	}
}
