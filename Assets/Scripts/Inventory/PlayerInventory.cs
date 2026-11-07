using System;
using System.Collections.Generic;
using UnityEngine;

// The player's items: the 8-slot hotbar (which is also the default inventory), the backpack section (sized by the
// equipped backpack, 0 slots without one), the backpack equip slot, and which items have been discovered.
// All slot rules live in InventoryContainer / CraftingLogic; this adds the player-specific rules on top
// (backpack equipping, world drops, auto-equip on craft, carry weight).
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private ItemDatabase itemDatabase;
    [SerializeField] private RecipeDatabase recipeDatabase;
    [SerializeField] private int hotbarSize = 8;
    [Tooltip("Carry weight (kg) before the player is slowed. The equipped backpack adds to this.")]
    [SerializeField] private float baseCarryWeight = 30f;
    [Tooltip("Given on start (handy for testing).")]
    [SerializeField] private List<ItemAmount> startingItems = new List<ItemAmount>();

    private InventoryContainer[] containers;
    private int selectedIndex;

    public ItemDatabase Items => itemDatabase;
    public RecipeDatabase Recipes => recipeDatabase;
    public InventoryContainer Hotbar { get; private set; }
    public InventoryContainer Backpack { get; private set; }
    // A 1-slot container so the UI can treat the equip slot like any other slot.
    public InventoryContainer BackpackEquip { get; private set; }
    public ItemDiscovery Discovery { get; private set; }

    // Hotbar first, then backpack: the order items are added in.
    public IReadOnlyList<InventoryContainer> Containers => containers;

    public ItemStack EquippedBackpack => BackpackEquip[0];
    public int SelectedIndex => selectedIndex;
    public ItemStack SelectedStack => Hotbar[selectedIndex];

    public float TotalWeight => Hotbar.TotalWeight + Backpack.TotalWeight + BackpackEquip.TotalWeight;
    public float MaxCarryWeight => baseCarryWeight + (EquippedBackpack.IsEmpty ? 0f : EquippedBackpack.item.backpack.carryWeightBonus);
    public bool IsOverencumbered => TotalWeight > MaxCarryWeight;

    // Any slot in any of the player's containers changed (or the backpack was resized).
    public event Action Changed;
    public event Action<int> SelectionChanged;

    private void Awake()
    {
        Hotbar = new InventoryContainer(hotbarSize);
        Backpack = new InventoryContainer(0);
        BackpackEquip = new InventoryContainer(1);
        containers = new[] { Hotbar, Backpack };
        Discovery = new ItemDiscovery(recipeDatabase != null ? recipeDatabase.recipes : null);

        // Anything that lands in one of the player's slots (picked up, crafted, dragged out of a chest) is discovered.
        foreach (var container in new[] { Hotbar, Backpack, BackpackEquip })
        {
            var c = container;
            c.SlotChanged += index =>
            {
                if (index >= 0 && index < c.Size && !c[index].IsEmpty) Discovery.Discover(c[index].item);
                Changed?.Invoke();
            };
        }
    }

    private void Start()
    {
        foreach (var entry in startingItems)
        {
            AddOrDrop(entry.item, entry.count);
        }
    }

    public void Select(int index)
    {
        index = ((index % hotbarSize) + hotbarSize) % hotbarSize;
        if (index == selectedIndex) return;
        selectedIndex = index;
        SelectionChanged?.Invoke(selectedIndex);
    }

    public int Count(ItemDefinition item) => CraftingLogic.CountAll(containers, item);

    // ---------------------------------------------------------------- Adding / removing

    // Adds to the hotbar, then the backpack. Returns what didn't fit (nothing is dropped).
    public int Add(ItemDefinition item, int count)
    {
        if (item == null || count <= 0) return count;
        int leftover = CraftingLogic.AddAll(containers, item, count);
        if (leftover < count) Discovery.Discover(item);
        return leftover;
    }

    // Adds what fits and drops the rest at the player's feet.
    public void AddOrDrop(ItemDefinition item, int count)
    {
        int leftover = Add(item, count);
        if (leftover > 0)
        {
            Discovery.Discover(item);
            DropInWorld(new ItemStack(item, leftover));
        }
    }

    public int Remove(ItemDefinition item, int count) => CraftingLogic.RemoveAll(containers, item, count);

    // Spawns a stack in front of the player with a small toss.
    public WorldItem DropInWorld(ItemStack stack)
    {
        if (stack.IsEmpty) return null;
        Vector3 forward = transform.forward;
        Vector3 position = transform.position + forward * 0.8f + Vector3.up * 1.2f;
        return WorldItem.Spawn(stack, position, forward * 2f + Vector3.up * 1.5f);
    }

    public void DropFromSlot(InventoryContainer container, int index, int amount)
    {
        if (container == BackpackEquip)
        {
            if (!Backpack.IsEmpty) return; // can't drop a backpack that still holds items
            DropInWorld(container.TakeFrom(index, amount));
            ApplyBackpackSize();
            return;
        }
        DropInWorld(container.TakeFrom(index, amount));
    }

    // ---------------------------------------------------------------- Backpack

    // Equips a backpack from a slot. A previously equipped backpack goes back into that slot.
    // Fails if the backpack section's items wouldn't fit in the new size.
    public bool TryEquipBackpack(InventoryContainer from, int index)
    {
        var stack = from[index];
        if (stack.IsEmpty || !stack.item.backpack.enabled || from == BackpackEquip) return false;

        int newSize = stack.item.backpack.slots;
        var old = EquippedBackpack;

        // Take it out first: if it was stored in the backpack section itself, that frees its slot.
        from.Clear(index);
        if (!Backpack.FitsIn(newSize))
        {
            from.Set(index, stack);
            return false;
        }

        BackpackEquip.Set(0, stack);
        ApplyBackpackSize();

        if (!old.IsEmpty)
        {
            if (index < from.Size && from[index].IsEmpty) from.Set(index, old);
            else AddOrDrop(old.item, old.count);
        }
        return true;
    }

    // Moves the equipped backpack into a slot (or the first free one when index < 0). Only allowed when the
    // backpack section is empty, since those slots disappear with it.
    public bool TryUnequipBackpack(InventoryContainer to, int index)
    {
        var stack = EquippedBackpack;
        if (stack.IsEmpty || !Backpack.IsEmpty || to == Backpack) return false;

        if (index < 0)
        {
            if (Hotbar.Capacity(stack.item) < 1) return false;
            BackpackEquip.Clear(0);
            Hotbar.TryAdd(stack.item, 1);
        }
        else
        {
            if (!to[index].IsEmpty)
            {
                // Swapping onto another backpack is an equip of that one.
                return to[index].item.backpack.enabled && TryEquipBackpack(to, index);
            }
            BackpackEquip.Clear(0);
            to.Set(index, stack);
        }

        ApplyBackpackSize();
        return true;
    }

    public bool CanUnequipBackpack => !EquippedBackpack.IsEmpty && Backpack.IsEmpty;

    private void ApplyBackpackSize()
    {
        int size = EquippedBackpack.IsEmpty ? 0 : EquippedBackpack.item.backpack.slots;
        Backpack.Compact(size);
        if (!Backpack.Resize(size))
        {
            Debug.LogWarning("Backpack resize refused; items would be lost.");
        }
    }

    // ---------------------------------------------------------------- Crafting

    public bool CanCraft(RecipeDefinition recipe, CraftingStationType station) =>
        CraftingLogic.CanCraft(recipe, Discovery, containers, station);

    public int MaxCraftable(RecipeDefinition recipe) => CraftingLogic.MaxCraftable(recipe, containers);

    public bool Craft(RecipeDefinition recipe, CraftingStationType station)
    {
        if (!CanCraft(recipe, station)) return false;

        CraftingLogic.ConsumeIngredients(recipe, containers);
        int count = recipe.outputCount;
        Discovery.Discover(recipe.output);

        // A crafted backpack goes straight on if nothing is equipped.
        if (recipe.output.backpack.enabled && EquippedBackpack.IsEmpty && count > 0)
        {
            BackpackEquip.Set(0, new ItemStack(recipe.output, 1));
            ApplyBackpackSize();
            count--;
        }

        AddOrDrop(recipe.output, count);
        return true;
    }

    public RecipeDefinition RecipeFor(ItemDefinition item) => recipeDatabase != null ? recipeDatabase.ForOutput(item) : null;

    public bool CanRecycle(ItemStack stack, CraftingStationType station) =>
        !stack.IsEmpty && CraftingLogic.CanRecycle(RecipeFor(stack.item), stack.count, station);

    // Breaks down the whole stack in a slot into part of its ingredients.
    public bool Recycle(InventoryContainer container, int index, CraftingStationType station)
    {
        var stack = container[index];
        if (container == BackpackEquip || !CanRecycle(stack, station)) return false;

        var yield = CraftingLogic.RecycleYield(RecipeFor(stack.item), stack.count);
        container.Clear(index);
        foreach (var amount in yield)
        {
            AddOrDrop(amount.item, amount.count);
        }
        return true;
    }
}
