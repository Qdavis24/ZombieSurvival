using Godot;
using ZombieSurvival.scripts.inventory_system;

public abstract partial class InteractableBase : Node3D, IInteractable
{
    [Export] private Area3D _interactionRange;
    public virtual string InteractPrompt => "Press E to interact";

    protected Node3D InteractionRangeBody;

    public override void _Ready()
    {
        if (EventBus.Instance == null)
        {
            GD.PrintErr($"{nameof(InteractableBase)}: EventBus autoload is missing. Add it in Project > Autoloads.");
            return;
        }

        _interactionRange.BodyEntered += OnBodyEntered;
        _interactionRange.BodyExited += OnBodyExited;
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustPressed("interact"))
        {
            OnInteract();
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        if (body is not IInventoryOwner) return;
        InteractionRangeBody = body;
        EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerEnteredInteractableRange, this);
        OnPlayerEnteredRange(body);
    }

    private void OnBodyExited(Node3D body)
    {
        if (body is not IInventoryOwner) return;
        InteractionRangeBody = null;
        EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerExitedInteractableRange, this);
        OnPlayerExitedRange(body);
    }

    protected virtual void OnPlayerEnteredRange(Node3D body) { }
    protected virtual void OnPlayerExitedRange(Node3D body) { }
    
    // BaseInteractable.cs
    protected virtual void Disable()
    {
        if (InteractionRangeBody != null)
            EventBus.Instance.EmitSignal(EventBus.SignalName.PlayerExitedInteractableRange, this);
        
        _interactionRange.QueueFree();
    }

    protected abstract void OnInteract();
}