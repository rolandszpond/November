using System;

// What an item can be used for. Flags so one item can serve several purposes (an axe is Tool | Weapon).
[Flags]
public enum ItemCategory
{
    None = 0,
    Resource = 1 << 0,
    Building = 1 << 1,
    Tool = 1 << 2,
    Weapon = 1 << 3,
    Food = 1 << 4,
    Backpack = 1 << 5,
    Station = 1 << 6,
}

public enum ToolType
{
    None,
    Axe,
    Pickaxe,
}

// Where a recipe can be crafted. None = by hand from the inventory menu (also available at every station).
public enum CraftingStationType
{
    None,
    Workbench,
}
