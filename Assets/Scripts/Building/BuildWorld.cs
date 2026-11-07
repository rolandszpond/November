using System.Collections.Generic;
using UnityEngine;

// The data model for everything built: walls by grid edge, floors by grid cell, objects free-placed.
// All spawning/removing goes through here so the data and the scene never drift apart; save/load and undo
// can be built on top of these collections later.
public class BuildWorld : MonoBehaviour
{
    [SerializeField] private BuildCatalog catalog;
    [SerializeField] private float groundHeight;
    [Tooltip("Floors sit this far above the ground so they don't z-fight with it.")]
    [SerializeField] private float floorOffset = 0.02f;

    private readonly Dictionary<WallEdge, BuiltPiece> walls = new Dictionary<WallEdge, BuiltPiece>();
    private readonly Dictionary<Vector2Int, BuiltPiece> floors = new Dictionary<Vector2Int, BuiltPiece>();
    private readonly HashSet<BuiltPiece> objects = new HashSet<BuiltPiece>();

    public BuildCatalog Catalog => catalog;
    public float CellSize => catalog.cellSize;
    public float GroundHeight => groundHeight;

    public IReadOnlyDictionary<WallEdge, BuiltPiece> Walls => walls;
    public IReadOnlyDictionary<Vector2Int, BuiltPiece> Floors => floors;
    public IReadOnlyCollection<BuiltPiece> Objects => objects;

    // ---- Grid maths ----

    public Vector2Int WorldToCell(Vector3 position) =>
        new Vector2Int(Mathf.FloorToInt(position.x / CellSize), Mathf.FloorToInt(position.z / CellSize));

    public Vector2Int WorldToCorner(Vector3 position) =>
        new Vector2Int(Mathf.RoundToInt(position.x / CellSize), Mathf.RoundToInt(position.z / CellSize));

    public Vector3 CornerToWorld(Vector2Int corner) =>
        new Vector3(corner.x * CellSize, groundHeight, corner.y * CellSize);

    // Height objects should stand at: on top of a floor tile if there is one, otherwise the ground.
    public float SurfaceHeightAt(Vector3 position) =>
        floors.ContainsKey(WorldToCell(position)) ? groundHeight + floorOffset : groundHeight;

    // Synty wall pieces span local X from -2.5 to 0 with the pivot at one end, so the pivot goes on the
    // edge's End corner with local +X pointing from Start to End.
    public void GetWallPose(WallEdge edge, out Vector3 position, out Quaternion rotation)
    {
        position = CornerToWorld(edge.End);
        rotation = edge.AlongX ? Quaternion.identity : Quaternion.Euler(0f, -90f, 0f);
    }

    // Synty floor tiles span local X -2.5..0 and Z 0..2.5.
    public void GetFloorPose(Vector2Int cell, out Vector3 position, out Quaternion rotation)
    {
        position = new Vector3((cell.x + 1) * CellSize, groundHeight + floorOffset, cell.y * CellSize);
        rotation = Quaternion.identity;
    }

    // ---- Queries ----

    public bool HasWall(WallEdge edge) => walls.ContainsKey(edge);
    public bool HasFloor(Vector2Int cell) => floors.ContainsKey(cell);

    // ---- Changes ----

    public bool TryPlaceWall(WallEdge edge, BuildCatalog.Item item)
    {
        if (HasWall(edge))
        {
            return false;
        }

        GetWallPose(edge, out Vector3 position, out Quaternion rotation);
        BuiltPiece piece = Spawn(item, position, rotation, PieceKind.Wall);
        piece.Edge = edge;
        walls.Add(edge, piece);
        return true;
    }

    public bool TryPlaceFloor(Vector2Int cell, BuildCatalog.Item item)
    {
        if (HasFloor(cell))
        {
            return false;
        }

        GetFloorPose(cell, out Vector3 position, out Quaternion rotation);
        BuiltPiece piece = Spawn(item, position, rotation, PieceKind.Floor);
        piece.Cell = cell;
        floors.Add(cell, piece);
        return true;
    }

    public BuiltPiece PlaceObject(BuildCatalog.Item item, Vector3 position, Quaternion rotation)
    {
        BuiltPiece piece = Spawn(item, position, rotation, PieceKind.Object);
        objects.Add(piece);
        return piece;
    }

    public void Remove(BuiltPiece piece)
    {
        switch (piece.Kind)
        {
            case PieceKind.Wall:
                walls.Remove(piece.Edge);
                break;
            case PieceKind.Floor:
                floors.Remove(piece.Cell);
                break;
            case PieceKind.Object:
                objects.Remove(piece);
                break;
        }

        Destroy(piece.gameObject);
    }

    private BuiltPiece Spawn(BuildCatalog.Item item, Vector3 position, Quaternion rotation, PieceKind kind)
    {
        GameObject instance = Instantiate(item.prefab, position, rotation, transform);
        BuiltPiece piece = instance.AddComponent<BuiltPiece>();
        piece.Kind = kind;
        piece.ItemId = item.id;
        return piece;
    }
}
