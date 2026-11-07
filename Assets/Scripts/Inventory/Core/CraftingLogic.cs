using System;
using System.Collections.Generic;

// Pure crafting/recycling rules over a set of containers (hotbar + backpack). The player-side component decides
// where outputs go (e.g. auto-equipping a backpack) and drops overflow in the world.
public static class CraftingLogic
{
    // Hand recipes work anywhere; station recipes only at that station.
    public static bool StationAllows(RecipeDefinition recipe, CraftingStationType station) =>
        recipe.station == CraftingStationType.None || recipe.station == station;

    public static int CountAll(IReadOnlyList<InventoryContainer> containers, ItemDefinition item)
    {
        int total = 0;
        foreach (var container in containers) total += container.Count(item);
        return total;
    }

    public static bool HasIngredients(RecipeDefinition recipe, IReadOnlyList<InventoryContainer> containers, int times = 1)
    {
        foreach (var ingredient in recipe.ingredients)
        {
            if (CountAll(containers, ingredient.item) < ingredient.count * times) return false;
        }
        return true;
    }

    // How many times in a row the recipe could be crafted from what's in the containers.
    public static int MaxCraftable(RecipeDefinition recipe, IReadOnlyList<InventoryContainer> containers)
    {
        int max = int.MaxValue;
        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient.item == null || ingredient.count <= 0) continue;
            max = Math.Min(max, CountAll(containers, ingredient.item) / ingredient.count);
        }
        return max == int.MaxValue ? 0 : max;
    }

    public static bool CanCraft(RecipeDefinition recipe, ItemDiscovery discovery,
        IReadOnlyList<InventoryContainer> containers, CraftingStationType station)
    {
        return recipe != null && recipe.output != null
            && discovery.IsUnlocked(recipe)
            && StationAllows(recipe, station)
            && HasIngredients(recipe, containers);
    }

    // Removes one craft's worth of ingredients. Takes from the last container first (backpack before hotbar).
    public static void ConsumeIngredients(RecipeDefinition recipe, IReadOnlyList<InventoryContainer> containers)
    {
        foreach (var ingredient in recipe.ingredients)
        {
            RemoveAll(containers, ingredient.item, ingredient.count);
        }
    }

    public static int RemoveAll(IReadOnlyList<InventoryContainer> containers, ItemDefinition item, int count)
    {
        int removed = 0;
        for (int i = containers.Count - 1; i >= 0 && removed < count; i--)
        {
            removed += containers[i].Remove(item, count - removed);
        }
        return removed;
    }

    // Adds to the containers in order (hotbar first). Returns what didn't fit.
    public static int AddAll(IReadOnlyList<InventoryContainer> containers, ItemDefinition item, int count)
    {
        foreach (var container in containers)
        {
            if (count <= 0) break;
            count = container.TryAdd(item, count);
        }
        return count;
    }

    // What recycling `count` of the recipe's output gives back. Empty if nothing would come back.
    public static List<ItemAmount> RecycleYield(RecipeDefinition recipe, int count)
    {
        var result = new List<ItemAmount>();
        if (recipe == null || !recipe.recyclable || count <= 0) return result;

        if (recipe.recycleOverride != null && recipe.recycleOverride.Count > 0)
        {
            foreach (var entry in recipe.recycleOverride)
            {
                if (entry.item != null && entry.count > 0) result.Add(new ItemAmount(entry.item, entry.count * count));
            }
            return result;
        }

        int outputCount = Math.Max(1, recipe.outputCount);
        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient.item == null) continue;
            // Small epsilon so e.g. 0.5 * 2 isn't floored to 0 by float error.
            int amount = (int)Math.Floor(ingredient.count * recipe.recycleReturn * count / (double)outputCount + 1e-6);
            if (amount > 0) result.Add(new ItemAmount(ingredient.item, amount));
        }
        return result;
    }

    public static bool CanRecycle(RecipeDefinition recipe, int count, CraftingStationType station) =>
        recipe != null && recipe.recyclable && StationAllows(recipe, station) && RecycleYield(recipe, count).Count > 0;
}
