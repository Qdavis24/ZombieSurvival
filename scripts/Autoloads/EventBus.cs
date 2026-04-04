using Godot;

public partial class EventBus : Node
{
	public static EventBus Instance { get; private set; }

	[Signal]
	public delegate void PlayerEnteredInteractableRangeEventHandler(InteractNotifier interactable);

	[Signal]
	public delegate void PlayerExitedInteractableRangeEventHandler(InteractNotifier interactable);

	public override void _Ready()
	{
		Instance = this;
	}
}
