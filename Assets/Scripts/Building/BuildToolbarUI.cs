using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Bottom-of-screen build toolbar, generated in code from the tool list and catalog:
//   hint line / tool buttons / item buttons for the current tool.
// Placeholder visuals; swap for a designed UI later without touching the tools.
public class BuildToolbarUI : MonoBehaviour
{
    private static readonly Color PanelColor = new Color(0.08f, 0.09f, 0.11f, 0.85f);
    private static readonly Color ButtonColor = new Color(0.22f, 0.24f, 0.28f, 1f);
    private static readonly Color SelectedColor = new Color(0.25f, 0.55f, 0.95f, 1f);

    private BuildController build;
    private GameObject canvasRoot;
    private Font font;
    private Text hintText;
    private RectTransform toolRow;
    private RectTransform itemRow;
    private readonly List<(Image image, IBuildTool tool)> toolButtons = new List<(Image, IBuildTool)>();
    private readonly List<Image> itemButtons = new List<Image>();
    private IBuildTool itemsShownFor;

    public void Show(BuildController controller)
    {
        if (build != controller)
        {
            if (build != null) build.ToolChanged -= Refresh;
            build = controller;
            build.ToolChanged += Refresh;
        }

        EnsureBuilt();
        canvasRoot.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        if (canvasRoot != null)
        {
            canvasRoot.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (build != null) build.ToolChanged -= Refresh;
    }

    private void Refresh()
    {
        if (canvasRoot == null || build == null)
        {
            return;
        }

        IBuildTool current = build.CurrentTool;
        hintText.text = current.Hint;

        foreach ((Image image, IBuildTool tool) in toolButtons)
        {
            image.color = tool == current ? SelectedColor : ButtonColor;
        }

        if (itemsShownFor != current)
        {
            RebuildItems(current);
        }

        for (int i = 0; i < itemButtons.Count; i++)
        {
            itemButtons[i].color = i == current.SelectedIndex ? SelectedColor : ButtonColor;
        }
    }

    private void RebuildItems(IBuildTool tool)
    {
        itemsShownFor = tool;
        foreach (Transform child in itemRow)
        {
            Destroy(child.gameObject);
        }
        itemButtons.Clear();

        for (int i = 0; i < tool.Items.Count; i++)
        {
            int index = i;
            itemButtons.Add(CreateButton(itemRow, tool.Items[i].displayName, 130f, () => build.SelectItem(index)));
        }
    }

    // ---- Construction ----

    private void EnsureBuilt()
    {
        if (canvasRoot != null)
        {
            return;
        }

        UIUtil.EnsureEventSystem();
        font =Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        canvasRoot = new GameObject("Build Toolbar", typeof(RectTransform));
        canvasRoot.transform.SetParent(transform, false);
        var canvas = canvasRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRoot.AddComponent<GraphicRaycaster>();

        // Panel along the bottom edge.
        var panel = NewChild("Panel", canvasRoot.transform);
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(1f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.sizeDelta = new Vector2(0f, 150f);
        panel.gameObject.AddComponent<Image>().color = PanelColor;
        var column = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(16, 16, 10, 12);
        column.spacing = 8f;
        column.childAlignment = TextAnchor.UpperLeft;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;

        hintText = CreateText(NewChild("Hint", panel), "", 20, TextAnchor.MiddleLeft, new Color(0.8f, 0.83f, 0.88f));
        hintText.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

        toolRow = CreateRow(panel, "Tools");
        itemRow = CreateRow(panel, "Items");

        foreach (IBuildTool tool in build.SelectableTools)
        {
            IBuildTool captured = tool;
            Image image = CreateButton(toolRow, tool.Name, 150f, () => build.SetTool(captured));
            toolButtons.Add((image, tool));
        }
    }

    private RectTransform CreateRow(RectTransform parent, string name)
    {
        RectTransform row = NewChild(name, parent);
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;
        return row;
    }

    private Image CreateButton(RectTransform parent, string label, float width, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = NewChild(label, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = ButtonColor;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        var layout = rect.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = 40f;

        RectTransform textRect = NewChild("Label", rect);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        CreateText(textRect, label, 20, TextAnchor.MiddleCenter, Color.white);
        return image;
    }

    private Text CreateText(RectTransform rect, string content, int size, TextAnchor alignment, Color color)
    {
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.text = content;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform NewChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }
}
