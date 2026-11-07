using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Generates the inventory UI prefabs (slot, recipe row, HUD, inventory screen) with placeholder styling.
// Run once, then restyle the prefabs by hand. Re-running overwrites them, so after hand edits don't run it again
// (or copy the prefabs first). Layout is for a 1920x1080 reference resolution.
public static class UIPrefabBuilder
{
    public const string Folder = "Assets/Prefabs/UI";
    public const string SlotPath = Folder + "/Slot.prefab";
    public const string RecipeRowPath = Folder + "/RecipeRow.prefab";
    public const string HudPath = Folder + "/InventoryHUD.prefab";
    public const string ScreenPath = Folder + "/InventoryScreen.prefab";

    private static readonly Color PanelColor = new Color(0.07f, 0.08f, 0.1f, 1f);
    private static readonly Color SlotColor = new Color(0.12f, 0.13f, 0.16f, 1f);
    private static readonly Color ButtonColor = new Color(0.22f, 0.36f, 0.52f, 1f);
    private static readonly Color Muted = new Color(0.62f, 0.7f, 0.78f);
    private const float SlotSize = 80f;
    private const float SlotSpacing = 8f;

    [MenuItem("Tools/Inventory/2. Build UI Prefabs")]
    public static void BuildAll()
    {
        ImportTmpEssentialsIfNeeded();
        StarterContentBuilder.EnsureFolder(Folder);
        var slot = BuildSlot();
        var row = BuildRecipeRow();
        BuildHud(slot);
        BuildScreen(slot, row);
        AssetDatabase.SaveAssets();
        Debug.Log("Inventory UI prefabs built in " + Folder);
    }

    public static void ImportTmpEssentialsIfNeeded()
    {
        if (TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null) return;
        var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
        string path = package.resolvedPath + "/Package Resources/TMP Essential Resources.unitypackage";
#pragma warning disable CS0618 // replacement API differs between editor versions; this one still works
        AssetDatabase.ImportPackage(path, false);
#pragma warning restore CS0618
        AssetDatabase.Refresh();
    }

    // ---------------------------------------------------------------- Slot / row

