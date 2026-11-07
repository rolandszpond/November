using System.Collections.Generic;
using UnityEngine;

// Click a grid corner and drag to draw a straight wall (along X or Z, whichever the drag is longer in).
// Release to build. Edges that already have a wall show red and are skipped.
public class WallTool : IBuildTool
{
    private readonly BuildController build;
    private bool dragging;
    private Vector2Int dragStart;
    private readonly List<WallEdge> edges = new List<WallEdge>();

    public WallTool(BuildController build)
    {
        this.build = build;
    }

    public string Name => "Wall";
    public string Hint => "Drag from a corner to draw walls      Right-click / Esc cancel";
    public IReadOnlyList<BuildCatalog.Item> Items => build.World.Catalog.walls;
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

        Vector2Int corner = world.WorldToCorner(point);

        if (!dragging)
        {
            build.Ghosts.Show(world.Catalog.cornerMarker, world.CornerToWorld(corner), Quaternion.identity, true);

            if (build.PrimaryPressed)
            {
                dragging = true;
                dragStart = corner;
            }
        }

        if (dragging)
        {
            // Lock to the axis with the larger drag distance.
            Vector2Int delta = corner - dragStart;
            Vector2Int end = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? new Vector2Int(corner.x, dragStart.y)
                : new Vector2Int(dragStart.x, corner.y);

            edges.Clear();
            edges.AddRange(WallEdge.Line(dragStart, end));

            BuildCatalog.Item item = Items[SelectedIndex];
            if (edges.Count == 0)
            {
                build.Ghosts.Show(world.Catalog.cornerMarker, world.CornerToWorld(dragStart), Quaternion.identity, true);
            }
            foreach (WallEdge edge in edges)
            {
                world.GetWallPose(edge, out Vector3 position, out Quaternion rotation);
                build.Ghosts.Show(item.prefab, position, rotation, !world.HasWall(edge));
            }

            if (build.CancelPressed)
            {
                dragging = false;
            }
            else if (build.PrimaryReleased)
            {
                foreach (WallEdge edge in edges)
                {
                    world.TryPlaceWall(edge, item);
                }
                dragging = false;
            }
        }
        else if (build.CancelPressed)
        {
            build.SetTool(build.NoTool);
            return;
        }

        build.Ghosts.End();
    }
}
