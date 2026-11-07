using System;

// A count of one item in a slot. Value type: copying it never aliases another slot's contents.
[Serializable]
public struct ItemStack
{
    public ItemDefinition item;
    public int count;

    public static readonly ItemStack Empty = default;

    public ItemStack(ItemDefinition item, int count)
    {
        this.item = count > 0 ? item : null;
        this.count = item != null ? Math.Max(0, count) : 0;
    }

    public bool IsEmpty => item == null || count <= 0;
    public float Weight => IsEmpty ? 0f : item.weight * count;
    public int SpaceLeft => IsEmpty ? 0 : item.maxStack - count;

    public bool CanMergeWith(ItemStack other) =>
        !IsEmpty && !other.IsEmpty && item == other.item && item.IsStackable;

    public ItemStack WithCount(int newCount) => new ItemStack(item, newCount);

    public override string ToString() => IsEmpty ? "(empty)" : $"{item.DisplayName} x{count}";
}
