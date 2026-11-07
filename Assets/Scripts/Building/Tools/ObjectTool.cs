using System.Collections.Generic;
using UnityEngine;

// The selected object follows the mouse (snapped), R rotates 90°, click places. Stays active for repeat placing.
// Red when it would overlap a wall, another object or the player.
public class ObjectTool : IBuildTool
{
    private readonly BuildController build;
    private float yaw;

    public ObjectTool(BuildController build)
    {
        this.build = build;
    }

    public string Name => "Object";
    public string Hint => "Click to place      R rotate      Right-click / Esc drop tool";
    public IReadOnlyList<BuildCatalog.Item> Items => build.World.Catalog.objects;
    public int SelectedIndex { get; set; }

    public void Enter() { }
    public void Exit() { }

    public void Update()
    {
        build.Ghosts.Begin();

        if (build.CancelPressed)
        {
            build.SetTool(build.NoTool);
            return;
        }

        if (build.RotatePressed)
        {
            yaw = (yaw + 90f) % 360f;
        }

        if (Items.Count == 0 || build.PointerOverUI || !build.TryGetPointerOnGround(out Vector3 point))
        {
            build.Ghosts.End();
            return;
        }

        BuildCatalog.Item item = Items[SelectedIndex];
        float snap = build.ObjectSnap;
        var position = new Vector3(Mathf.Round(point.x / snap) * snap, 0f, Mathf.Round(point.z / snap) * snap);
        position.y = build.World.SurfaceHeightAt(position);
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);

        bool valid = build.CanPlaceObject(item.prefab, position, rotation);
        build.Ghosts.Show(item.prefab, position, rotation, valid);

        if (valid && build.PrimaryPressed)
        {
            build.World.PlaceObject(item, position, rotation);
        }

        build.Ghosts.End();
    }
}
