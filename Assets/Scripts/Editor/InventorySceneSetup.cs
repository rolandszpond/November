using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Wires the inventory into the open scene: player components, HUD + inventory screen, a few pickups and loot
// chests around the player. Re-running doesn't duplicate anything already there.
public static class InventorySceneSetup
{
    private const string GhostMaterialPath = "Assets/Building/BuildGhost.mat";

    [MenuItem("Tools/Inventory/4. Set Up Open Scene")]
    public static void Setup()
    {
        var player = Object.FindAnyObjectByType<PlayerController>();
        var game = Object.FindAnyObjectByType<GameModeController>();
        if (player == null || game == null)
        {
            Debug.LogError("Scene needs a PlayerController and a GameModeController.");
            return;
        }

        var itemDb = AssetDatabase.LoadAssetAtPath<ItemDatabase>(StarterContentBuilder.ItemDatabasePath);
        var recipeDb = AssetDatabase.LoadAssetAtPath<RecipeDatabase>(StarterContentBuilder.RecipeDatabasePath);

        // ---- Player components
        var inventory = GetOrAdd<PlayerInventory>(player.gameObject);
        Set(inventory, "itemDatabase", itemDb);
        Set(inventory, "recipeDatabase", recipeDb);
        var interactor = GetOrAdd<PlayerInteractor>(player.gameObject);
        Set(interactor, "inventory", inventory);
        Set(interactor, "game", game);
        var placer = GetOrAdd<ItemPlacer>(player.gameObject);
        Set(placer, "inventory", inventory);
        Set(placer, "ghostMaterial", AssetDatabase.LoadAssetAtPath<Material>(GhostMaterialPath));
        var hotbar = GetOrAdd<HotbarController>(player.gameObject);
        Set(hotbar, "inventory", inventory);
        Set(hotbar, "placer", placer);
        Set(player, "inventory", inventory);

        // ---- UI
        var hud = Object.FindAnyObjectByType<HotbarHUD>(FindObjectsInactive.Include)
                  ?? Instantiate(UIPrefabBuilder.HudPath).GetComponent<HotbarHUD>();
        Set(hud, "inventory", inventory);
        Set(hud, "interactor", interactor);
        Set(hud, "game", game);

        var screen = Object.FindAnyObjectByType<InventoryScreen>(FindObjectsInactive.Include)
                     ?? Instantiate(UIPrefabBuilder.ScreenPath).GetComponent<InventoryScreen>();
        Set(screen, "inventory", inventory);

        Set(game, "inventoryScreen", screen);
        var so = new SerializedObject(game);
        var behaviours = so.FindProperty("playModeBehaviours");
        var list = new List<Behaviour> { interactor, hotbar, placer };
        behaviours.arraySize = list.Count;
        for (int i = 0; i < list.Count; i++) behaviours.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        so.ApplyModifiedProperties();

        // ---- World content
        if (Object.FindAnyObjectByType<WorldItem>() == null && itemDb != null)
        {
            var parent = new GameObject("Pickups").transform;
            Undo.RegisterCreatedObjectUndo(parent.gameObject, "Pickups");
            Vector3 origin = player.transform.position;
            var spawns = new (string id, int count, float angle, float radius)[]
            {
                ("stick", 3, 20f, 3f), ("stick", 4, 70f, 4.5f), ("stick", 3, 200f, 5f),
                ("stone", 3, 110f, 3.5f), ("stone", 3, 250f, 4f), ("stone", 2, 320f, 5.5f),
                ("fiber", 5, 150f, 3f), ("fiber", 6, 290f, 4.5f), ("fiber", 4, 40f, 6f),
                ("log", 2, 180f, 6f), ("log", 2, 0f, 7f),
            };
            foreach (var (id, count, angle, radius) in spawns)
            {
                var item = itemDb.Find(id);
                if (item == null) continue;
                Vector3 pos = Ground(origin + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * radius) + Vector3.up * 0.3f;
                CreateWorldItem(item, count, pos, parent);
            }
        }

        var chestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StarterContentBuilder.LootChestPrefabPath);
        if (Object.FindAnyObjectByType<LootContainer>() == null && chestPrefab != null)
        {
            var parent = new GameObject("LootChests").transform;
            Undo.RegisterCreatedObjectUndo(parent.gameObject, "LootChests");
            Vector3 origin = player.transform.position;
            foreach (float angle in new[] { 60f, 180f, 300f })
            {
                Vector3 pos = Ground(origin + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * 9f);
                var chest = (GameObject)PrefabUtility.InstantiatePrefab(chestPrefab, parent);
                chest.transform.position = pos;
                chest.transform.rotation = Quaternion.LookRotation(origin - pos, Vector3.up);
            }
        }

        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        Debug.Log("Inventory scene setup done. Save the scene to keep it.");
    }

    private static void CreateWorldItem(ItemDefinition item, int count, Vector3 position, Transform parent)
    {
        var go = new GameObject($"WorldItem_{item.id}");
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        go.AddComponent<Rigidbody>();
        var worldItem = go.AddComponent<WorldItem>();
        var so = new SerializedObject(worldItem);
        so.FindProperty("item").objectReferenceValue = item;
        so.FindProperty("count").intValue = count;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Visual child so it shows in the editor; WorldItem only builds one at runtime when there's none.
        if (item.worldPrefab != null)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(item.worldPrefab, go.transform);
            visual.name = "Visual";
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = item.worldPrefab.transform.localScale * item.worldScale;
        }
        Undo.RegisterCreatedObjectUndo(go, "World item");
    }

    private static Vector3 Ground(Vector3 position)
    {
        if (Physics.Raycast(position + Vector3.up * 20f, Vector3.down, out var hit, 60f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point;
        }
        return position;
    }

    private static GameObject Instantiate(string prefabPath)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(instance, "Inventory UI");
        return instance;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(go);
    }

    private static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError($"{target.GetType().Name} has no field '{field}'");
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }
}
