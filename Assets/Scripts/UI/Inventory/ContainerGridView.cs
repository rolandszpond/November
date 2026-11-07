using System.Collections.Generic;
using UnityEngine;

// Shows every slot of one InventoryContainer, using a GridLayoutGroup on `content` for layout.
// Rebuilds when the container is resized (backpack equipped/unequipped).
public class ContainerGridView : MonoBehaviour
{
    [SerializeField] private SlotView slotPrefab;
    [SerializeField] private RectTransform content;
    [Tooltip("Show 1..9 key hints on the slots (hotbar).")]
    [SerializeField] private bool showKeyHints;

    private readonly List<SlotView> slots = new List<SlotView>();
    private InventoryContainer container;
    private ISlotHandler handler;

    public InventoryContainer Container => container;
    public IReadOnlyList<SlotView> Slots => slots;

    public void Bind(InventoryContainer newContainer, ISlotHandler slotHandler)
    {
        Unbind();
        container = newContainer;
        handler = slotHandler;
        if (container != null) container.SlotChanged += OnSlotChanged;
        Rebuild();
    }

    public void Unbind()
    {
        if (container != null) container.SlotChanged -= OnSlotChanged;
        container = null;
    }

    private void OnDestroy() => Unbind();

    private void OnSlotChanged(int index)
    {
        if (index < 0 || index >= slots.Count || slots.Count != container.Size)
        {
            Rebuild();
            return;
        }
        slots[index].Refresh();
    }

    private void Rebuild()
    {
        var parent = content != null ? content : (RectTransform)transform;
        int size = container != null ? container.Size : 0;

        while (slots.Count > size)
        {
            Destroy(slots[slots.Count - 1].gameObject);
            slots.RemoveAt(slots.Count - 1);
        }
        while (slots.Count < size)
        {
            var slot = Instantiate(slotPrefab, parent);
            slot.gameObject.SetActive(true);
            slots.Add(slot);
        }
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].name = $"Slot {i}";
            slots[i].Bind(container, i, handler, showKeyHints ? (i + 1).ToString() : null);
        }
    }

    public void SetSelected(int index)
    {
        for (int i = 0; i < slots.Count; i++) slots[i].SetSelected(i == index);
    }

    public void RefreshAll()
    {
        foreach (var slot in slots) slot.Refresh();
    }
}
