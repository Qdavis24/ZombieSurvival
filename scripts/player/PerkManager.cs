using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ZombieSurvival.scripts.player.weapons;

public partial class PerkManager : Node
{
    public enum PerkType
    {
        Health,
        Reload,
        Sprint
    }

    [Signal]
    public delegate void PerkAppliedEventHandler(PerkType perkType);

    [Export] private PlayerController _playerController;
    [Export] private WeaponManager _weaponManager;
    [Export] private Node3D _weaponSocket;

    [ExportGroup("Perk Scenes")]
    [Export] private PackedScene _healthPerkScene;
    [Export] private PackedScene _reloadPerkScene;
    [Export] private PackedScene _sprintPerkScene;

    [ExportGroup("Stats")]
    [Export] private float _healthPerkMaxHealth = 130f;
    [Export] private float _reloadSpeedMultiplier = 1.7f;

    private readonly HashSet<PerkType> _activePerks = new();
    private bool _isUsingPerk;

    public bool HasPerk(PerkType perkType)
    {
        return _activePerks.Contains(perkType);
    }

    public bool CanUsePerk(PerkType perkType)
    {
        return !_isUsingPerk
               && !_weaponManager.IsHandsBusy
               && !HasPerk(perkType)
               && GetPerkScene(perkType) != null;
    }

    public Task<bool> TryUseHealthPerk()
    {
        return TryUsePerk(PerkType.Health);
    }

    public Task<bool> TryUseReloadPerk()
    {
        return TryUsePerk(PerkType.Reload);
    }

    public Task<bool> TryUseSprintPerk()
    {
        return TryUsePerk(PerkType.Sprint);
    }

    public async Task<bool> TryUsePerk(PerkType perkType)
    {
        if (!CanUsePerk(perkType))
            return false;

        var perkScene = GetPerkScene(perkType);
        if (perkScene == null)
            return false;

        _isUsingPerk = true;

        try
        {
            await _weaponManager.PlayTemporaryHandAction(
                WeaponManager.HandActionState.UsingPerk,
                async () =>
                {
                    await PlayPerkAnimation(perkScene);
                    ApplyPerk(perkType);
                }
            );

            return HasPerk(perkType);
        }
        finally
        {
            _isUsingPerk = false;
        }
    }

    private PackedScene GetPerkScene(PerkType perkType)
    {
        return perkType switch
        {
            PerkType.Health => _healthPerkScene,
            PerkType.Reload => _reloadPerkScene,
            PerkType.Sprint => _sprintPerkScene,
            _ => throw new ArgumentOutOfRangeException(nameof(perkType), perkType, null)
        };
    }

    private async Task PlayPerkAnimation(PackedScene perkScene)
    {
        var perkNode = perkScene.Instantiate<Node3D>();
        _weaponSocket.AddChild(perkNode);

        var animPlayer = perkNode.GetNodeOrNull<AnimationPlayer>("Rig/perk_injection/AnimationPlayer");
        if (animPlayer == null)
        {
            perkNode.QueueFree();
            throw new InvalidOperationException($"Perk scene '{perkScene.ResourcePath}' is missing Rig/perk_injection/AnimationPlayer.");
        }

        if (!animPlayer.HasAnimation("Injecct"))
        {
            perkNode.QueueFree();
            throw new InvalidOperationException($"Perk scene '{perkScene.ResourcePath}' is missing animation 'Injecct'.");
        }

        animPlayer.Play("Injecct");
        await ToSignal(animPlayer, AnimationPlayer.SignalName.AnimationFinished);

        perkNode.QueueFree();
    }

    private void ApplyPerk(PerkType perkType)
    {
        if (!_activePerks.Add(perkType))
            return;

        switch (perkType)
        {
            case PerkType.Health:
                _playerController.SetMaxHealth(_healthPerkMaxHealth);
                break;
            case PerkType.Reload:
                _weaponManager.SetReloadSpeedMultiplier(_reloadSpeedMultiplier);
                break;
            case PerkType.Sprint:
                _playerController.SetSprintEnabled(true);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(perkType), perkType, null);
        }

        EmitSignal(SignalName.PerkApplied, Variant.From(perkType));
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("test_health_perk"))
            _ = TryUseHealthPerk();

        if (Input.IsActionJustPressed("test_reload_perk"))
            _ = TryUseReloadPerk();

        if (Input.IsActionJustPressed("test_sprint_perk"))
            _ = TryUseSprintPerk();
    }
}
