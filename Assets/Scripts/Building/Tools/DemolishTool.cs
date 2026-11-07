using System;
using System.Collections.Generic;

// Hover a built wall, floor or object to highlight it red; click to remove it.
public class DemolishTool : IBuildTool
{
    private readonly BuildController build;
    private BuiltPiece hovered;

    public DemolishTool(BuildController build)
    {
        this.build = build;
    }

    public string Name => "Demolish";
    public string Hint => "Click a wall, floor or object to remove it      Right-click / Esc drop tool";
    public IReadOnlyList<BuildCatalog.Item> Items => Array.Empty<BuildCatalog.Item>();
    public int SelectedIndex { get; set; }

    public void Enter()
    {
        hovered = null;
    }

    public void Exit()
    {
        SetHovered(null);
    }

    public void Update()
    {
        if (build.CancelPressed)
        {
            build.SetTool(build.NoTool);
            return;
        }

        SetHovered(build.PieceUnderPointer());

        if (hovered != null && build.PrimaryPressed)
        {
            BuiltPiece target = hovered;
            SetHovered(null);
            build.World.Remove(target);
        }
    }

    private void SetHovered(BuiltPiece piece)
    {
        if (piece == hovered)
        {
            return;
        }

        if (hovered != null)
        {
            hovered.SetHighlight(null);
        }

        hovered = piece;

        if (hovered != null)
        {
            hovered.SetHighlight(build.InvalidMaterial);
        }
    }
}
