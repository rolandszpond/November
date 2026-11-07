using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class InventoryCoreTests
{
    private readonly List<Object> created = new List<Object>();

    private ItemDefinition Item(string id, int maxStack, float weight = 1f)
    {
        var item = ScriptableObject.CreateInstance<ItemDefinition>();
        item.id = id;
        item.displayName = id;
        item.maxStack = maxStack;
        item.weight = weight;
        created.Add(item);
        return item;
    }

    private RecipeDefinition Recipe(ItemDefinition output, int outputCount, CraftingStationType station,
        params (ItemDefinition item, int count)[] ingredients)
    {
        var recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
        recipe.id = output.id;
        recipe.output = output;
        recipe.outputCount = outputCount;
        recipe.station = station;
        foreach (var (item, count) in ingredients) recipe.ingredients.Add(new ItemAmount(item, count));
        created.Add(recipe);
        return recipe;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var obj in created) Object.DestroyImmediate(obj);
        created.Clear();
    }

    // ------------------------------------------------------------------ Container

    [Test]
    public void TryAdd_TopsUpExistingStacksBeforeEmptySlots()
    {
        var stick = Item("stick", 20);
        var c = new InventoryContainer(3);
        c.Set(2, new ItemStack(stick, 15));

        int leftover = c.TryAdd(stick, 10);

        Assert.AreEqual(0, leftover);
        Assert.AreEqual(20, c[2].count);
        Assert.AreEqual(5, c[0].count);
        Assert.IsTrue(c[1].IsEmpty);
    }

    [Test]
    public void TryAdd_ReturnsOverflow_AndNonStackablesTakeOneSlotEach()
    {
        var axe = Item("axe", 1);
        var c = new InventoryContainer(2);

        int leftover = c.TryAdd(axe, 3);

        Assert.AreEqual(1, leftover);
        Assert.AreEqual(1, c[0].count);
        Assert.AreEqual(1, c[1].count);
    }

    [Test]
    public void Remove_TakesFromLastSlotsFirst()
    {
        var stone = Item("stone", 20);
        var c = new InventoryContainer(3);
        c.Set(0, new ItemStack(stone, 5));
        c.Set(2, new ItemStack(stone, 5));

        Assert.AreEqual(7, c.Remove(stone, 7));
        Assert.AreEqual(3, c[0].count);
        Assert.IsTrue(c[2].IsEmpty);
    }

    [Test]
    public void MoveOrMerge_MergesUpToMaxStack_RestStaysInSource()
    {
        var stick = Item("stick", 20);
        var a = new InventoryContainer(1);
        var b = new InventoryContainer(1);
        a.Set(0, new ItemStack(stick, 10));
        b.Set(0, new ItemStack(stick, 15));

        Assert.IsTrue(InventoryContainer.MoveOrMerge(a, 0, b, 0, 10));
        Assert.AreEqual(20, b[0].count);
        Assert.AreEqual(5, a[0].count);
    }

    [Test]
    public void MoveOrMerge_SwapsDifferentItems_OnlyWhenCarryingWholeStack()
    {
        var stick = Item("stick", 20);
        var stone = Item("stone", 20);
        var c = new InventoryContainer(2);
        c.Set(0, new ItemStack(stick, 10));
        c.Set(1, new ItemStack(stone, 4));

        Assert.IsFalse(InventoryContainer.MoveOrMerge(c, 0, c, 1, 5), "partial carry onto a different item is a no-op");
        Assert.IsTrue(InventoryContainer.MoveOrMerge(c, 0, c, 1, 10));
        Assert.AreEqual(stone, c[0].item);
        Assert.AreEqual(stick, c[1].item);
    }

    [Test]
    public void Split_MovesHalfRoundedUp()
    {
        var stick = Item("stick", 20);
        var c = new InventoryContainer(2);
        c.Set(0, new ItemStack(stick, 7));

        Assert.IsTrue(c.Split(0, 1));
        Assert.AreEqual(3, c[0].count);
        Assert.AreEqual(4, c[1].count);
    }

    [Test]
    public void Resize_RefusesToDeleteItems_CompactMakesItFit()
    {
        var stick = Item("stick", 20);
        var c = new InventoryContainer(4);
        c.Set(3, new ItemStack(stick, 1));

        Assert.IsFalse(c.Resize(2));
        Assert.IsTrue(c.FitsIn(2));
        c.Compact(2);
        Assert.IsTrue(c.Resize(2));
        Assert.AreEqual(1, c.Count(stick));
    }

    [Test]
    public void TotalWeight_SumsAllStacks()
    {
        var log = Item("log", 10, 2f);
        var c = new InventoryContainer(2);
        c.TryAdd(log, 12);
        Assert.AreEqual(24f, c.TotalWeight, 0.001f);
    }

    // ------------------------------------------------------------------ Discovery / crafting

    [Test]
    public void Recipe_UnlocksWhenAllIngredientsDiscovered()
    {
        var stick = Item("stick", 20);
        var stone = Item("stone", 20);
        var axe = Item("axe", 1);
        var recipe = Recipe(axe, 1, CraftingStationType.None, (stick, 2), (stone, 2));
        var discovery = new ItemDiscovery(new[] { recipe });
        RecipeDefinition unlocked = null;
        discovery.RecipeUnlocked += r => unlocked = r;

        discovery.Discover(stick);
        Assert.IsFalse(discovery.IsUnlocked(recipe));
        Assert.IsNull(unlocked);

        discovery.Discover(stone);
        Assert.IsTrue(discovery.IsUnlocked(recipe));
        Assert.AreEqual(recipe, unlocked);
        Assert.IsFalse(discovery.Discover(stone), "second discovery is not new");
    }

    [Test]
    public void CanCraft_CountsAcrossContainers_AndRespectsStation()
    {
        var log = Item("log", 10);
        var plank = Item("plank", 20);
        var recipe = Recipe(plank, 4, CraftingStationType.Workbench, (log, 3));
        var discovery = new ItemDiscovery(new[] { recipe });
        discovery.Discover(log);
        var hotbar = new InventoryContainer(1);
        var backpack = new InventoryContainer(1);
        hotbar.TryAdd(log, 2);
        backpack.TryAdd(log, 1);
        var containers = new[] { hotbar, backpack };

        Assert.IsFalse(CraftingLogic.CanCraft(recipe, discovery, containers, CraftingStationType.None));
        Assert.IsTrue(CraftingLogic.CanCraft(recipe, discovery, containers, CraftingStationType.Workbench));

        CraftingLogic.ConsumeIngredients(recipe, containers);
        Assert.AreEqual(0, CraftingLogic.CountAll(containers, log));
        Assert.AreEqual(0, CraftingLogic.AddAll(containers, plank, recipe.outputCount));
        Assert.AreEqual(4, CraftingLogic.CountAll(containers, plank));
    }

    [Test]
    public void HandRecipe_IsAvailableAtAnyStation()
    {
        var fiber = Item("fiber", 50);
        var bag = Item("bag", 1);
        var recipe = Recipe(bag, 1, CraftingStationType.None, (fiber, 1));
        Assert.IsTrue(CraftingLogic.StationAllows(recipe, CraftingStationType.Workbench));
    }

    [Test]
    public void AddAll_ReturnsOverflowWhenFull()
    {
        var stick = Item("stick", 5);
        var containers = new[] { new InventoryContainer(1), new InventoryContainer(1) };
        Assert.AreEqual(2, CraftingLogic.AddAll(containers, stick, 12));
    }

    [Test]
    public void RecycleYield_ReturnsFractionFlooredPerIngredient()
    {
        var stick = Item("stick", 20);
        var stone = Item("stone", 20);
        var fiber = Item("fiber", 50);
        var axe = Item("axe", 1);
        var recipe = Recipe(axe, 1, CraftingStationType.None, (stick, 2), (stone, 3), (fiber, 1));

        var yield = CraftingLogic.RecycleYield(recipe, 1);

        Assert.AreEqual(2, yield.Count, "fiber floors to 0 and is left out");
        Assert.AreEqual(1, yield[0].count);
        Assert.AreEqual(1, yield[1].count);
    }

    [Test]
    public void Recycle_IsBlockedWhenNotRecyclable_OrWrongStation()
    {
        var log = Item("log", 10);
        var plank = Item("plank", 20);
        var recipe = Recipe(plank, 4, CraftingStationType.Workbench, (log, 2));

        Assert.IsTrue(CraftingLogic.CanRecycle(recipe, 4, CraftingStationType.Workbench));
        Assert.IsFalse(CraftingLogic.CanRecycle(recipe, 4, CraftingStationType.None));
        Assert.IsFalse(CraftingLogic.CanRecycle(recipe, 1, CraftingStationType.Workbench), "yield rounds to nothing");
        recipe.recyclable = false;
        Assert.IsFalse(CraftingLogic.CanRecycle(recipe, 4, CraftingStationType.Workbench));
    }
}
