using System;
using System.Collections.Generic;
using UnityEngine;

// One grid edge a wall can sit on: from corner Start, one cell along +X or +Z.
// Corners and cells share integer coordinates: cell (x, z) has corners (x, z) .. (x + 1, z + 1).
[Serializable]
public struct WallEdge : IEquatable<WallEdge>
{
    public Vector2Int Start;
    public bool AlongX;

    public WallEdge(Vector2Int start, bool alongX)
    {
        Start = start;
        AlongX = alongX;
    }

    public Vector2Int End => Start + (AlongX ? Vector2Int.right : Vector2Int.up);

    // Edges of a straight line between two corners that share an axis, in either direction.
    public static IEnumerable<WallEdge> Line(Vector2Int a, Vector2Int b)
    {
        if (a.y == b.y)
        {
            for (int x = Mathf.Min(a.x, b.x); x < Mathf.Max(a.x, b.x); x++)
            {
                yield return new WallEdge(new Vector2Int(x, a.y), true);
            }
        }
        else if (a.x == b.x)
        {
            for (int z = Mathf.Min(a.y, b.y); z < Mathf.Max(a.y, b.y); z++)
            {
                yield return new WallEdge(new Vector2Int(a.x, z), false);
            }
        }
    }

    public bool Equals(WallEdge other) => Start == other.Start && AlongX == other.AlongX;
    public override bool Equals(object obj) => obj is WallEdge other && Equals(other);
    public override int GetHashCode() => (Start.GetHashCode() * 397) ^ (AlongX ? 1 : 0);
    public override string ToString() => $"{Start}->{End}";
}
