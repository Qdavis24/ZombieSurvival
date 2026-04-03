using Godot;

public partial class EventBus : Node
{
	public static EventBus Instance { get; private set; }

	[Signal]
	public delegate void PlayerEnteredInteractableRangeEventHandler(Node3D interactable);

	[Signal]
	public delegate void PlayerExitedInteractableRangeEventHandler(Node3D interactable);

	public override void _Ready()
	{
		Instance = this;
	}
}
