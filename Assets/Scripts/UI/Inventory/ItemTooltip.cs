using System.Text;
using TMPro;
using UnityEngine;

// Hover info for a stack: name, uses (categories), weight, stack size and any tool/weapon/food/backpack stats.
public class ItemTooltip : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text title;
    [SerializeField] private TMP_Text body;
    [SerializeField] private Vector2 offset = new Vector2(18f, -18f);

    private readonly StringBuilder sb = new StringBuilder();

    public void Show(ItemStack stack, string extraLine = null)
    {
        if (stack.IsEmpty)
        {
            Hide();
            return;
        }

        var item = stack.item;
        title.text = item.DisplayName;

        sb.Clear();
        sb.AppendLine($"<color=#9fb3c8>{Categories(item.categories)}</color>");
        sb.AppendLine(item.IsStackable
            ? $"Stack {stack.count}/{item.maxStack}   Weight {item.weight:0.##} kg each ({stack.Weight:0.##} kg)"
            : $"Weight {item.weight:0.##} kg   Not stackable");
        if (item.tool.enabled) sb.AppendLine($"{item.tool.toolType}: gather power {item.tool.gatherPower:0.#}");
        if (item.weapon.enabled) sb.AppendLine($"Weapon: {item.weapon.damage:0.#} damage, {item.weapon.attackRate:0.#}/s");
        if (item.food.enabled) sb.AppendLine($"Food: {item.food.nutrition:0} nutrition");
        if (item.backpack.enabled) sb.AppendLine($"Backpack: {item.backpack.slots} slots, +{item.backpack.carryWeightBonus:0} kg carry");
        if (item.placeable.enabled) sb.AppendLine("Place from the hotbar (left click)");
        if (!string.IsNullOrEmpty(item.description)) sb.AppendLine().AppendLine(item.description);
        if (!string.IsNullOrEmpty(extraLine)) sb.AppendLine().Append(extraLine);
        body.text = sb.ToString().TrimEnd();

        gameObject.SetActive(true);
        panel.SetAsLastSibling();
        Follow(UnityEngine.InputSystem.Mouse.current != null
            ? UnityEngine.InputSystem.Mouse.current.position.ReadValue()
            : Vector2.zero);
    }

    public void Hide() => gameObject.SetActive(false);

    public void Follow(Vector2 screenPosition)
    {
        if (!gameObject.activeSelf) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)panel.parent, screenPosition, null, out var local);
        panel.anchoredPosition = local + offset;
    }

    private void Update()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null) Follow(mouse.position.ReadValue());
    }

    public static string Categories(ItemCategory categories)
    {
        if (categories == ItemCategory.None) return "Misc";
        return categories.ToString().Replace(", ", " / ");
    }
}
