using System;

namespace Relicfall.Items
{
    [Serializable]
    public sealed class InventorySlot
    {
        public ItemData Item;
        public int Count;
        public bool IsEmpty => Item == null || Count <= 0;

        public void Set(ItemData item, int count)
        {
            Item = item;
            Count = count;
        }

        public void Clear()
        {
            Item = null;
            Count = 0;
        }
    }
}
