using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Always-on play HUD: the hotbar with the selected slot, carry weight, the interaction prompt and toast messages
// (new items discovered, recipes unlocked, "inventory full"). Hidden outside Play mode.
public class HotbarHUD : MonoBehaviour
{
    [SerializeField] private PlayerInventory inventory;
    [SerializeField] private PlayerInteractor interactor;
    [SerializeField] private GameModeController game;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private ContainerGridView hotbarGrid;
    [SerializeField] private TMP_Text selectedName;
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private Image weightFill;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_Text toastText;
    [SerializeField] private float toastDuration = 3f;
    [SerializeField] private int maxToasts = 4;

    private readonly List<(string text, float time)> toasts = new List<(string, float)>();
    private bool weightDirty = true;

    private void Awake()
    {
        if (inventory == null) inventory = FindAnyObjectByType<PlayerInventory>();
        if (interactor == null) interactor = FindAnyObjectByType<PlayerInteractor>();
        if (game == null) game = FindAnyObjectByType<GameModeController>();
        UIUtil.EnsureEventSystem();
    }

    private void OnEnable() => HudMessages.Posted += AddToast;
    private void OnDisable() => HudMessages.Posted -= AddToast;

    // Start: PlayerInventory creates its containers in Awake.
    private void Start()
    {
        hotbarGrid.Bind(inventory.Hotbar, null);
        hotbarGrid.SetSelected(inventory.SelectedIndex);
        inventory.SelectionChanged += OnSelectionChanged;
        inventory.Changed += () => weightDirty = true;
        inventory.Discovery.ItemDiscovered += item => AddToast($"Discovered: {item.DisplayName}");
        inventory.Discovery.RecipeUnlocked += recipe => AddToast($"<color=#ffd77a>Recipe unlocked: {recipe.output.DisplayName}</color>");
        OnSelectionChanged(inventory.SelectedIndex);
    }

    private void OnDestroy()
    {
        if (inventory != null) inventory.SelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged(int index)
    {
        hotbarGrid.SetSelected(index);
        UpdateSelectedName();
    }

    private void UpdateSelectedName()
    {
        var stack = inventory.SelectedStack;
        selectedName.text = stack.IsEmpty ? "" : stack.item.DisplayName;
    }

    private void Update()
    {
        bool playing = game == null || game.StateMachine == null || game.StateMachine.CurrentState == game.PlayMode;
        group.alpha = playing ? 1f : 0f;

        if (weightDirty)
        {
            weightDirty = false;
            UpdateSelectedName();
            float weight = inventory.TotalWeight;
            float max = inventory.MaxCarryWeight;
            bool over = inventory.IsOverencumbered;
            weightText.text = $"{weight:0.#}/{max:0} kg";
            weightText.color = over ? new Color(1f, 0.48f, 0.48f) : Color.white;
            weightFill.fillAmount = max > 0f ? Mathf.Clamp01(weight / max) : 1f;
            weightFill.color = over ? new Color(0.9f, 0.35f, 0.3f) : new Color(0.45f, 0.7f, 0.45f);
        }

        string prompt = playing && interactor != null && interactor.isActiveAndEnabled ? interactor.CurrentPrompt : null;
        promptText.text = string.IsNullOrEmpty(prompt) ? "" : $"<b>[E]</b> {prompt}";

        UpdateToasts();
    }

    private void AddToast(string message)
    {
        toasts.Add((message, Time.unscaledTime));
        while (toasts.Count > maxToasts) toasts.RemoveAt(0);
        RenderToasts();
    }

    private void UpdateToasts()
    {
        int before = toasts.Count;
        toasts.RemoveAll(t => Time.unscaledTime - t.time > toastDuration);
        if (toasts.Count != before) RenderToasts();
    }

    private void RenderToasts()
    {
        var lines = new string[toasts.Count];
        for (int i = 0; i < toasts.Count; i++) lines[i] = toasts[i].text;
        toastText.text = string.Join("\n", lines);
    }
}
