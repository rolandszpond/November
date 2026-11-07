using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The inventory menu (Tab, a crafting station, or a loot chest). Left: backpack section + hotbar + weight.
// Right: crafting panel, or the chest's contents when looting.
// Slot gestures:
//   left drag          move the whole stack (merge onto the same item, swap with a different one)
//   right drag         carry half the stack (split)
//   release outside    drop the carried amount into the world
//   shift + click      quick-move between hotbar / backpack / open chest
//   right click        context menu (equip, split, recycle, drop, eat)
public class InventoryScreen : MonoBehaviour, ISlotHandler
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private GameObject root;

    [Header("Player")]
    [SerializeField] private ContainerGridView hotbarGrid;
    [SerializeField] private ContainerGridView backpackGrid;
    [SerializeField] private ContainerGridView backpackEquipGrid;
    [SerializeField] private GameObject backpackEmptyHint;
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private Image weightFill;

    [Header("Right panel")]
    [SerializeField] private CraftingPanel craftingPanel;
    [SerializeField] private GameObject lootPanel;
    [SerializeField] private TMP_Text lootTitle;
    [SerializeField] private ContainerGridView lootGrid;
    [SerializeField] private Button takeAllButton;

    [Header("Overlays")]
    [SerializeField] private RectTransform dragGhost;
    [SerializeField] private Image dragGhostIcon;
    [SerializeField] private TMP_Text dragGhostCount;
    [SerializeField] private ContextMenuView contextMenu;
    [SerializeField] private ItemTooltip tooltip;

    private InventoryContext context;
    private SlotView dragSource;
    private int dragAmount;
    private bool dropHandled;

    public bool IsOpen => root.activeSelf;

    private void Awake()
    {
        if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();
        takeAllButton.onClick.AddListener(TakeAll);
        root.SetActive(false);
        UIUtil.EnsureEventSystem();
    }

    public void Open(InventoryContext ctx)
    {
        context = ctx;
        root.SetActive(true);
        dragGhost.gameObject.SetActive(false);
        contextMenu.Hide();
        tooltip.Hide();

        hotbarGrid.Bind(inventory.Hotbar, this);
        backpackGrid.Bind(inventory.Backpack, this);
        backpackEquipGrid.Bind(inventory.BackpackEquip, this);
        inventory.Changed += RefreshPlayerInfo;
        RefreshPlayerInfo();

        bool looting = ctx.kind == InventoryContext.Kind.Loot && ctx.loot != null;
        lootPanel.SetActive(looting);
        if (looting)
        {
            lootTitle.text = ctx.title;
            lootGrid.Bind(ctx.loot.Container, this);
            craftingPanel.Hide();
        }
        else
        {
            lootGrid.Unbind();
            craftingPanel.Show(inventory, ctx);
        }
    }

    public void Close()
    {
        CancelDrag();
        contextMenu.Hide();
        tooltip.Hide();
        inventory.Changed -= RefreshPlayerInfo;
        hotbarGrid.Unbind();
        backpackGrid.Unbind();
        backpackEquipGrid.Unbind();
        lootGrid.Unbind();
        craftingPanel.Hide();
        root.SetActive(false);
        context = null;
    }

    private void RefreshPlayerInfo()
    {
        backpackEmptyHint.SetActive(inventory.EquippedBackpack.IsEmpty);
        float weight = inventory.TotalWeight;
        float max = inventory.MaxCarryWeight;
        bool over = inventory.IsOverencumbered;
        weightText.text = $"{(over ? "<color=#ff7b7b>" : "")}{weight:0.#} / {max:0} kg{(over ? "  Overencumbered</color>" : "")}";
        weightFill.fillAmount = max > 0f ? Mathf.Clamp01(weight / max) : 1f;
        weightFill.color = over ? new Color(0.9f, 0.35f, 0.3f) : new Color(0.45f, 0.7f, 0.45f);
    }

    private CraftingStationType Station => context != null ? context.station : CraftingStationType.None;
    private InventoryContainer LootContainer => context != null && context.kind == InventoryContext.Kind.Loot ? context.loot?.Container : null;

    // ---------------------------------------------------------------- Drag & drop

    public void OnSlotBeginDrag(SlotView slot, PointerEventData eventData)
    {
        var stack = slot.Stack;
        if (stack.IsEmpty || (eventData.button != PointerEventData.InputButton.Left && eventData.button != PointerEventData.InputButton.Right))
        {
            return;
        }

        contextMenu.Hide();
        tooltip.Hide();
        dragSource = slot;
        dropHandled = false;
        dragAmount = eventData.button == PointerEventData.InputButton.Right ? (stack.count + 1) / 2 : stack.count;

        dragGhost.gameObject.SetActive(true);
        dragGhost.SetAsLastSibling();
        dragGhostIcon.enabled = stack.item.icon != null;
        dragGhostIcon.sprite = stack.item.icon;
        dragGhostCount.text = stack.item.icon == null
            ? SlotView.Initials(stack.item.DisplayName) + (dragAmount > 1 ? $" {dragAmount}" : "")
            : dragAmount > 1 ? dragAmount.ToString() : "";
        slot.SetDimmed(true);
        MoveGhost(eventData.position);
    }

    public void OnSlotDrag(SlotView slot, PointerEventData eventData)
    {
        if (dragSource != null) MoveGhost(eventData.position);
    }

    public void OnSlotDrop(SlotView target, PointerEventData eventData)
    {
        if (dragSource == null) return;
        dropHandled = true;
        MoveStack(dragSource, target, dragAmount);
    }

    public void OnSlotEndDrag(SlotView slot, PointerEventData eventData)
    {
        if (dragSource == null) return;

        // Released over nothing (no slot, no panel) = drop into the world.
        if (!dropHandled && eventData.pointerCurrentRaycast.gameObject == null)
        {
            if (dragSource.Container == inventory.BackpackEquip && !inventory.CanUnequipBackpack)
            {
                HudMessages.Show("Empty your backpack first");
            }
            else
            {
                inventory.DropFromSlot(dragSource.Container, dragSource.Index, dragAmount);
            }
        }
        CancelDrag();
    }

    private void CancelDrag()
    {
        if (dragSource != null) dragSource.SetDimmed(false);
        dragSource = null;
        dragGhost.gameObject.SetActive(false);
    }

    private void MoveGhost(Vector2 screenPosition)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)dragGhost.parent, screenPosition, null, out var local);
        dragGhost.anchoredPosition = local;
    }

    private void MoveStack(SlotView from, SlotView to, int amount)
    {
        if (from == to) return;
        var equip = inventory.BackpackEquip;

        if (to.Container == equip)
        {
            if (!inventory.TryEquipBackpack(from.Container, from.Index))
            {
                HudMessages.Show(from.Stack.item.backpack.enabled ? "Backpack contents won't fit" : "Only backpacks go there");
            }
            return;
        }

        if (from.Container == equip)
        {
            if (!inventory.TryUnequipBackpack(to.Container, to.Index))
            {
                HudMessages.Show(inventory.Backpack.IsEmpty ? "Can't put the backpack there" : "Empty your backpack first");
            }
            return;
        }

        InventoryContainer.MoveOrMerge(from.Container, from.Index, to.Container, to.Index, amount);
    }

    // ---------------------------------------------------------------- Clicks

    public void OnSlotClick(SlotView slot, PointerEventData eventData)
    {
        if (slot.Stack.IsEmpty) return;
        var keyboard = Keyboard.current;
        bool shift = keyboard != null && keyboard.shiftKey.isPressed;

        if (eventData.button == PointerEventData.InputButton.Left && shift)
        {
            QuickMove(slot);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            tooltip.Hide();
            contextMenu.Show(eventData.position, BuildMenu(slot));
        }
    }

    // Shift+click: chest -> player, player -> open chest, otherwise hotbar <-> backpack.
    private void QuickMove(SlotView slot)
    {
        var from = slot.Container;
        var stack = slot.Stack;
        var loot = LootContainer;

        if (from == inventory.BackpackEquip)
        {
            if (!inventory.TryUnequipBackpack(inventory.Hotbar, -1)) HudMessages.Show("Empty your backpack first");
            return;
        }

        int leftover;
        if (from == loot) leftover = inventory.Add(stack.item, stack.count);
        else if (loot != null) leftover = loot.TryAdd(stack.item, stack.count);
        else if (from == inventory.Hotbar) leftover = inventory.Backpack.TryAdd(stack.item, stack.count);
        else leftover = inventory.Hotbar.TryAdd(stack.item, stack.count);

        from.Set(slot.Index, stack.WithCount(leftover));
    }

    private void TakeAll()
    {
        var loot = LootContainer;
        if (loot == null) return;
        bool full = false;
        for (int i = 0; i < loot.Size; i++)
        {
            var stack = loot[i];
            if (stack.IsEmpty) continue;
            int leftover = inventory.Add(stack.item, stack.count);
            if (leftover > 0) full = true;
            loot.Set(i, stack.WithCount(leftover));
        }
        if (full) HudMessages.Show("Inventory full");
    }

    private List<(string, Action)> BuildMenu(SlotView slot)
    {
        var menu = new List<(string, Action)>();
        var container = slot.Container;
        int index = slot.Index;
        var stack = slot.Stack;
        var item = stack.item;

        if (container == inventory.BackpackEquip)
        {
            menu.Add(("Unequip", () =>
            {
                if (!inventory.TryUnequipBackpack(inventory.Hotbar, -1)) HudMessages.Show("Empty your backpack first");
            }));
            return menu;
        }

        if (item.backpack.enabled && container != LootContainer)
        {
            menu.Add(("Equip", () =>
            {
                if (!inventory.TryEquipBackpack(container, index)) HudMessages.Show("Backpack contents won't fit");
            }));
        }

        if (item.food.enabled && container != LootContainer)
        {
            menu.Add(("Eat", () =>
            {
                container.TakeFrom(index, 1);
                HudMessages.Show($"Ate {item.DisplayName}");
            }));
        }

        if (stack.count > 1)
        {
            menu.Add(("Split", () => SplitIntoFreeSlot(container, index)));
        }

        if (container != LootContainer && inventory.CanRecycle(stack, Station))
        {
            var yield = CraftingLogic.RecycleYield(inventory.RecipeFor(item), stack.count);
            var parts = new List<string>();
            foreach (var y in yield) parts.Add($"{y.count} {y.item.DisplayName}");
            menu.Add(($"Recycle  ({string.Join(", ", parts)})", () => inventory.Recycle(container, index, Station)));
        }

        if (stack.count > 1) menu.Add(("Drop one", () => inventory.DropFromSlot(container, index, 1)));
        menu.Add((stack.count > 1 ? "Drop all" : "Drop", () => inventory.DropFromSlot(container, index, stack.count)));
        return menu;
    }

    private void SplitIntoFreeSlot(InventoryContainer container, int index)
    {
        for (int i = 0; i < container.Size; i++)
        {
            if (container[i].IsEmpty)
            {
                container.Split(index, i);
                return;
            }
        }
        HudMessages.Show("No free slot to split into");
    }

    // ---------------------------------------------------------------- Hover

    public void OnSlotHover(SlotView slot, bool entered)
    {
        if (!entered || dragSource != null || contextMenu.IsOpen || slot.Stack.IsEmpty)
        {
            tooltip.Hide();
            return;
        }

        string extra = null;
        var recipe = inventory.RecipeFor(slot.Stack.item);
        if (recipe != null && recipe.recyclable && slot.Container != LootContainer)
        {
            extra = inventory.CanRecycle(slot.Stack, Station)
                ? "<color=#9fb3c8>Right click to recycle</color>"
                : recipe.station != CraftingStationType.None && !CraftingLogic.StationAllows(recipe, Station)
                    ? $"<color=#9fb3c8>Recycle at a {recipe.station}</color>"
                    : null;
        }
        tooltip.Show(slot.Stack, extra);
    }
}
