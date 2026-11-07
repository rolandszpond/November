using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Creates (or updates in place) the starter items, recipes, databases and world prefabs (workbench, loot chest).
// Safe to re-run: assets are found by path and overwritten field by field, so references to them survive.
public static class StarterContentBuilder
{
    public const string ItemsFolder = "Assets/Items";
    public const string RecipesFolder = "Assets/Recipes";
    public const string WorldPrefabsFolder = "Assets/Prefabs/World";
    public const string ItemDatabasePath = ItemsFolder + "/ItemDatabase.asset";
    public const string RecipeDatabasePath = RecipesFolder + "/RecipeDatabase.asset";
    public const string WorkbenchPrefabPath = WorldPrefabsFolder + "/Workbench.prefab";
    public const string LootChestPrefabPath = WorldPrefabsFolder + "/LootChest.prefab";

    private const string Generic = "Assets/Synty/PolygonGeneric/Prefabs/";
    private const string Prototype = "Assets/Synty/PolygonPrototype/Prefabs/";

    [MenuItem("Tools/Inventory/1. Create Starter Content")]
    public static void Build()
    {
        EnsureFolder(ItemsFolder);
        EnsureFolder(RecipesFolder);
        EnsureFolder(WorldPrefabsFolder);

        var stick = Item("stick", "Stick", ItemCategory.Resource, 20, 0.1f, Generic + "Environment/SM_Gen_Env_Log_02.prefab", 0.6f,
            "A straight branch. Used in most basic recipes.");
        var stone = Item("stone", "Stone", ItemCategory.Resource, 20, 0.5f, Generic + "Environment/SM_Gen_Env_Rock_Pebbles_01.prefab", 0.3f,
            "A fist-sized stone.");
        var fiber = Item("fiber", "Plant Fiber", ItemCategory.Resource, 50, 0.05f, Generic + "Environment/SM_Gen_Env_Bush_01.prefab", 0.35f,
            "Tough fibres pulled from plants. Can be woven into rope and cloth.");
        var log = Item("log", "Log", ItemCategory.Resource | ItemCategory.Building, 10, 2f, Generic + "Environment/SM_Gen_Env_Log_01.prefab", 1f,
            "A heavy log. Cut it into planks at a workbench.");
        var plank = Item("plank", "Plank", ItemCategory.Building, 20, 0.5f, Generic + "Props/SM_Gen_Prop_Plank_01.prefab", 1f,
            "A rough wooden plank.");
        var vegetable = Item("vegetable", "Vegetable", ItemCategory.Food, 10, 0.2f, Generic + "Props/SM_Gen_Prop_Food_Vegetable_01.prefab", 0.25f,
            "Edible.");
        vegetable.food.enabled = true;
        vegetable.food.nutrition = 15f;

        var axe = Item("stone_axe", "Stone Axe", ItemCategory.Tool | ItemCategory.Weapon, 1, 3f, Generic + "Weapons/SM_Gen_Wep_Axe_01.prefab", 0.7f,
            "Chops trees. Works as a weapon in a pinch.");
        axe.tool.enabled = true;
        axe.tool.toolType = ToolType.Axe;
        axe.tool.gatherPower = 1f;
        axe.weapon.enabled = true;
        axe.weapon.damage = 15f;
        axe.weapon.attackRate = 1.2f;

        var pickaxe = Item("stone_pickaxe", "Stone Pickaxe", ItemCategory.Tool | ItemCategory.Weapon, 1, 3f, Generic + "Weapons/SM_Gen_Wep_Pickaxe_01.prefab", 0.8f,
            "Breaks rocks and ore.");
        pickaxe.tool.enabled = true;
        pickaxe.tool.toolType = ToolType.Pickaxe;
        pickaxe.tool.gatherPower = 1f;
        pickaxe.weapon.enabled = true;
        pickaxe.weapon.damage = 12f;
        pickaxe.weapon.attackRate = 1f;

        var simpleBackpack = Item("simple_backpack", "Simple Backpack", ItemCategory.Backpack, 1, 1f, Prototype + "Icons/SM_Icon_CoinBag_01.prefab", 0.5f,
            "A woven bag. Adds a row of backpack slots.");
        simpleBackpack.backpack.enabled = true;
        simpleBackpack.backpack.slots = 8;
        simpleBackpack.backpack.carryWeightBonus = 20f;

        var largeBackpack = Item("large_backpack", "Large Backpack", ItemCategory.Backpack, 1, 2f, Prototype + "Icons/SM_Icon_CoinBag_01.prefab", 0.7f,
            "A framed pack. Two rows of backpack slots.");
        largeBackpack.backpack.enabled = true;
        largeBackpack.backpack.slots = 16;
        largeBackpack.backpack.carryWeightBonus = 40f;

        var workbenchPrefab = BuildWorkbenchPrefab();
        var workbench = Item("workbench", "Workbench", ItemCategory.Station | ItemCategory.Building, 1, 10f, Generic + "Props/SM_Gen_Prop_Table_01.prefab", 0.6f,
            "Place it, then use it to craft planks, tools and bigger backpacks.");
        workbench.placeable.enabled = true;
        workbench.placeable.placedPrefab = workbenchPrefab;

        var items = new List<ItemDefinition> { stick, stone, fiber, log, plank, vegetable, axe, pickaxe, simpleBackpack, largeBackpack, workbench };
        foreach (var item in items) EditorUtility.SetDirty(item);

        var recipes = new List<RecipeDefinition>
        {
            Recipe("stone_axe", axe, 1, CraftingStationType.None, true, (stick, 2), (stone, 2), (fiber, 3)),
            Recipe("simple_backpack", simpleBackpack, 1, CraftingStationType.None, true, (fiber, 10), (stick, 4)),
            Recipe("workbench", workbench, 1, CraftingStationType.None, true, (log, 4), (stone, 4), (stick, 6)),
            Recipe("plank", plank, 4, CraftingStationType.Workbench, false, (log, 1)),
            Recipe("stone_pickaxe", pickaxe, 1, CraftingStationType.Workbench, true, (stick, 3), (stone, 4), (fiber, 3)),
            Recipe("large_backpack", largeBackpack, 1, CraftingStationType.Workbench, true, (plank, 6), (fiber, 20), (stick, 4)),
        };

        var itemDb = LoadOrCreate<ItemDatabase>(ItemDatabasePath);
        itemDb.items = items;
        EditorUtility.SetDirty(itemDb);

        var recipeDb = LoadOrCreate<RecipeDatabase>(RecipeDatabasePath);
        recipeDb.recipes = recipes;
        EditorUtility.SetDirty(recipeDb);

        BuildLootChestPrefab(new[]
        {
            (log, 2, 4, 1f), (fiber, 5, 10, 0.9f), (stone, 2, 5, 0.8f), (stick, 3, 6, 0.8f), (vegetable, 1, 3, 0.6f),
        });

        AssetDatabase.SaveAssets();
        Debug.Log($"Starter content: {items.Count} items, {recipes.Count} recipes.");
    }

