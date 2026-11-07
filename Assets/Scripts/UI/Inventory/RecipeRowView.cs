using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// One entry in the crafting list.
public class RecipeRowView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image background;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text label;
    [SerializeField] private TMP_Text subLabel;
    [SerializeField] private Color normalColor = new Color(0.14f, 0.15f, 0.18f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.25f, 0.4f, 0.6f, 1f);

    public RecipeDefinition Recipe { get; private set; }

    public void Bind(RecipeDefinition recipe, bool craftable, bool selected, Action<RecipeDefinition> onClick)
    {
        Recipe = recipe;
        var output = recipe.output;
        icon.enabled = output.icon != null;
        icon.sprite = output.icon;
        label.text = recipe.outputCount > 1 ? $"{output.DisplayName} x{recipe.outputCount}" : output.DisplayName;
        subLabel.text = recipe.station == CraftingStationType.None ? "" : recipe.station.ToString();
        label.alpha = craftable ? 1f : 0.45f;
        icon.color = craftable ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        background.color = selected ? selectedColor : normalColor;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(recipe));
    }
}
