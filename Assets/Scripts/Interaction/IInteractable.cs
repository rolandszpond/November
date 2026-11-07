// Anything the player can use with the Interact key (E): world items, loot chests, crafting stations.
// Implemented by a component on (or a parent of) the collider the interaction ray hits.
public interface IInteractable
{
    bool CanInteract(PlayerInteractor interactor);
    // Short action text shown on the HUD, e.g. "Pick up Log x3".
    string GetPrompt(PlayerInteractor interactor);
    void Interact(PlayerInteractor interactor);
}
