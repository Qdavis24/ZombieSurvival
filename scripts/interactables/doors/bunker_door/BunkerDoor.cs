using Godot;
using ZombieSurvival.scripts.inventory_system;
using ZombieSurvival.scripts.shared;

public partial class BunkerDoor : Node3D
{
    [Signal] public delegate void OpenedEventHandler();

    [Export] private InteractNotifier _interactNotifier;
    [Export] private Node3D _pivotPoint;
    [Export] private Node3D _doorMesh;
    [Export] private Node3D _doorCollision;
    [Export] private float _openAngleDegrees = -140f;
    [Export] private ItemType _requiredItem = ItemType.BoltCutters;
    [Export] private bool _unlocksMapArea;
    [Export] private MapArea _mapAreaDoorUnlocks;

    private bool _opened;

    public override void _Ready()
    {
        if (_interactNotifier == null)
        {
            GD.PrintErr("BunkerDoor: _interactNotifier not assigned.");
            return;
        }

        // Hook up interaction first so it never depends on the door-mesh setup below.
        _interactNotifier.PlayerEnteredRange += OnPlayerEnteredRange;
        _interactNotifier.PlayerExitedRange += OnPlayerExitedRange;
        _interactNotifier.Interacted += OnInteracted;

        SetupPivot();
    }

    // Place the (axis-aligned) pivot at the door's hinge (the door mesh origin) and bring
    // the door mesh + its collider under it, so rotating the pivot swings the whole door.
    private void SetupPivot()
    {
        if (_pivotPoint == null || _doorMesh == null)
        {
            GD.PrintErr("BunkerDoor: _pivotPoint or _doorMesh not assigned; door won't swing.");
            return;
        }

        // The collider may already be a child of the pivot; remember its world placement
        // so moving the pivot to the hinge doesn't drag it off the doorway opening.
        var colliderGlobal = _doorCollision?.GlobalTransform;

        _pivotPoint.GlobalPosition = _doorMesh.GlobalPosition;
        _doorMesh.Reparent(_pivotPoint);

        if (_doorCollision != null)
        {
            if (_doorCollision.GetParent() != _pivotPoint)
                _doorCollision.Reparent(_pivotPoint);
            _doorCollision.GlobalTransform = colliderGlobal.Value;
        }
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
        if (_opened) return;
        if (player is not IInventoryOwner inventoryOwner) return;
        if (inventoryOwner.Inventory.GetAmount(_requiredItem) < 1) return;

        _opened = true;
        OpenDoor();
        _interactNotifier.Disable();

        if (_unlocksMapArea)
            EventBus.Instance.EmitSignal(EventBus.SignalName.MapAreaUnlocked, (int)_mapAreaDoorUnlocks);

        EmitSignalOpened();
    }

    public void OpenDoor()
    {
        if (_pivotPoint == null) return;

        var tween = CreateTween();
        tween.TweenProperty(_pivotPoint, "rotation:y", Mathf.DegToRad(_openAngleDegrees), 1.0f)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.InOut);
    }
}
