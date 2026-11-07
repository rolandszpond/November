using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Right side of the inventory screen. Lists the unlocked recipes available in the current context (hand recipes,
// plus the station's own at a crafting table), craftable ones first, and crafts the selected one.
public class CraftingPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text title;
    [SerializeField] private RectTransform listContent;
    [SerializeField] private RecipeRowView rowPrefab;
    [SerializeField] private TMP_Text lockedHint;
    [Header("Details")]
    [SerializeField] private GameObject details;
    [SerializeField] private Image detailIcon;
    [SerializeField] private TMP_Text detailName;
    [SerializeField] private TMP_Text detailDescription;
    [SerializeField] private TMP_Text detailIngredients;
    [SerializeField] private Button craftButton;
    [SerializeField] private Button craftAllButton;
    [SerializeField] private TMP_Text craftAllLabel;

    private readonly List<RecipeRowView> rows = new List<RecipeRowView>();
    private readonly List<RecipeDefinition> visible = new List<RecipeDefinition>();
    private readonly StringBuilder sb = new StringBuilder();
    private PlayerInventory inventory;
    private InventoryContext context;
    private RecipeDefinition selected;
    private bool dirty;

    private void Awake()
    {
        craftButton.onClick.AddListener(() => Craft(1));
        craftAllButton.onClick.AddListener(() => Craft(int.MaxValue));
    }

    public void Show(PlayerInventory playerInventory, InventoryContext ctx)
    {
        Hide();
        inventory = playerInventory;
        context = ctx;
        title.text = ctx.kind == InventoryContext.Kind.Station ? ctx.title : "Crafting";
        inventory.Changed += MarkDirty;
        inventory.Discovery.RecipeUnlocked += OnRecipeUnlocked;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (inventory != null)
        {
            inventory.Changed -= MarkDirty;
            inventory.Discovery.RecipeUnlocked -= OnRecipeUnlocked;
        }
        inventory = null;
        gameObject.SetActive(false);
    }

    private void OnRecipeUnlocked(RecipeDefinition _) => MarkDirty();
    private void MarkDirty() => dirty = true;

    // Inventory changes come in bursts (one event per slot); rebuild once per frame at most.
    private void LateUpdate()
    {
        if (dirty) Refresh();
    }

    private bool IsCraftable(RecipeDefinition r) => inventory.CanCraft(r, context.station);

    private void Refresh()
    {
        dirty = false;
        if (inventory == null || inventory.Recipes == null) return;

        visible.Clear();
        int locked = 0;
        foreach (var recipe in inventory.Recipes.recipes)
        {
            if (recipe == null || recipe.output == null || !CraftingLogic.StationAllows(recipe, context.station)) continue;
            if (inventory.Discovery.IsUnlocked(recipe)) visible.Add(recipe);
            else locked++;
        }
        // Craftable first, then this station's recipes before hand recipes, then by name.
        visible.Sort((a, b) =>
        {
            int c = IsCraftable(b).CompareTo(IsCraftable(a));
            if (c != 0) return c;
            c = (b.station != CraftingStationType.None).CompareTo(a.station != CraftingStationType.None);
            return c != 0 ? c : string.CompareOrdinal(a.output.DisplayName, b.output.DisplayName);
        });

        while (rows.Count < visible.Count)
        {
            var row = Instantiate(rowPrefab, listContent);
            row.gameObject.SetActive(true);
            rows.Add(row);
        }
        for (int i = 0; i < rows.Count; i++)
        {
            bool used = i < visible.Count;
            rows[i].gameObject.SetActive(used);
            if (used) rows[i].Bind(visible[i], IsCraftable(visible[i]), visible[i] == selected, Select);
        }

        lockedHint.text = visible.Count == 0
            ? "Loot items to discover recipes."
            : locked > 0 ? $"{locked} more recipe{(locked == 1 ? "" : "s")} to discover" : "";

        if (selected == null || !visible.Contains(selected)) selected = visible.Count > 0 ? visible[0] : null;
        RefreshDetails();
    }

    private void Select(RecipeDefinition recipe)
    {
        selected = recipe;
        Refresh();
    }

    private void RefreshDetails()
    {
        details.SetActive(selected != null);
        if (selected == null) return;

        var output = selected.output;
        detailIcon.enabled = output.icon != null;
        detailIcon.sprite = output.icon;
        detailName.text = selected.outputCount > 1 ? $"{output.DisplayName} x{selected.outputCount}" : output.DisplayName;
        detailDescription.text = $"<color=#9fb3c8>{ItemTooltip.Categories(output.categories)}</color>  {output.weight:0.##} kg"
            + (string.IsNullOrEmpty(output.description) ? "" : "\n" + output.description);

        sb.Clear();
        foreach (var ingredient in selected.ingredients)
        {
            int have = inventory.Count(ingredient.item);
            string color = have >= ingredient.count ? "#b6e3a8" : "#ff7b7b";
            sb.AppendLine($"<color={color}>{have}/{ingredient.count}</color>  {ingredient.item.DisplayName}");
        }
        detailIngredients.text = sb.ToString().TrimEnd();

        bool craftable = IsCraftable(selected);
        int max = craftable ? inventory.MaxCraftable(selected) : 0;
        craftButton.interactable = craftable;
        craftAllButton.interactable = max > 1;
        craftAllLabel.text = max > 1 ? $"Craft x{max}" : "Craft all";
    }

    private void Craft(int times)
    {
        if (selected == null) return;
        int crafted = 0;
        while (crafted < times && inventory.Craft(selected, context.station)) crafted++;
        if (crafted > 0)
        {
            HudMessages.Show(crafted > 1
                ? $"Crafted {selected.output.DisplayName} x{crafted * selected.outputCount}"
                : $"Crafted {selected.output.DisplayName}");
        }
        Refresh();
    }
}