    private static SlotView BuildSlot()
    {
        var root = new GameObject("Slot", typeof(RectTransform));
        try
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(SlotSize, SlotSize);
            var bg = root.AddComponent<Image>();
            bg.color = SlotColor;

            var selection = Stretch("Selection", root.transform, -3f);
            var selImage = selection.gameObject.AddComponent<Image>();
            selImage.color = new Color(1f, 0.84f, 0.35f, 0.9f);
            selImage.raycastTarget = false;
            selection.SetAsFirstSibling();
            selection.gameObject.SetActive(false);
            // Inner cut-out so the selection reads as an outline.
            var inner = Stretch("Inner", selection, 3f);
            var innerImage = inner.gameObject.AddComponent<Image>();
            innerImage.color = SlotColor;
            innerImage.raycastTarget = false;

            var icon = Stretch("Icon", root.transform, 8f).gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var fallback = Text("Fallback", Stretch("FallbackRect", root.transform, 4f), "", 22, TextAlignmentOptions.Center, Muted);
            var count = Text("Count", Stretch("CountRect", root.transform, 5f), "", 18, TextAlignmentOptions.BottomRight, Color.white);
            count.fontStyle = FontStyles.Bold;
            var hint = Text("KeyHint", Stretch("KeyHintRect", root.transform, 4f), "", 14, TextAlignmentOptions.TopLeft, Muted);

            var view = root.AddComponent<SlotView>();
            Wire(view, "background", bg);
            Wire(view, "icon", icon);
            Wire(view, "countText", count);
            Wire(view, "fallbackLabel", fallback);
            Wire(view, "keyHint", hint);
            Wire(view, "selection", selection.gameObject);
            return PrefabUtility.SaveAsPrefabAsset(root, SlotPath).GetComponent<SlotView>();
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static RecipeRowView BuildRecipeRow()
    {
        var root = new GameObject("RecipeRow", typeof(RectTransform));
        try
        {
            var bg = root.AddComponent<Image>();
            bg.color = SlotColor;
            var button = root.AddComponent<Button>();
            button.targetGraphic = bg;
            root.AddComponent<LayoutElement>().preferredHeight = 52f;

            var icon = Box("Icon", root.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(6f, 0f), new Vector2(44f, 44f))
                .gameObject.AddComponent<Image>();
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var labelRect = Box("Label", root.transform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(58f, 0f), new Vector2(-150f, 0f));
            var label = AddText(labelRect, "Recipe", 20, TextAlignmentOptions.MidlineLeft, Color.white);
            var subRect = Box("SubLabel", root.transform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(-8f, 0f), new Vector2(90f, 0f));
            var sub = AddText(subRect, "", 14, TextAlignmentOptions.MidlineRight, Muted);

            var view = root.AddComponent<RecipeRowView>();
            Wire(view, "button", button);
            Wire(view, "background", bg);
            Wire(view, "icon", icon);
            Wire(view, "label", label);
            Wire(view, "subLabel", sub);
            return PrefabUtility.SaveAsPrefabAsset(root, RecipeRowPath).GetComponent<RecipeRowView>();
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ---------------------------------------------------------------- HUD

    private static void BuildHud(SlotView slot)
    {
        // No GraphicRaycaster: the HUD never blocks clicks (and dragging an item over it still drops it in the world).
        var root = CanvasRoot("InventoryHUD", 5, raycaster: false);
        try
        {
            var groupRect = Stretch("Group", root.transform, 0f);
            var group = groupRect.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            float hotbarWidth = 8 * SlotSize + 7 * SlotSpacing;
            var hotbarRect = Box("Hotbar", groupRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(hotbarWidth, SlotSize));
            var hotbar = Grid(hotbarRect, slot, 8, SlotSize, true);

            var selected = AddText(Box("SelectedName", groupRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 112f), new Vector2(500f, 30f)),
                "", 20, TextAlignmentOptions.Center, Color.white);

            float weightX = hotbarWidth * 0.5f + 24f;
            var weightBg = Box("WeightBar", groupRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(weightX, 30f), new Vector2(180f, 14f));
            weightBg.gameObject.AddComponent<Image>().color = SlotColor;
            var fill = FilledImage(Stretch("Fill", weightBg, 0f));
            var weightText = AddText(Box("WeightText", groupRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(weightX, 52f), new Vector2(220f, 28f)),
                "", 20, TextAlignmentOptions.BottomLeft, Color.white);
            weightText.fontStyle = FontStyles.Bold;

            // Above the hotbar, clear of the character.
            var promptRect = Box("Prompt", groupRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 156f), new Vector2(700f, 44f));
            var prompt = AddText(promptRect, "", 26, TextAlignmentOptions.Center, Color.white);

            // Toasts sit outside the group so they also show while a menu is open.
            // Top centre, above the inventory screen's panels.
            var toast = AddText(Box("Toasts", root.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(900f, 150f)),
                "", 22, TextAlignmentOptions.Top, Color.white);

            var hud = root.AddComponent<HotbarHUD>();
            Wire(hud, "group", group);
            Wire(hud, "hotbarGrid", hotbar);
            Wire(hud, "selectedName", selected);
            Wire(hud, "weightText", weightText);
            Wire(hud, "weightFill", fill);
            Wire(hud, "promptText", prompt);
            Wire(hud, "toastText", toast);
            PrefabUtility.SaveAsPrefabAsset(root, HudPath);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ---------------------------------------------------------------- Inventory screen

    private static void BuildScreen(SlotView slot, RecipeRowView rowPrefab)
    {
        var canvas = CanvasRoot("InventoryScreen", 20, raycaster: true);
        try
        {
            var root = Stretch("Root", canvas.transform, 0f);
            // Dim the game, but don't catch raycasts: releasing a dragged item over the open screen drops it.
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.45f);
            dim.raycastTarget = false;

            float gridWidth = 8 * SlotSize + 7 * SlotSpacing;

            // ---- Left: backpack + hotbar
            var left = Panel("PlayerPanel", root, new Vector2(-400f, 0f), new Vector2(gridWidth + 48f, 640f));
            AddText(TopLeft("Title", left, 24f, 18f, 400f, 40f), "Inventory", 32, TextAlignmentOptions.MidlineLeft, Color.white);

            var equipRect = TopLeft("BackpackEquip", left, 24f, 70f, SlotSize, SlotSize);
            var equip = Grid(equipRect, slot, 1, SlotSize, false);
            AddText(TopLeft("BackpackLabel", left, 24f + SlotSize + 16f, 70f, 400f, 36f), "Backpack", 24, TextAlignmentOptions.MidlineLeft, Color.white);
            AddText(TopLeft("BackpackSub", left, 24f + SlotSize + 16f, 106f, 500f, 30f), "Drag a backpack onto the slot to equip it", 16, TextAlignmentOptions.MidlineLeft, Muted);

            var backpackRect = TopLeft("BackpackGrid", left, 24f, 166f, gridWidth, 3 * SlotSize + 2 * SlotSpacing);
            var backpack = Grid(backpackRect, slot, 8, SlotSize, false);
            var emptyHint = AddText(TopLeft("BackpackEmptyHint", left, 24f, 166f, gridWidth, 3 * SlotSize), "Craft a Simple Backpack to expand your inventory",
                20, TextAlignmentOptions.Center, Muted);

            AddText(TopLeft("HotbarLabel", left, 24f, 440f, 400f, 30f), "Hotbar", 22, TextAlignmentOptions.MidlineLeft, Color.white);
            var hotbarRect = TopLeft("HotbarGrid", left, 24f, 476f, gridWidth, SlotSize);
            var hotbar = Grid(hotbarRect, slot, 8, SlotSize, true);

            var weightBg = TopLeft("WeightBar", left, 24f, 584f, 360f, 18f);
            weightBg.gameObject.AddComponent<Image>().color = SlotColor;
            var weightFill = FilledImage(Stretch("Fill", weightBg, 0f));
            var weightText = AddText(TopLeft("WeightText", left, 400f, 574f, gridWidth - 376f, 36f), "", 20, TextAlignmentOptions.MidlineLeft, Color.white);
            AddText(TopLeft("Help", left, 24f, 612f, gridWidth, 24f),
                "Drag: move   Right-drag: split   Shift+click: quick move   Right-click: options   Drag outside: drop",
                14, TextAlignmentOptions.MidlineLeft, Muted);

            // ---- Right: crafting
            var right = Panel("RightPanel", root, new Vector2(420f, 0f), new Vector2(720f, 640f));
            var craftingRect = Stretch("Crafting", right, 0f);
            var crafting = BuildCraftingPanel(craftingRect, rowPrefab);

            // ---- Right: loot (replaces crafting while looting)
            var lootRect = Stretch("Loot", right, 0f);
            var lootTitle = AddText(TopLeft("Title", lootRect, 24f, 18f, 600f, 40f), "Chest", 32, TextAlignmentOptions.MidlineLeft, Color.white);
            float lootGridWidth = 8 * 72f + 7 * SlotSpacing;
            var lootGridRect = TopLeft("LootGrid", lootRect, 24f, 76f, lootGridWidth, 2 * 72f + SlotSpacing);
            var lootGrid = Grid(lootGridRect, slot, 8, 72f, false);
            var takeAll = MakeButton("TakeAll", lootRect, "Take All", 24f, 76f + 2 * 72f + SlotSpacing + 20f, 200f, 50f);
            AddText(TopLeft("LootHelp", lootRect, 24f, 330f, 640f, 60f), "Drag items into your inventory, or Shift+click to grab them.",
                18, TextAlignmentOptions.TopLeft, Muted);
            lootRect.gameObject.SetActive(false);

            // ---- Overlays
            var ghost = Box("DragGhost", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72f, 72f));
            var ghostIcon = ghost.gameObject.AddComponent<Image>();
            ghostIcon.preserveAspect = true;
            ghostIcon.raycastTarget = false;
            var ghostCount = AddText(Stretch("Count", ghost, 2f), "", 18, TextAlignmentOptions.BottomRight, Color.white);
            ghostCount.fontStyle = FontStyles.Bold;
            ghost.gameObject.SetActive(false);

            var menu = BuildContextMenu(root);
            var tooltip = BuildTooltip(root);

            var screen = canvas.AddComponent<InventoryScreen>();
            Wire(screen, "root", root.gameObject);
            Wire(screen, "hotbarGrid", hotbar);
            Wire(screen, "backpackGrid", backpack);
            Wire(screen, "backpackEquipGrid", equip);
            Wire(screen, "backpackEmptyHint", emptyHint.gameObject);
            Wire(screen, "weightText", weightText);
            Wire(screen, "weightFill", weightFill);
            Wire(screen, "craftingPanel", crafting);
            Wire(screen, "lootPanel", lootRect.gameObject);
            Wire(screen, "lootTitle", lootTitle);
            Wire(screen, "lootGrid", lootGrid);
            Wire(screen, "takeAllButton", takeAll);
            Wire(screen, "dragGhost", ghost);
            Wire(screen, "dragGhostIcon", ghostIcon);
            Wire(screen, "dragGhostCount", ghostCount);
            Wire(screen, "contextMenu", menu);
            Wire(screen, "tooltip", tooltip);
            root.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(canvas, ScreenPath);
        }
        finally
        {
            Object.DestroyImmediate(canvas);
        }
    }

    private static CraftingPanel BuildCraftingPanel(RectTransform parent, RecipeRowView rowPrefab)
    {
        var title = AddText(TopLeft("Title", parent, 24f, 18f, 600f, 40f), "Crafting", 32, TextAlignmentOptions.MidlineLeft, Color.white);

        // Scrollable recipe list.
        var scrollRect = TopLeft("RecipeList", parent, 24f, 76f, 320f, 500f);
        scrollRect.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);
        var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.scrollSensitivity = 30f;
        var viewport = Stretch("Viewport", scrollRect, 4f);
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Box("Content", viewport, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero);
        var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 4f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = content;

        var lockedHint = AddText(TopLeft("LockedHint", parent, 24f, 584f, 320f, 40f), "", 16, TextAlignmentOptions.TopLeft, Muted);

        // Details of the selected recipe.
        var details = TopLeft("Details", parent, 368f, 76f, 328f, 540f);
        var icon = TopLeft("Icon", details, 0f, 0f, 96f, 96f).gameObject.AddComponent<Image>();
        icon.preserveAspect = true;
        var name = AddText(TopLeft("Name", details, 108f, 0f, 220f, 48f), "", 24, TextAlignmentOptions.TopLeft, Color.white);
        name.fontStyle = FontStyles.Bold;
        var description = AddText(TopLeft("Description", details, 108f, 50f, 220f, 90f), "", 15, TextAlignmentOptions.TopLeft, Color.white);
        AddText(TopLeft("RequiresLabel", details, 0f, 150f, 328f, 30f), "Requires", 20, TextAlignmentOptions.MidlineLeft, Muted);
        var ingredients = AddText(TopLeft("Ingredients", details, 0f, 184f, 328f, 240f), "", 20, TextAlignmentOptions.TopLeft, Color.white);
        var craft = MakeButton("Craft", details, "Craft", 0f, 440f, 156f, 54f);
        var craftAll = MakeButton("CraftAll", details, "Craft all", 172f, 440f, 156f, 54f);

        var panel = parent.gameObject.AddComponent<CraftingPanel>();
        Wire(panel, "title", title);
        Wire(panel, "listContent", content);
        Wire(panel, "rowPrefab", rowPrefab);
        Wire(panel, "lockedHint", lockedHint);
        Wire(panel, "details", details.gameObject);
        Wire(panel, "detailIcon", icon);
        Wire(panel, "detailName", name);
        Wire(panel, "detailDescription", description);
        Wire(panel, "detailIngredients", ingredients);
        Wire(panel, "craftButton", craft);
        Wire(panel, "craftAllButton", craftAll);
        Wire(panel, "craftAllLabel", craftAll.GetComponentInChildren<TMP_Text>());
        return panel;
    }

