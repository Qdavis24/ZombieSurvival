using Godot;
using ZombieSurvival.scripts.inventory_system;
using ZombieSurvival.scripts.shared;

public partial class RpgChest : Node3D
{
    [Export] private InteractNotifier _interactNotifier;
    [Export] private ItemType _requiredItem;
   
    [Export] private AudioStream _lockedSound;
    [Export] private AudioStream _successfulSound;
    
    [Export] private MeshInstance3D _chestMesh;
    [Export] private MeshInstance3D _openChestMesh;

    [Export] private PackedScene _chestTop;
    [Export] private Sprite3D _keyPadLight;

    public override void _Ready()
    {
        _interactNotifier.PlayerEnteredRange += OnPlayerEnteredRange;
        _interactNotifier.PlayerExitedRange += OnPlayerExitedRange;
        _interactNotifier.Interacted += OnInteracted;
    }

    private void OnPlayerEnteredRange()
    {
        EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerEnteredInteractableRange, _interactNotifier);
    }

    private void OnPlayerExitedRange()
    {
        EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerExitedInteractableRange, _interactNotifier);
    }

    private void OnInteracted(Node3D player)
    {
        if (player is not IInventoryOwner inventoryOwner) return;
        if (inventoryOwner.Inventory.ConsumeItem(_requiredItem, 1))
        {
            OpenChest();
            _interactNotifier.Disable();
            AudioManager.I.Play3D(_successfulSound, GlobalPosition, -18f);
            SpawnTop();
            FlickerScreen(Colors.Green);
        }
        else
        {
            AudioManager.I.Play3D(_lockedSound, GlobalPosition, -8f);
            FlickerScreen(Colors.Red);
        }
    }

    private void FlickerScreen(Color color)
    {
        _keyPadLight.Modulate = color;
        _keyPadLight.Visible = true;
        var tween = CreateTween();
        tween.TweenProperty(_keyPadLight, "modulate:a", 0f, 0.35f)
            .SetTrans(Tween.TransitionType.Expo)
            .SetEase(Tween.EaseType.In);
        tween.TweenCallback(Callable.From(() => _keyPadLight.Visible = false));
    }

    private void OpenChest()
    {
        _chestMesh.Visible = false;
        _openChestMesh.Visible = true;
    }

    private void SpawnTop()
    {
        var top = _chestTop.Instantiate<RigidBody3D>();
        Containers.Instance.VFX.AddChild(top);
        top.GlobalTransform = _chestMesh.GlobalTransform;
        top.ApplyCentralImpulse(new Vector3(0, 2.5f, 0.5f) * 2f);
    }
}