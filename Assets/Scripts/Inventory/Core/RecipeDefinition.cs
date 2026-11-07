using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct ItemAmount
{
    public ItemDefinition item;
    [Min(1)] public int count;

    public ItemAmount(ItemDefinition item, int count)
    {
        this.item = item;
        this.count = count;
    }
}

// One crafting recipe. It is unlocked once the player has discovered every ingredient, and can only be crafted at
// its station (hand recipes, station None, are available everywhere).
[CreateAssetMenu(menuName = "Inventory/Recipe Definition")]
public class RecipeDefinition : ScriptableObject
{
    public string id;
    public ItemDefinition output;
    [Min(1)] public int outputCount = 1;
    public List<ItemAmount> ingredients = new List<ItemAmount>();
    public CraftingStationType station = CraftingStationType.None;

    [Header("Recycling")]
    public bool recyclable = true;
    [Range(0f, 1f)]
    [Tooltip("Fraction of each ingredient given back when the output is recycled.")]
    public float recycleReturn = 0.5f;
    [Tooltip("If not empty, recycling one output gives exactly this instead of a fraction of the ingredients.")]
    public List<ItemAmount> recycleOverride = new List<ItemAmount>();
}
