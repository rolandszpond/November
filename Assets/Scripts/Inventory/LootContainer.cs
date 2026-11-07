using System;
using System.Collections.Generic;
using UnityEngine;

// A chest/crate the player can open. The loot table is rolled once, the first time it's opened, and the items
// then stay in the container until taken. Looting is how most items are first discovered.
public class LootContainer : MonoBehaviour, IInteractable
{
    [Serializable]
    public class LootEntry
    {
        public ItemDefinition item;
        [Min(0)] public int min = 1;
        [Min(0)] public int max = 1;
        [Range(0f, 1f)] public float chance = 1f;
    }

    [SerializeField] private string displayName = "Chest";
    [SerializeField, Min(1)] private int slots = 16;
    [SerializeField] private List<LootEntry> loot = new List<LootEntry>();

    private bool rolled;

    public string DisplayName => displayName;
    public InventoryContainer Container { get; private set; }

    private void Awake()
    {
        Container = new InventoryContainer(slots);
    }

    private void Roll()
    {
        if (rolled) return;
        rolled = true;
        foreach (var entry in loot)
        {
            if (entry.item == null || UnityEngine.Random.value > entry.chance) continue;
            int amount = UnityEngine.Random.Range(entry.min, Mathf.Max(entry.min, entry.max) + 1);
            Container.TryAdd(entry.item, amount);
        }
    }

    public bool CanInteract(PlayerInteractor interactor) => interactor.Game != null;

    public string GetPrompt(PlayerInteractor interactor) =>
        rolled && Container.IsEmpty ? $"Open {displayName} (empty)" : $"Open {displayName}";

    public void Interact(PlayerInteractor interactor)
    {
        Roll();
        interactor.Game.OpenInventory(InventoryContext.ForLoot(this));
    }
}
