using System;

namespace ZombieSurvival.scripts.inventory_system;

public enum ItemType
{
    BoltCutters,
    MilitaryKeyCard,
    Money,
    PistolAmmo,
    RifleAmmo,
    ShotgunAmmo,
    RpgAmmo,
    Grenades,
    Shotgun,
    Rifle,
    Rpg,
}

public static class ItemTypeExtensions                                                                                                                                                                                            
{               
    public static ItemGroup GetGroup(this ItemType type) => type switch
    {
        ItemType.Money => ItemGroup.Currency,
        ItemType.PistolAmmo or ItemType.RifleAmmo                                                                                                                                                                                 
            or ItemType.ShotgunAmmo or ItemType.RpgAmmo => ItemGroup.Ammo,
        ItemType.MilitaryKeyCard or ItemType.BoltCutters => ItemGroup.Key,                                                                                                                                                                    
        ItemType.Grenades => ItemGroup.Consumable,
        ItemType.Shotgun or ItemType.Rifle or ItemType.Rpg => ItemGroup.Weapon,
        _ => throw new ArgumentOutOfRangeException()                                                                                                                                                                              
    };                                                                                                                                                                                                                            
}      