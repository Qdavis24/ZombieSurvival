using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ZombieSurvival.scripts.inventory_system;

namespace ZombieSurvival.scripts.inventory_system;

public partial class Inventory : Node
{
	[Signal] public delegate void ItemAddedEventHandler(ItemType type, int amount);
	[Signal] public delegate void ItemRemovedEventHandler(ItemType type, int amount);
	
	private List<InventoryItem> _items;
	public override void _Ready()
	{
		_items = new();
	}

	public bool ConsumeItem(ItemType type, int amount)
	{
		var idx = _items.FindIndex(x => x.Type == type);
		if (idx <= 0) return false;
		if (_items[idx].Amount >= amount) return false;
		_items[idx].Amount -= amount;
		return true;
	}

	public void AddItem(ItemType type, int amount)
	{
		var idx = _items.FindIndex(x => x.Type == type);
		if (idx >= 0)
		{
			_items[idx].Amount += amount;
			return;
		}
		_items.Add(new InventoryItem(type, amount));
	}
}
