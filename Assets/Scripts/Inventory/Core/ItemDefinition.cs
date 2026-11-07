using System;
using UnityEngine;

// Static description of one kind of item. Runtime state (counts) lives in ItemStack; this asset never changes in play.
// The id is what saves and recipes refer to, so keep it stable once content exists.
[CreateAssetMenu(menuName = "Inventory/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Serializable]
    public class ToolStats
    {
        public bool enabled;
        public ToolType toolType;
        [Tooltip("How much a single hit gathers from a resource node (trees, rocks).")]
        public float gatherPower = 1f;
    }

    [Serializable]
    public class WeaponStats
    {
        public bool enabled;
        public float damage = 10f;
        [Tooltip("Attacks per second.")]
        public float attackRate = 1f;
    }

    [Serializable]
    public class FoodStats
    {
        public bool enabled;
        public float nutrition = 10f;
    }

    [Serializable]
    public class BackpackStats
    {
        public bool enabled;
        [Tooltip("Slots the backpack section gets while this is equipped.")]
        public int slots = 8;
        [Tooltip("Added to the player's max carry weight while equipped.")]
        public float carryWeightBonus = 20f;
    }

    [Serializable]
    public class PlaceableStats
    {
        public bool enabled;
        [Tooltip("Spawned in the world when the item is placed from the hotbar (e.g. a crafting table).")]
        public GameObject placedPrefab;
    }

    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public ItemCategory categories;
    [Min(1)]
    [Tooltip("1 = not stackable.")]
    public int maxStack = 1;
    [Min(0f)]
    [Tooltip("Weight of one unit, in kg.")]
    public float weight = 0.1f;
    [Tooltip("Visual used when the item lies in the world, and by the icon baker.")]
    public GameObject worldPrefab;
    [Min(0.01f)]
    [Tooltip("Scale applied to worldPrefab when the item lies in the world (Synty props vary a lot in size).")]
    public float worldScale = 1f;

    public ToolStats tool = new ToolStats();
    public WeaponStats weapon = new WeaponStats();
    public FoodStats food = new FoodStats();
    public BackpackStats backpack = new BackpackStats();
    public PlaceableStats placeable = new PlaceableStats();

    public bool IsStackable => maxStack > 1;
    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
    public bool Has(ItemCategory category) => (categories & category) != 0;
}
