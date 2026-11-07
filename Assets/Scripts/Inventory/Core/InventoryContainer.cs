using System;
using System.Collections.Generic;

// A fixed-size row/grid of slots (the hotbar, the backpack, a loot chest). All slot rules live here so the UI only
// translates pointer gestures into these calls.
public class InventoryContainer
{
    private ItemStack[] slots;

    // Fired with the slot index whenever a slot's contents change, and with -1 after a resize.
    public event Action<int> SlotChanged;

    public InventoryContainer(int size)
    {
        slots = new ItemStack[Math.Max(0, size)];
    }

    public int Size => slots.Length;
    public ItemStack this[int index] => slots[index];

    public float TotalWeight
    {
        get
        {
            float total = 0f;
            foreach (var stack in slots) total += stack.Weight;
            return total;
        }
    }

    public bool IsEmpty
    {
        get
        {
            foreach (var stack in slots)
            {
                if (!stack.IsEmpty) return false;
            }
            return true;
        }
    }

    public void Set(int index, ItemStack stack)
    {
        slots[index] = stack.IsEmpty ? ItemStack.Empty : stack;
        SlotChanged?.Invoke(index);
    }

    public void Clear(int index) => Set(index, ItemStack.Empty);

    public int Count(ItemDefinition item)
    {
        int total = 0;
        foreach (var stack in slots)
        {
            if (!stack.IsEmpty && stack.item == item) total += stack.count;
        }
        return total;
    }

    // How many of this item would fit, without adding them.
    public int Capacity(ItemDefinition item)
    {
        int total = 0;
        foreach (var stack in slots)
        {
            if (stack.IsEmpty) total += item.maxStack;
            else if (stack.item == item) total += stack.SpaceLeft;
        }
        return total;
    }

    // Tops up existing stacks of the item first, then fills empty slots left to right. Returns what didn't fit.
    public int TryAdd(ItemDefinition item, int count)
    {
        if (item == null || count <= 0) return 0;

        if (item.IsStackable)
        {
            for (int i = 0; i < slots.Length && count > 0; i++)
            {
                if (slots[i].IsEmpty || slots[i].item != item || slots[i].SpaceLeft <= 0) continue;
                int moved = Math.Min(count, slots[i].SpaceLeft);
                Set(i, slots[i].WithCount(slots[i].count + moved));
                count -= moved;
            }
        }

        for (int i = 0; i < slots.Length && count > 0; i++)
        {
            if (!slots[i].IsEmpty) continue;
            int moved = Math.Min(count, item.maxStack);
            Set(i, new ItemStack(item, moved));
            count -= moved;
        }

        return count;
    }

    // Removes up to count of the item, taking from the last slots first so the front of the hotbar stays put.
    // Returns how many were actually removed.
    public int Remove(ItemDefinition item, int count)
    {
        int removed = 0;
        for (int i = slots.Length - 1; i >= 0 && removed < count; i--)
        {
            if (slots[i].IsEmpty || slots[i].item != item) continue;
            int take = Math.Min(count - removed, slots[i].count);
            Set(i, slots[i].WithCount(slots[i].count - take));
            removed += take;
        }
        return removed;
    }

    // Takes up to amount out of one slot and returns it (e.g. to drop it in the world).
    public ItemStack TakeFrom(int index, int amount)
    {
        var stack = slots[index];
        if (stack.IsEmpty || amount <= 0) return ItemStack.Empty;
        amount = Math.Min(amount, stack.count);
        Set(index, stack.WithCount(stack.count - amount));
        return stack.WithCount(amount);
    }

    // Changes the slot count. Refuses (returns false) if shrinking would delete non-empty slots.
    public bool Resize(int newSize)
    {
        newSize = Math.Max(0, newSize);
        for (int i = newSize; i < slots.Length; i++)
        {
            if (!slots[i].IsEmpty) return false;
        }
        Array.Resize(ref slots, newSize);
        SlotChanged?.Invoke(-1);
        return true;
    }

    // Could the current items fit in newSize slots (after compacting)?
    public bool FitsIn(int newSize)
    {
        int used = 0;
        foreach (var stack in slots)
        {
            if (!stack.IsEmpty) used++;
        }
        return used <= newSize;
    }

    // Moves items out of slots >= newSize into free slots below it, so a following Resize(newSize) succeeds.
    public void Compact(int newSize)
    {
        for (int i = newSize; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty) continue;
            for (int j = 0; j < newSize; j++)
            {
                if (!slots[j].IsEmpty) continue;
                Set(j, slots[i]);
                Clear(i);
                break;
            }
        }
    }

    public IEnumerable<ItemStack> Stacks()
    {
        foreach (var stack in slots)
        {
            if (!stack.IsEmpty) yield return stack;
        }
    }

    // Drag/drop between two slots (same or different containers). amount = how many are being carried from the
    // source; less than the full stack is a split.
    //   empty target           -> move (or split into it)
    //   same stackable item    -> merge as many as fit; the rest stays in the source
    //   different item         -> swap, only when carrying the whole stack
    // Returns true if anything changed.
    public static bool MoveOrMerge(InventoryContainer from, int fromIndex, InventoryContainer to, int toIndex, int amount)
    {
        if (from == to && fromIndex == toIndex) return false;
        var source = from[fromIndex];
        if (source.IsEmpty) return false;
        amount = Math.Min(Math.Max(1, amount), source.count);
        var target = to[toIndex];

        if (target.IsEmpty)
        {
            to.Set(toIndex, source.WithCount(amount));
            from.Set(fromIndex, source.WithCount(source.count - amount));
            return true;
        }

        if (target.CanMergeWith(source))
        {
            int moved = Math.Min(amount, target.SpaceLeft);
            if (moved <= 0) return false;
            to.Set(toIndex, target.WithCount(target.count + moved));
            from.Set(fromIndex, source.WithCount(source.count - moved));
            return true;
        }

        if (amount == source.count)
        {
            from.Set(fromIndex, target);
            to.Set(toIndex, source);
            return true;
        }

        return false;
    }

    // Same-container convenience: move half (rounded up) of a stack into an empty slot.
    public bool Split(int fromIndex, int toIndex)
    {
        var source = slots[fromIndex];
        if (source.count < 2 || !slots[toIndex].IsEmpty) return false;
        return MoveOrMerge(this, fromIndex, this, toIndex, (source.count + 1) / 2);
    }

    public static bool Swap(InventoryContainer a, int aIndex, InventoryContainer b, int bIndex)
    {
        if (a == b && aIndex == bIndex) return false;
        var stackA = a[aIndex];
        a.Set(aIndex, b[bIndex]);
        b.Set(bIndex, stackA);
        return true;
    }
}
