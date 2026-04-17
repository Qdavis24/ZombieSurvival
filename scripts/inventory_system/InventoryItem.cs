using System;
using System.Collections.Generic;
using Godot;

namespace ZombieSurvival.scripts.inventory_system;

public class InventoryItem
{
    public enum AddResultType
    {
        Added,
        Stacked,  // already existed, incremented
        Full
    }
    
    public enum RemoveResultType
    {
        Removed,
        Emptied,  // amount hit 0
        Empty     // already at 0, nothing to remove
    }

    public struct AddResult
    {
        public AddResultType Type;
        public int Amount;
    }

    public struct RemoveResult
    {
        public RemoveResultType Type;
        public int Amount;
    }
    
    
    
    public ItemType Type;
    public int Amount => _amount;
    private int _amount;
    private int _maxAmount;

    private Dictionary<ItemType, int> _itemsMaxAmount = new()
    {
        { ItemType.PistolAmmo, 120 },
        { ItemType.ShotgunAmmo, 120 },
        { ItemType.RifleAmmo, 120 },
        { ItemType.RpgAmmo, 20 },
        { ItemType.Grenades, 20 },
    };

    public InventoryItem(ItemType type)
    {
        Type = type;
        _maxAmount = _itemsMaxAmount.TryGetValue(type, out var val) ? val : int.MaxValue;
    }

    public AddResult TryIncrement(int amount)
    {
        if (_amount >= _maxAmount)
            return new AddResult { Type = AddResultType.Full, Amount = 0 };

        int prev = _amount;
        _amount = Math.Min(_amount + amount, _maxAmount);
        return new AddResult
        {
            Type = _amount == _maxAmount ? AddResultType.Stacked : AddResultType.Added,
            Amount = _amount - prev
        };
    }

    public RemoveResult TryDecrement(int amount)
    {
        if (_amount <= 0)
            return new RemoveResult { Type = RemoveResultType.Empty, Amount = 0 };

        int prev = _amount;
        _amount = Math.Max(_amount - amount, 0);
        return new RemoveResult
        {
            Type = _amount <= 0 ? RemoveResultType.Emptied : RemoveResultType.Removed,
            Amount = prev - _amount
        };
    }
}