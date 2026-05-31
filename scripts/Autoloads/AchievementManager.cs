using Godot;
using System.Collections.Generic;

// Tracks achievement unlocks + lifetime stats, persisted to user://stats.cfg.
// Listens to existing EventBus signals so gameplay code stays untouched.
public partial class AchievementManager : Node
{
    public static AchievementManager Instance { get; private set; }

    [Signal] public delegate void AchievementUnlockedEventHandler(string id);

    public enum Stat { None, Kills, Headshots, BestRound }

    public sealed class AchievementDef
    {
        public string Id;
        public string Name;
        public string Description;
        public int Target;          // 0 = one-shot (no progress bar)
        public Stat ProgressStat;   // which lifetime stat drives the progress bar
    }

    public readonly List<AchievementDef> Definitions = new()
    {
        new() { Id = "downtown", Name = "Downtown", Description = "Play the Downtown map." },
        new() { Id = "bunker", Name = "Bunker", Description = "Play the Bunker map." },
        new() { Id = "forest", Name = "Forest", Description = "Play the Forest map." },
        new() { Id = "round5", Name = "Still Standing I", Description = "Reach round 5.", Target = 5, ProgressStat = Stat.BestRound },
        new() { Id = "round10", Name = "Still Standing II", Description = "Reach round 10.", Target = 10, ProgressStat = Stat.BestRound },
        new() { Id = "round15", Name = "Still Standing III", Description = "Reach round 15.", Target = 15, ProgressStat = Stat.BestRound },
        new() { Id = "kills10", Name = "Getting Started", Description = "Kill 10 zombies.", Target = 10, ProgressStat = Stat.Kills },
        new() { Id = "headshots100", Name = "Brains", Description = "Land 100 headshot kills.", Target = 100, ProgressStat = Stat.Headshots },
        new() { Id = "juiced", Name = "Juiced", Description = "Buy all 3 perks in a single run." },
        new() { Id = "kills1000", Name = "Hopefully There Isn't a Cure", Description = "Kill 1,000 zombies total.", Target = 1000, ProgressStat = Stat.Kills },
    };

    private const string Path = "user://stats.cfg";
    private const string StatsSection = "stats";
    private const string UnlockSection = "unlocked";

    public int TotalKills { get; private set; }
    public int TotalHeadshots { get; private set; }
    public int BestRound { get; private set; }

    private readonly HashSet<string> _unlocked = new();
    private int _perksThisRun;

    public override void _Ready()
    {
        Instance = this;
        Load();

        if (EventBus.Instance == null)
            return;

        EventBus.Instance.ZombieKilled += OnZombieKilled;
        EventBus.Instance.GameStarted += OnGameStarted;
        EventBus.Instance.RoundReached += OnRoundReached;
        EventBus.Instance.PerkPurchased += OnPerkPurchased;
    }

    public bool IsUnlocked(string id) => _unlocked.Contains(id);

    public int GetProgress(AchievementDef def) => def.ProgressStat switch
    {
        Stat.Kills => TotalKills,
        Stat.Headshots => TotalHeadshots,
        Stat.BestRound => BestRound,
        _ => 0,
    };

    private void OnZombieKilled(bool headshot)
    {
        TotalKills++;
        if (headshot) TotalHeadshots++;
        EvaluateProgressAchievements();
        Save();
    }

    private void OnGameStarted(int levelIndex)
    {
        _perksThisRun = 0;
        switch (levelIndex)
        {
            case 0: Unlock("downtown"); break;
            case 1: Unlock("bunker"); break;
            case 2: Unlock("forest"); break;
        }
        Save();
    }

    private void OnRoundReached(int round)
    {
        if (round > BestRound)
            BestRound = round;
        EvaluateProgressAchievements();
        Save();
    }

    private void OnPerkPurchased(int perkType)
    {
        _perksThisRun++;
        if (_perksThisRun >= 3)
            Unlock("juiced");
        Save();
    }

    private void EvaluateProgressAchievements()
    {
        foreach (var def in Definitions)
            if (def.Target > 0 && !_unlocked.Contains(def.Id) && GetProgress(def) >= def.Target)
                Unlock(def.Id);
    }

    private void Unlock(string id)
    {
        if (!_unlocked.Add(id))
            return;

        EmitSignal(SignalName.AchievementUnlocked, id);
        // Later: forward to Steam (SteamUserStats.SetAchievement) here.
    }

    private void Load()
    {
        var config = new ConfigFile();
        if (config.Load(Path) != Error.Ok)
            return;

        TotalKills = (int)config.GetValue(StatsSection, "total_kills", 0);
        TotalHeadshots = (int)config.GetValue(StatsSection, "total_headshots", 0);
        BestRound = (int)config.GetValue(StatsSection, "best_round", 0);

        foreach (var def in Definitions)
            if ((bool)config.GetValue(UnlockSection, def.Id, false))
                _unlocked.Add(def.Id);
    }

    private void Save()
    {
        var config = new ConfigFile();
        config.SetValue(StatsSection, "total_kills", TotalKills);
        config.SetValue(StatsSection, "total_headshots", TotalHeadshots);
        config.SetValue(StatsSection, "best_round", BestRound);

        foreach (var def in Definitions)
            config.SetValue(UnlockSection, def.Id, _unlocked.Contains(def.Id));

        config.Save(Path);
    }
}
