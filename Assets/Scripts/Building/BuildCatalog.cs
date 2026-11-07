using System;
using System.Collections.Generic;
using UnityEngine;

// Everything the player can build, grouped by tool. Item ids are what gets saved, so keep them stable
// once save files exist (renaming a display name or swapping a prefab is fine; changing an id is not).
[CreateAssetMenu(menuName = "Building/Build Catalog")]
public class BuildCatalog : ScriptableObject
{
    [Serializable]
    public class Item
    {
        public string id;
        public string displayName;
        public GameObject prefab;
    }

    [Tooltip("Grid size in metres. Must match the modular kit (Synty Polygon pieces are 2.5 m).")]
    public float cellSize = 2.5f;

    [Tooltip("Ghost shown at the grid corner under the cursor while using the wall tool.")]
    public GameObject cornerMarker;

    public List<Item> walls = new List<Item>();
    public List<Item> floors = new List<Item>();
    public List<Item> objects = new List<Item>();

    public Item Find(string id)
    {
        return walls.Find(i => i.id == id) ?? floors.Find(i => i.id == id) ?? objects.Find(i => i.id == id);
    }
}
