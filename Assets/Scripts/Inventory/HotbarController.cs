using UnityEngine;
using UnityEngine.InputSystem;

// Play-mode hotbar input: 1-8 / scroll wheel select a slot, left click uses the selected item.
// Only enabled in Play mode (GameModeController toggles it), so Build mode's 1-4 tool keys don't clash.
// Uses so far: placeable items start placement; food is eaten. Tools/weapons are just held (combat comes later).
public class HotbarController : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private ItemPlacer placer;

    private static readonly Key[] SlotKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8,
    };

    private void Awake()
    {
        if (inventory == null) inventory = GetComponent<PlayerInventory>();
        if (placer == null) placer = GetComponent<ItemPlacer>();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            for (int i = 0; i < SlotKeys.Length && i < inventory.Hotbar.Size; i++)
            {
                if (keyboard[SlotKeys[i]].wasPressedThisFrame) inventory.Select(i);
            }
        }

        var mouse = Mouse.current;
        if (mouse == null) return;

        float scroll = mouse.scroll.ReadValue().y;
        if (scroll > 0.01f) inventory.Select(inventory.SelectedIndex - 1);
        else if (scroll < -0.01f) inventory.Select(inventory.SelectedIndex + 1);

        if (placer != null && placer.IsPlacing) return;
        if (mouse.leftButton.wasPressedThisFrame && !UIUtil.IsPointerOverUI()) UseSelected();
    }

    private void UseSelected()
    {
        var stack = inventory.SelectedStack;
        if (stack.IsEmpty) return;

        if (stack.item.placeable.enabled && stack.item.placeable.placedPrefab != null && placer != null)
        {
            placer.Begin(stack.item);
        }
        else if (stack.item.food.enabled)
        {
            inventory.Hotbar.TakeFrom(inventory.SelectedIndex, 1);
            HudMessages.Show($"Ate {stack.item.DisplayName}");
        }
    }
}
