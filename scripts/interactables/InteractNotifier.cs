using Godot;
using ZombieSurvival.scripts.inventory_system;

public partial class InteractNotifier : Node3D
{
    [Export] private Area3D _interactionRange;
    [Export] public string InteractPrompt { get; private set; }

    private Node3D _playerInRange;

    [Signal]
    public delegate void PlayerEnteredRangeEventHandler();

    [Signal]
    public delegate void PlayerExitedRangeEventHandler();

    [Signal]
    public delegate void InteractedEventHandler(Node3D player);

    public override void _Ready()
    {
        if (EventBus.Instance == null)
        {
            GD.PrintErr($"{nameof(InteractNotifier)}: EventBus autoload is missing. Add it in Project > Autoloads.");
            return;
        }

        _interactionRange.BodyEntered += OnBodyEntered;
        _interactionRange.BodyExited += OnBodyExited;
    }

    public override void _Input(InputEvent @event)
    {
        if (_playerInRange == null) return;
        if (Input.IsActionJustPressed("interact"))
            EmitSignal(SignalName.Interacted, _playerInRange);
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not IInventoryOwner) return;
        _playerInRange = body;
        EmitSignal(SignalName.PlayerEnteredRange);
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is not IInventoryOwner) return;
        _playerInRange = null;
        EmitSignal(SignalName.PlayerExitedRange);
    }

    public void Disable()
    {
        if (_playerInRange != null)
            EmitSignal(SignalName.PlayerExitedRange);
        _interactionRange.QueueFree();
    }
}
