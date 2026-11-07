using System.Collections.Generic;
using UnityEngine;

// Every crafting recipe. Also used to find how an item was made, which is what recycling gives back.
[CreateAssetMenu(menuName = "Inventory/Recipe Database")]
public class RecipeDatabase : ScriptableObject
{
    public List<RecipeDefinition> recipes = new List<RecipeDefinition>();

    public RecipeDefinition Find(string id)
    {
        foreach (var recipe in recipes)
        {
            if (recipe != null && recipe.id == id)
            {
                return recipe;
            }
        }
        return null;
    }

    // The first recipe that produces this item, or null if it can't be crafted.
    public RecipeDefinition ForOutput(ItemDefinition item)
    {
        foreach (var recipe in recipes)
        {
            if (recipe != null && recipe.output == item)
            {
                return recipe;
            }
        }
        return null;
    }
}
