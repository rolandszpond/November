using System;

// Fire-and-forget toast messages ("New item: Stone", "Inventory full"). The HUD shows them; gameplay code doesn't
// need a reference to it.
public static class HudMessages
{
    public static event Action<string> Posted;

    public static void Show(string message) => Posted?.Invoke(message);
}