    private static ContextMenuView BuildContextMenu(RectTransform parent)
    {
        var rootRect = Stretch("ContextMenu", parent, 0f);
        var blockerImage = rootRect.gameObject.AddComponent<Image>();
        blockerImage.color = Color.clear;
        var blocker = rootRect.gameObject.AddComponent<Button>();
        blocker.transition = Selectable.Transition.None;

        var panel = Box("Panel", rootRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), Vector2.zero, new Vector2(300f, 0f));
        panel.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.11f, 0.14f, 0.98f);
        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.spacing = 2f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var template = MakeButton("ButtonTemplate", panel, "Option", 0f, 0f, 292f, 40f);
        template.GetComponent<Image>().color = SlotColor;
        template.GetComponentInChildren<TMP_Text>().alignment = TextAlignmentOptions.MidlineLeft;
        template.GetComponentInChildren<TMP_Text>().margin = new Vector4(12f, 0f, 8f, 0f);
        template.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

        var view = rootRect.gameObject.AddComponent<ContextMenuView>();
        Wire(view, "panel", panel);
        Wire(view, "buttonTemplate", template);
        Wire(view, "blocker", blocker);
        rootRect.gameObject.SetActive(false);
        return view;
    }

    private static ItemTooltip BuildTooltip(RectTransform parent)
    {
        var rootRect = Stretch("Tooltip", parent, 0f);
        var panel = Box("Panel", rootRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 1f), Vector2.zero, new Vector2(380f, 0f));
        var bg = panel.gameObject.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.06f, 0.08f, 0.97f);
        bg.raycastTarget = false;
        var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 12);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        panel.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var title = AddText(NewRect("Title", panel), "", 22, TextAlignmentOptions.TopLeft, Color.white);
        title.fontStyle = FontStyles.Bold;
        var body = AddText(NewRect("Body", panel), "", 16, TextAlignmentOptions.TopLeft, Color.white);

        var tooltip = rootRect.gameObject.AddComponent<ItemTooltip>();
        Wire(tooltip, "panel", panel);
        Wire(tooltip, "title", title);
        Wire(tooltip, "body", body);
        rootRect.gameObject.SetActive(false);
        return tooltip;
    }

    // ---------------------------------------------------------------- Building blocks

    private static GameObject CanvasRoot(string name, int sortingOrder, bool raycaster)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        if (raycaster) go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static RectTransform Panel(string name, RectTransform parent, Vector2 position, Vector2 size)
    {
        var rect = Box(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size);
        rect.gameObject.AddComponent<Image>().color = PanelColor;
        return rect;
    }

    private static ContainerGridView Grid(RectTransform rect, SlotView slot, int columns, float cell, bool keyHints)
    {
        var grid = rect.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(cell, cell);
        grid.spacing = new Vector2(SlotSpacing, SlotSpacing);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.childAlignment = TextAnchor.UpperLeft;
        var view = rect.gameObject.AddComponent<ContainerGridView>();
        Wire(view, "slotPrefab", slot);
        Wire(view, "content", rect);
        var so = new SerializedObject(view);
        so.FindProperty("showKeyHints").boolValue = keyHints;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    private static Image FilledImage(RectTransform rect)
    {
        var image = rect.gameObject.AddComponent<Image>();
        // Filled images need a sprite to fill; the built-in UI sprite works.
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillAmount = 0f;
        image.color = new Color(0.45f, 0.7f, 0.45f);
        image.raycastTarget = false;
        return image;
    }

    private static Button MakeButton(string name, RectTransform parent, string label, float x, float y, float w, float h)
    {
        var rect = TopLeft(name, parent, x, y, w, h);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = ButtonColor;
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        AddText(Stretch("Label", rect, 0f), label, 20, TextAlignmentOptions.Center, Color.white);
        return button;
    }

    private static TMP_Text Text(string name, RectTransform rect, string text, float size, TextAlignmentOptions align, Color color)
    {
        rect.name = name;
        return AddText(rect, text, size, align, color);
    }

    private static TMP_Text AddText(RectTransform rect, string text, float size, TextAlignmentOptions align, Color color)
    {
        var tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static RectTransform Stretch(string name, Transform parent, float inset)
    {
        var rect = NewRect(name, parent);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
        return rect;
    }

    private static RectTransform Box(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var rect = NewRect(name, parent);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    // Positioned from the parent's top-left corner, y measured downwards.
    private static RectTransform TopLeft(string name, Transform parent, float x, float y, float w, float h) =>
        Box(name, parent, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));

    private static void Wire(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError($"{target.GetType().Name} has no serialized field '{field}'");
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
