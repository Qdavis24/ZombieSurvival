using System;

namespace ZombieSurvival.scripts.inventory_system;

public enum ItemType
{
    BoltCutters,
    MilitaryKeyCard,
    ParasiticMaterial,
    PistolAmmo,
    RifleAmmo,
    ShotgunAmmo,
    RpgAmmo,
    Grenades,
}

public static class ItemTypeExtensions                                                                                                                                                                                            
{               
    public static ItemGroup GetGroup(this ItemType type) => type switch
    {
        ItemType.ParasiticMaterial => ItemGroup.Currency,
        ItemType.PistolAmmo or ItemType.RifleAmmo                                                                                                                                                                                 
            or ItemType.ShotgunAmmo or ItemType.RpgAmmo => ItemGroup.Ammo,
        ItemType.MilitaryKeyCard or ItemType.BoltCutters => ItemGroup.Key,                                                                                                                                                                    
        ItemType.Grenades => ItemGroup.Consumable,                                                                                                                                                                               
        _ => throw new ArgumentOutOfRangeException()                                                                                                                                                                              
    };                                                                                                                                                                                                                            
}      