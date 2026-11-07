using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Receives pointer gestures from slots. InventoryScreen implements it; HUD slots have no handler (display only).
public interface ISlotHandler
{
    void OnSlotBeginDrag(SlotView slot, PointerEventData eventData);
    void OnSlotDrag(SlotView slot, PointerEventData eventData);
    void OnSlotEndDrag(SlotView slot, PointerEventData eventData);
    void OnSlotDrop(SlotView target, PointerEventData eventData);
    void OnSlotClick(SlotView slot, PointerEventData eventData);
    void OnSlotHover(SlotView slot, bool entered);
}

// One inventory slot: shows a stack (icon, count) and forwards drag/drop/click to its handler.
// It holds no state of its own beyond which container slot it shows.
public class SlotView : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text countText;
    [Tooltip("Shown when the item has no icon yet (its initials).")]
    [SerializeField] private TMP_Text fallbackLabel;
    [SerializeField] private TMP_Text keyHint;
    [SerializeField] private GameObject selection;
    // Opaque: in linear colour space even 10% transparency lets bright scenery show through clearly.
    [SerializeField] private Color normalColor = new Color(0.12f, 0.13f, 0.16f, 1f);
    [SerializeField] private Color highlightColor = new Color(0.22f, 0.25f, 0.3f, 1f);

    private ISlotHandler handler;

    public InventoryContainer Container { get; private set; }
    public int Index { get; private set; }
    public ItemStack Stack => Container != null && Index < Container.Size ? Container[Index] : ItemStack.Empty;

    public void Bind(InventoryContainer container, int index, ISlotHandler slotHandler, string hint = null)
    {
        Container = container;
        Index = index;
        handler = slotHandler;
        if (keyHint != null)
        {
            keyHint.text = hint ?? "";
            keyHint.gameObject.SetActive(!string.IsNullOrEmpty(hint));
        }
        SetSelected(false);
        SetHighlighted(false);
        Refresh();
    }

    public void Refresh()
    {
        var stack = Stack;
        bool hasItem = !stack.IsEmpty;
        bool hasIcon = hasItem && stack.item.icon != null;

        icon.enabled = hasIcon;
        if (hasIcon) icon.sprite = stack.item.icon;

        if (fallbackLabel != null)
        {
            fallbackLabel.gameObject.SetActive(hasItem && !hasIcon);
            if (hasItem && !hasIcon) fallbackLabel.text = Initials(stack.item.DisplayName);
        }

        countText.text = hasItem && stack.count > 1 ? stack.count.ToString() : "";
    }

    public void SetSelected(bool selected)
    {
        if (selection != null) selection.SetActive(selected);
    }

    public void SetHighlighted(bool highlighted)
    {
        if (background != null) background.color = highlighted ? highlightColor : normalColor;
    }

    public void SetDimmed(bool dimmed)
    {
        var color = dimmed ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
        icon.color = color;
        countText.alpha = dimmed ? 0.35f : 1f;
    }

    public static string Initials(string name)
    {
        if (string.IsNullOrEmpty(name)) return "?";
        var parts = name.Split(' ');
        if (parts.Length == 1) return name.Length <= 3 ? name : name.Substring(0, 3);
        string result = "";
        foreach (var part in parts)
        {
            if (part.Length > 0 && result.Length < 3) result += char.ToUpperInvariant(part[0]);
        }
        return result;
    }

    public void OnBeginDrag(PointerEventData eventData) => handler?.OnSlotBeginDrag(this, eventData);
    public void OnDrag(PointerEventData eventData) => handler?.OnSlotDrag(this, eventData);
    public void OnEndDrag(PointerEventData eventData) => handler?.OnSlotEndDrag(this, eventData);
    public void OnDrop(PointerEventData eventData) => handler?.OnSlotDrop(this, eventData);
    public void OnPointerClick(PointerEventData eventData) => handler?.OnSlotClick(this, eventData);

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (handler == null) return;
        SetHighlighted(true);
        handler.OnSlotHover(this, true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (handler == null) return;
        SetHighlighted(false);
        handler.OnSlotHover(this, false);
    }
}
