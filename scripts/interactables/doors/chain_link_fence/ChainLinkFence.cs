using Godot;
using ZombieSurvival.scripts.inventory_system;
using ZombieSurvival.scripts.shared;

public partial class ChainLinkFence : Node3D
{
    [Export] private InteractNotifier _interactNotifier;
    [Export] private ItemType _requiredItem;
    [Export] private Node3D _pivotPoint;
    [Export] private CollisionShape3D _invisibleWall;
    [Export] private AudioStream _lockedSound;
    [Export] private AudioStream _lockBreakSound;
    [Export] private MeshInstance3D _lockMesh;
    [Export] private PackedScene _brokenLock;
    [Export] private MapArea _mapAreaDoorUnlocks;

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
            EventBus.Instance.EmitSignal(EventBus.SignalName.MapAreaUnlocked, (int)_mapAreaDoorUnlocks);
            OpenGate();
            _interactNotifier.Disable();
            _invisibleWall.QueueFree();
            AudioManager.I.Play3D(_lockBreakSound, GlobalPosition, -18f);
            SpawnBrokenLock();
        }
        else
        {
            AudioManager.I.Play3D(_lockedSound, GlobalPosition, -8f);
            ShakeLock();
        }
    }

    private void OpenGate()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(_pivotPoint, "rotation:y", Mathf.DegToRad(-90f), 1.0f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }

    private void ShakeLock()
    {
        if (_lockMesh == null) return;
        var origin = _lockMesh.Position;
        Tween tween = CreateTween();
        tween.TweenProperty(_lockMesh, "position:x", origin.X + 0.04f, 0.05f);
        tween.TweenProperty(_lockMesh, "position:x", origin.X - 0.04f, 0.05f);
        tween.TweenProperty(_lockMesh, "position:x", origin.X + 0.03f, 0.04f);
        tween.TweenProperty(_lockMesh, "position:x", origin.X - 0.03f, 0.04f);
        tween.TweenProperty(_lockMesh, "position:x", origin.X, 0.04f);
    }

    private void SpawnBrokenLock()
    {
        if (_lockMesh == null) return;
        _lockMesh.Visible = false;

        if (_brokenLock == null) return;
        var instance = _brokenLock.Instantiate<RigidBody3D>();
        Containers.Instance.VFX.AddChild(instance);
        instance.GlobalPosition = _lockMesh.GlobalPosition;
        instance.GlobalRotation = _lockMesh.GlobalRotation;
        instance.ApplyImpulse(Vector3.Up * 2f);
    }
}