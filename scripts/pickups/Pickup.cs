using Godot;
using System;
using ZombieSurvival.scripts.inventory_system;

public partial class Pickup : RigidBody3D
{
	[Export] private Timer _despawnTimer;
	[Export] private Area3D _pickupRange;
	[Export] private ItemType _itemType;
	[Export] private int _itemAmount;
	[Export] private AudioStream _pickupSound;
	
	public override void _Ready()
	{
		_despawnTimer.Timeout += QueueFree;
		_pickupRange.BodyEntered += PickupRangeOnBodyEntered;
		_despawnTimer.Start();
	}

	private void PickupRangeOnBodyEntered(Node3D body)
	{
		if (body is IInventoryOwner inventoryOwner)
		{
			inventoryOwner.Inventory.AddItem(_itemType, _itemAmount);
			AudioManager.I.Play3D(_pickupSound, GlobalPosition);
			CallDeferred(MethodName.QueueFree);
			
			// var result = inventoryOwner.Inventory.AddItem(_itemType, _itemAmount);
			// AudioManager.I.Play3D(_pickupSound, GlobalPosition);
			// if(result.Amount == _itemAmount)
			// 	CallDeferred(MethodName.QueueFree);
			// _itemAmount -= result.Amount;
		}
	}

	public int GetItemAmount()
	{
		return _itemAmount;
	}
}
