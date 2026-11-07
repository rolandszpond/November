using UnityEngine;

public enum PieceKind
{
    Wall,
    Floor,
    Object
}

// Added to every spawned wall/floor/object. Records what it is and where it sits in BuildWorld,
// so a raycast hit can be traced back to its data (demolish) and the world can be saved later.
public class BuiltPiece : MonoBehaviour
{
    public PieceKind Kind;
    public string ItemId;
    public Vector2Int Cell;   // floors
    public WallEdge Edge;     // walls

    private Renderer[] renderers;
    private Material[][] originalMaterials;

    // Pass a material to tint the whole piece (e.g. demolish hover), or null to restore it.
    public void SetHighlight(Material material)
    {
        if (renderers == null)
        {
            renderers = GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].sharedMaterials;
            }
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (material == null)
            {
                renderers[i].sharedMaterials = originalMaterials[i];
                continue;
            }

            var tinted = new Material[originalMaterials[i].Length];
            for (int m = 0; m < tinted.Length; m++)
            {
                tinted[m] = material;
            }
            renderers[i].sharedMaterials = tinted;
        }
    }
}
