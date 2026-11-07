using System.Collections.Generic;
using UnityEngine;

// Every item in the game. Lookup by id is for saves and debug commands.
[CreateAssetMenu(menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemDefinition> items = new List<ItemDefinition>();

    public ItemDefinition Find(string id)
    {
        foreach (var item in items)
        {
            if (item != null && item.id == id)
            {
                return item;
            }
        }
        return null;
    }
}
