using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Right-click menu for a slot. A full-screen transparent blocker behind it closes it when clicking elsewhere.
public class ContextMenuView : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private Button buttonTemplate;
    [SerializeField] private Button blocker;

    private readonly List<Button> buttons = new List<Button>();

    public bool IsOpen => gameObject.activeSelf;

    private void Awake()
    {
        buttonTemplate.gameObject.SetActive(false);
        blocker.onClick.AddListener(Hide);
    }

    public void Show(Vector2 screenPosition, IReadOnlyList<(string label, Action action)> entries)
    {
        foreach (var b in buttons) Destroy(b.gameObject);
        buttons.Clear();
        if (entries.Count == 0)
        {
            Hide();
            return;
        }

        foreach (var (label, action) in entries)
        {
            var button = Instantiate(buttonTemplate, panel);
            button.gameObject.SetActive(true);
            button.GetComponentInChildren<TMP_Text>().text = label;
            var captured = action;
            button.onClick.AddListener(() =>
            {
                Hide();
                captured();
            });
            buttons.Add(button);
        }

        gameObject.SetActive(true);
        var canvas = GetComponentInParent<Canvas>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)panel.parent, screenPosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local);
        panel.anchoredPosition = local;
        panel.SetAsLastSibling();
    }

    public void Hide() => gameObject.SetActive(false);
}
