using Godot;
using ZombieSurvival.scripts.shared;

public partial class EventBus : Node
{
    public static EventBus Instance { get; private set; }

    [Signal]
    public delegate void PlayerEnteredInteractableRangeEventHandler(InteractNotifier interactable);

    [Signal]
    public delegate void PlayerExitedInteractableRangeEventHandler(InteractNotifier interactable);

    [Signal]
    public delegate void MapAreaUnlockedEventHandler(int mapArea);

    public override void _Ready()
    {
        Instance = this;
    }
}