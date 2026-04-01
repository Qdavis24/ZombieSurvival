using Godot;
using System;
using ZombieSurvival.scripts.inventory_system;

public partial class ParasiticMaterial : RigidBody3D
{
	[Export] private GpuParticles3D _blowupParticles;
	[Export] private GpuParticles3D _pickupParticles;
	[Export] private Timer _despawnTimer;
	[Export] private Area3D _pickupRange;
	[Export] private MeshInstance3D _parasiteMesh;
	private Node3D _target;
	private float _speed = 3.0f;
	
	public override void _Ready()
	{
		_despawnTimer.Timeout += TriggerFree;
		_pickupRange.BodyEntered += PickupRangeOnBodyEntered;
		_despawnTimer.Start();
	}

	private void PickupRangeOnBodyEntered(Node3D body)
	{
		if (body is IInventoryOwner inventoryOwner)
		{
			inventoryOwner.Inventory.AddItem(ItemType.ParasiticMaterial, 1);
			TriggerFree();
		}
	}

	private void TriggerFree()
	{
		_pickupRange.BodyEntered -= PickupRangeOnBodyEntered;
		_parasiteMesh.Visible = false;
		_pickupParticles.Emitting = false;
		_blowupParticles.Emitting = true;
		_blowupParticles.Finished += QueueFree;
	}
	
}