    // ---------------------------------------------------------------- Items / recipes

    private static ItemDefinition Item(string id, string displayName, ItemCategory categories, int maxStack, float weight,
        string prefabPath, float worldSize, string description)
    {
        var item = LoadOrCreate<ItemDefinition>($"{ItemsFolder}/{id}.asset");
        item.id = id;
        item.displayName = displayName;
        item.description = description;
        item.categories = categories;
        item.maxStack = maxStack;
        item.weight = weight;
        item.worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (item.worldPrefab == null) Debug.LogWarning($"{id}: prefab not found at {prefabPath}");
        item.worldScale = ScaleForSize(item.worldPrefab, worldSize);
        // Reset trait blocks; the caller enables what applies.
        item.tool = new ItemDefinition.ToolStats();
        item.weapon = new ItemDefinition.WeaponStats();
        item.food = new ItemDefinition.FoodStats();
        item.backpack = new ItemDefinition.BackpackStats();
        item.placeable = new ItemDefinition.PlaceableStats();
        return item;
    }

    private static RecipeDefinition Recipe(string id, ItemDefinition output, int outputCount, CraftingStationType station,
        bool recyclable, params (ItemDefinition item, int count)[] ingredients)
    {
        var recipe = LoadOrCreate<RecipeDefinition>($"{RecipesFolder}/{id}.asset");
        recipe.id = id;
        recipe.output = output;
        recipe.outputCount = outputCount;
        recipe.station = station;
        recipe.recyclable = recyclable;
        recipe.recycleReturn = 0.5f;
        recipe.ingredients = new List<ItemAmount>();
        foreach (var (item, count) in ingredients) recipe.ingredients.Add(new ItemAmount(item, count));
        EditorUtility.SetDirty(recipe);
        return recipe;
    }

    // Uniform scale that makes the prefab's largest dimension `size` metres.
    private static float ScaleForSize(GameObject prefab, float size)
    {
        if (prefab == null) return 1f;
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            var bounds = RendererBounds(instance);
            float max = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            return max > 0.0001f ? Mathf.Round(size / max * 1000f) / 1000f : 1f;
        }
        finally
        {
            Object.DestroyImmediate(instance);
        }
    }

    public static Bounds RendererBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.one * 0.5f);
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        return bounds;
    }

    // ---------------------------------------------------------------- World prefabs

    private static GameObject BuildWorkbenchPrefab()
    {
        var root = new GameObject("Workbench");
        try
        {
            var table = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Generic + "Props/SM_Gen_Prop_Table_01.prefab"));
            table.transform.SetParent(root.transform, false);
            FitBoxCollider(root);
            var station = root.AddComponent<CraftingStation>();
            var so = new SerializedObject(station);
            so.FindProperty("stationType").enumValueIndex = (int)CraftingStationType.Workbench;
            so.FindProperty("displayName").stringValue = "Workbench";
            so.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root, WorkbenchPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void BuildLootChestPrefab((ItemDefinition item, int min, int max, float chance)[] loot)
    {
        var root = new GameObject("LootChest");
        try
        {
            var chest = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Generic + "Props/SM_Gen_Prop_Chest_01.prefab"));
            chest.transform.SetParent(root.transform, false);
            FitBoxCollider(root);
            var container = root.AddComponent<LootContainer>();
            var so = new SerializedObject(container);
            so.FindProperty("displayName").stringValue = "Chest";
            so.FindProperty("slots").intValue = 16;
            var list = so.FindProperty("loot");
            list.arraySize = loot.Length;
            for (int i = 0; i < loot.Length; i++)
            {
                var entry = list.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("item").objectReferenceValue = loot[i].item;
                entry.FindPropertyRelative("min").intValue = loot[i].min;
                entry.FindPropertyRelative("max").intValue = loot[i].max;
                entry.FindPropertyRelative("chance").floatValue = loot[i].chance;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, LootChestPrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // A box on the root around all renderers, so the interaction ray and physics always have something to hit.
    private static void FitBoxCollider(GameObject root)
    {
        var bounds = RendererBounds(root);
        var box = root.AddComponent<BoxCollider>();
        box.center = root.transform.InverseTransformPoint(bounds.center);
        box.size = bounds.size;
    }

    // ---------------------------------------------------------------- Helpers

    public static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;
        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
