using UnityEngine;

// A placed crafting table. Interacting opens the inventory with this station's recipes (plus hand recipes).
public class CraftingStation : MonoBehaviour, IInteractable
{
    [SerializeField] private CraftingStationType stationType = CraftingStationType.Workbench;
    [SerializeField] private string displayName = "Workbench";

    public CraftingStationType StationType => stationType;
    public string DisplayName => displayName;

    public bool CanInteract(PlayerInteractor interactor) => interactor.Game != null;

    public string GetPrompt(PlayerInteractor interactor) => $"Use {displayName}";

    public void Interact(PlayerInteractor interactor)
    {
        interactor.Game.OpenInventory(InventoryContext.ForStation(this));
    }
}
