// Why the inventory screen is open: from Tab (hand crafting), at a crafting station, or looting a container.
// Decides what the right-hand panel shows.
public class InventoryContext
{
    public enum Kind { Personal, Station, Loot }

    public Kind kind;
    public CraftingStationType station;
    public string title;
    public LootContainer loot;

    public static InventoryContext Personal() =>
        new InventoryContext { kind = Kind.Personal, station = CraftingStationType.None, title = "Crafting" };

    public static InventoryContext ForStation(CraftingStation s) =>
        new InventoryContext { kind = Kind.Station, station = s.StationType, title = s.DisplayName };

    public static InventoryContext ForLoot(LootContainer container) =>
        new InventoryContext { kind = Kind.Loot, station = CraftingStationType.None, title = container.DisplayName, loot = container };
}
