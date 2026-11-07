using System.Collections.Generic;
using UnityEngine;

// Reusable see-through previews of prefabs. Tools redraw every frame:
//   Begin();  Show(prefab, pos, rot, valid) per ghost;  End();
// Ghosts not shown this frame are hidden; instances are kept per prefab and reused, never re-created per frame.
public class GhostPool
{
    private class Ghost
    {
        public GameObject Root;
        public Renderer[] Renderers;
        public Material[][] Slots;
        public bool? Valid;
    }

    private const int IgnoreRaycastLayer = 2;

    private readonly Transform parent;
    private readonly Material validMaterial;
    private readonly Material invalidMaterial;
    private readonly Dictionary<GameObject, List<Ghost>> pool = new Dictionary<GameObject, List<Ghost>>();
    private readonly Dictionary<GameObject, int> used = new Dictionary<GameObject, int>();

    public GhostPool(Transform parent, Material validMaterial, Material invalidMaterial)
    {
        this.parent = parent;
        this.validMaterial = validMaterial;
        this.invalidMaterial = invalidMaterial;
    }

    public void Begin()
    {
        used.Clear();
    }

    public void Show(GameObject prefab, Vector3 position, Quaternion rotation, bool valid)
    {
        if (prefab == null)
        {
            return;
        }

        if (!pool.TryGetValue(prefab, out List<Ghost> list))
        {
            list = new List<Ghost>();
            pool.Add(prefab, list);
        }

        used.TryGetValue(prefab, out int index);
        used[prefab] = index + 1;

        if (index == list.Count)
        {
            list.Add(Create(prefab));
        }

        Ghost ghost = list[index];
        ghost.Root.transform.SetPositionAndRotation(position, rotation);
        if (ghost.Valid != valid)
        {
            SetMaterial(ghost, valid ? validMaterial : invalidMaterial);
            ghost.Valid = valid;
        }

        if (!ghost.Root.activeSelf)
        {
            ghost.Root.SetActive(true);
        }
    }

    public void End()
    {
        foreach (KeyValuePair<GameObject, List<Ghost>> entry in pool)
        {
            used.TryGetValue(entry.Key, out int count);
            for (int i = count; i < entry.Value.Count; i++)
            {
                if (entry.Value[i].Root.activeSelf)
                {
                    entry.Value[i].Root.SetActive(false);
                }
            }
        }
    }

    public void HideAll()
    {
        Begin();
        End();
    }

    private Ghost Create(GameObject prefab)
    {
        GameObject root = Object.Instantiate(prefab, parent);
        root.name = prefab.name + " (Ghost)";

        // Purely visual: no collisions, no raycast hits, no scripts, no shadows.
        foreach (Collider collider in root.GetComponentsInChildren<Collider>())
        {
            Object.Destroy(collider);
        }
        foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>())
        {
            Object.Destroy(behaviour);
        }
        foreach (Transform t in root.GetComponentsInChildren<Transform>())
        {
            t.gameObject.layer = IgnoreRaycastLayer;
        }

        var ghost = new Ghost { Root = root, Renderers = root.GetComponentsInChildren<Renderer>() };
        ghost.Slots = new Material[ghost.Renderers.Length][];
        for (int i = 0; i < ghost.Renderers.Length; i++)
        {
            ghost.Renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ghost.Slots[i] = new Material[ghost.Renderers[i].sharedMaterials.Length];
        }
        return ghost;
    }

    private static void SetMaterial(Ghost ghost, Material material)
    {
        for (int i = 0; i < ghost.Renderers.Length; i++)
        {
            Material[] slots = ghost.Slots[i];
            for (int m = 0; m < slots.Length; m++)
            {
                slots[m] = material;
            }
            ghost.Renderers[i].sharedMaterials = slots;
        }
    }
}
