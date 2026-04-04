using Godot;
using ZombieSurvival.scripts.inventory_system;

public partial class ChainLinkFence : Node3D
{
    [Export] private InteractNotifier _interactNotifier;
    [Export] private ItemType _requiredItem;
    [Export] private Node3D _pivotPoint;
    [Export] private CollisionShape3D _invisibleWall;
    [Export] private AudioStream _lockedSound;

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
            OpenGate();
            _interactNotifier.Disable();
            _invisibleWall.QueueFree();
        }
        else
            AudioManager.I.Play3D(_lockedSound, GlobalPosition);
    }

    private void OpenGate()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(_pivotPoint, "rotation:y", Mathf.DegToRad(-90f), 1.0f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }
}
