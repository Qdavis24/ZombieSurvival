using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using ZombieSurvival.scripts.inventory_system;

namespace ZombieSurvival.scripts.inventory_system;

public partial class Inventory : Node
{
    
    [Signal]
    public delegate void ItemAddedEventHandler(ItemType type, int amount);

    [Signal]
    public delegate void ItemRemovedEventHandler(ItemType type, int amount);
    
    private List<InventoryItem> _items = new();
    
    public int GetAmount(ItemType type)
    {
        var idx = _items.FindIndex(x => x.Type == type);
        return idx >= 0 ? _items[idx].Amount : 0;
    }

    public bool ConsumeItem(ItemType type, int amount)
    {
        var idx = _items.FindIndex(x => x.Type == type);
        if (idx < 0) return false;
        if (_items[idx].Amount < amount) return false;

        var result = _items[idx].TryDecrement(amount);
        
        EmitSignalItemRemoved(type, result.Amount);
        return true;
    }

    public InventoryItem.AddResult AddItem(ItemType type, int amount)
    {
        var idx = _items.FindIndex(x => x.Type == type);
        InventoryItem.AddResult result;
        if (idx >= 0)
        {
            result = _items[idx].TryIncrement(amount);
        }
        else
        {
            var item = new InventoryItem(type);
            _items.Add(item);
            result = item.TryIncrement(amount);
        }
        
        EmitSignalItemAdded(type, result.Amount);
        return result;
    }
}