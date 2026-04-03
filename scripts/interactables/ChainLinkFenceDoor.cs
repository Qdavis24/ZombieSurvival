using Godot;
using System;
using ZombieSurvival.scripts.inventory_system;

public partial class ChainLinkFenceDoor : InteractableBase
{
	[Export] private ItemType _requiredItem;
	[Export] private Node3D _pivotPoint;
	[Export] private CollisionShape3D _invisibleWall;

	protected override void OnInteract()
	{
		GD.Print("interact");
		if (InteractionRangeBody is IInventoryOwner inventoryOwner)
		{
			if (inventoryOwner.Inventory.ConsumeItem(_requiredItem, 1))
			{
				OpenGate();
				Disable();
			}
		}
	}

	private void OpenGate()
	{
		Tween tween = CreateTween();
		tween.TweenProperty(_pivotPoint, "rotation:y", Mathf.DegToRad(-90f), 1.0f)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.InOut);
	}

	protected override void Disable()
	{
		base.Disable();
		_invisibleWall.QueueFree();
	}
}
