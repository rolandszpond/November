using System.Collections.Generic;
using UnityEngine;

// Click and drag a rectangle of floor tiles; release to build. Tiles that already have a floor show red.
public class FloorTool : IBuildTool
{
    // Keeps a runaway drag from spawning thousands of ghosts.
    private const int MaxSide = 24;

    private readonly BuildController build;
    private bool dragging;
    private Vector2Int dragStart;

    public FloorTool(BuildController build)
    {
        this.build = build;
    }

    public string Name => "Floor";
    public string Hint => "Drag to paint floor tiles      Right-click / Esc cancel";
    public IReadOnlyList<BuildCatalog.Item> Items => build.World.Catalog.floors;
    public int SelectedIndex { get; set; }

    public void Enter()
    {
        dragging = false;
    }

    public void Exit()
    {
        dragging = false;
    }

    public void Update()
    {
        BuildWorld world = build.World;
        build.Ghosts.Begin();

        if (Items.Count == 0 || !build.TryGetPointerOnGround(out Vector3 point))
        {
            build.Ghosts.End();
            return;
        }

        BuildCatalog.Item item = Items[SelectedIndex];
        Vector2Int cell = world.WorldToCell(point);

        if (!dragging && build.PrimaryPressed)
        {
            dragging = true;
            dragStart = cell;
        }
        else if (!dragging && build.CancelPressed)
        {
            build.SetTool(build.NoTool);
            return;
        }

        Vector2Int from = dragging ? dragStart : cell;
        Vector2Int to = new Vector2Int(
            Mathf.Clamp(cell.x, from.x - MaxSide + 1, from.x + MaxSide - 1),
            Mathf.Clamp(cell.y, from.y - MaxSide + 1, from.y + MaxSide - 1));
        Vector2Int min = Vector2Int.Min(from, to);
        Vector2Int max = Vector2Int.Max(from, to);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int z = min.y; z <= max.y; z++)
            {
                var c = new Vector2Int(x, z);
                world.GetFloorPose(c, out Vector3 position, out Quaternion rotation);
                build.Ghosts.Show(item.prefab, position, rotation, !world.HasFloor(c));
            }
        }

        if (dragging)
        {
            if (build.CancelPressed)
            {
                dragging = false;
            }
            else if (build.PrimaryReleased)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    for (int z = min.y; z <= max.y; z++)
                    {
                        world.TryPlaceFloor(new Vector2Int(x, z), item);
                    }
                }
                dragging = false;
            }
        }

        build.Ghosts.End();
    }
}
