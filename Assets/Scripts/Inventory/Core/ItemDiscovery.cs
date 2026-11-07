using System;
using System.Collections.Generic;

// Which items the player has ever had. A recipe unlocks once every one of its ingredients has been discovered.
// Stored as ids so a save system can persist it later.
public class ItemDiscovery
{
    private readonly HashSet<string> discovered = new HashSet<string>();
    private readonly IReadOnlyList<RecipeDefinition> recipes;

    public event Action<ItemDefinition> ItemDiscovered;
    public event Action<RecipeDefinition> RecipeUnlocked;

    public ItemDiscovery(IReadOnlyList<RecipeDefinition> recipes)
    {
        this.recipes = recipes ?? Array.Empty<RecipeDefinition>();
    }

    public IEnumerable<string> DiscoveredIds => discovered;

    public bool IsDiscovered(ItemDefinition item) => item != null && discovered.Contains(item.id);

    public bool IsUnlocked(RecipeDefinition recipe)
    {
        if (recipe == null) return false;
        foreach (var ingredient in recipe.ingredients)
        {
            if (!IsDiscovered(ingredient.item)) return false;
        }
        return true;
    }

    // Returns true the first time an item is seen, and raises RecipeUnlocked for each recipe it completes.
    public bool Discover(ItemDefinition item)
    {
        if (item == null || !discovered.Add(item.id)) return false;

        ItemDiscovered?.Invoke(item);
        foreach (var recipe in recipes)
        {
            if (recipe == null || !Uses(recipe, item)) continue;
            if (IsUnlocked(recipe)) RecipeUnlocked?.Invoke(recipe);
        }
        return true;
    }

    private static bool Uses(RecipeDefinition recipe, ItemDefinition item)
    {
        foreach (var ingredient in recipe.ingredients)
        {
            if (ingredient.item == item) return true;
        }
        return false;
    }
}
