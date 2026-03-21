namespace ZombieSurvival.scripts.inventory_system;

public class InventoryItem(ItemType type, int amount)
{
    public ItemType Type = type;
    public int Amount = amount;
}