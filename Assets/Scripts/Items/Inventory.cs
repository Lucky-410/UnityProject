using System;
using Relicfall.Combat;
using Relicfall.Player;
using UnityEngine;

namespace Relicfall.Items
{
    [RequireComponent(typeof(EquipmentController), typeof(Health))]
    public sealed class Inventory : MonoBehaviour
    {
        [SerializeField, Range(8, 12)] private int capacity = 10;
        [SerializeField] private ItemData[] weaponItems;
        [SerializeField] private WorldItem worldItemPrefab;
        [SerializeField] private PotionDrinkVisual drinkVisual;
        [SerializeField] private ItemData equippedPotion;

        private InventorySlot[] slots;
        private EquipmentController equipment;
        private Health health;

        public int Capacity => slots != null ? slots.Length : capacity;
        public ItemData EquippedPotion => equippedPotion;
        public string EquippedWeaponName(int index)
        {
            WeaponData weapon = index == 0 ? equipment.Slot1 : equipment.Slot2;
            ItemData item = FindWeaponItem(weapon);
            return item != null ? item.DisplayName : "空";
        }
        public event Action OnChanged;
        public event Action<string> OnFeedback;

        private void Awake()
        {
            slots = new InventorySlot[capacity];
            for (int i = 0; i < slots.Length; i++) slots[i] = new InventorySlot();
            equipment = GetComponent<EquipmentController>();
            health = GetComponent<Health>();
        }

        public InventorySlot GetSlot(int index) => index >= 0 && index < slots.Length ? slots[index] : null;

        public void RestoreSlots(ItemData[] items, int[] counts, ItemData potion)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].Clear();
                if (items == null || counts == null || i >= items.Length || i >= counts.Length ||
                    items[i] == null || counts[i] <= 0) continue;
                slots[i].Set(items[i], Mathf.Min(counts[i], items[i].MaxStack));
            }
            equippedPotion = potion != null && potion.Kind == ItemKind.Potion ? potion : null;
            OnChanged?.Invoke();
        }

        public int Count(ItemData item)
        {
            int total = 0;
            foreach (InventorySlot slot in slots)
                if (slot.Item == item) total += slot.Count;
            return total;
        }

        public bool CanAdd(ItemData item, int count, int freeIndex = -1)
        {
            if (item == null || count <= 0) return false;
            int room = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                InventorySlot slot = slots[i];
                if (i == freeIndex || slot.IsEmpty) room += item.MaxStack;
                else if (slot.Item == item) room += item.MaxStack - slot.Count;
                if (room >= count) return true;
            }
            return false;
        }

        public bool TryAdd(ItemData item, int count = 1)
        {
            if (!CanAdd(item, count))
            {
                OnFeedback?.Invoke("背包已满");
                return false;
            }
            int remaining = count;
            foreach (InventorySlot slot in slots)
            {
                if (slot.Item != item || slot.Count >= item.MaxStack) continue;
                int amount = Mathf.Min(remaining, item.MaxStack - slot.Count);
                slot.Count += amount;
                remaining -= amount;
                if (remaining == 0) break;
            }
            if (remaining > 0)
            {
                foreach (InventorySlot slot in slots)
                {
                    if (!slot.IsEmpty) continue;
                    int amount = Mathf.Min(remaining, item.MaxStack);
                    slot.Set(item, amount);
                    remaining -= amount;
                    if (remaining == 0) break;
                }
            }
            OnChanged?.Invoke();
            return true;
        }

        public bool TryRemoveAt(int index, int count = 1)
        {
            InventorySlot slot = GetSlot(index);
            if (slot == null || slot.IsEmpty || count <= 0 || slot.Count < count) return false;
            slot.Count -= count;
            if (slot.Count == 0) slot.Clear();
            OnChanged?.Invoke();
            return true;
        }

        public bool TryRemove(ItemData item, int count = 1)
        {
            if (Count(item) < count || count <= 0) return false;
            int remaining = count;
            for (int i = 0; i < slots.Length && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.Item != item) continue;
                int amount = Mathf.Min(remaining, slot.Count);
                TryRemoveAt(i, amount);
                remaining -= amount;
            }
            return true;
        }

        public bool Equip(int index)
        {
            InventorySlot slot = GetSlot(index);
            if (slot == null || slot.IsEmpty) return false;
            if (slot.Item.Kind == ItemKind.Potion)
            {
                equippedPotion = slot.Item;
                OnChanged?.Invoke();
                OnFeedback?.Invoke("药水已放入快捷栏");
                return true;
            }
            if (slot.Item.Kind != ItemKind.Weapon || slot.Item.Weapon == null) return false;
            WeaponData previous = equipment.CurrentWeapon;
            ItemData previousItem = FindWeaponItem(previous);
            if (previous != null && (previousItem == null ||
                !CanAdd(previousItem, 1, slot.Count == 1 ? index : -1)))
            {
                OnFeedback?.Invoke("背包没有空间放回旧武器");
                return false;
            }
            WeaponData replacement = slot.Item.Weapon;
            TryRemoveAt(index);
            equipment.EquipWeapon(equipment.ActiveSlot, replacement);
            if (previousItem != null) TryAdd(previousItem);
            OnFeedback?.Invoke("武器已装备");
            return true;
        }

        public bool UnequipWeapon()
        {
            WeaponData weapon = equipment.CurrentWeapon;
            ItemData item = FindWeaponItem(weapon);
            if (item == null || !CanAdd(item, 1))
            {
                OnFeedback?.Invoke("背包已满，无法卸下武器");
                return false;
            }
            equipment.UnequipWeapon(equipment.ActiveSlot);
            TryAdd(item);
            OnFeedback?.Invoke("武器已卸下");
            return true;
        }

        public bool Drop(int index, int count = 1)
        {
            InventorySlot slot = GetSlot(index);
            if (slot == null || slot.IsEmpty || worldItemPrefab == null || count > slot.Count)
                return false;
            ItemData item = slot.Item;
            if (!TryRemoveAt(index, count)) return false;
            WorldItem.Spawn(worldItemPrefab, item, count,
                transform.position + new Vector3(0.45f, 0.02f, 0f));
            OnFeedback?.Invoke("物品已丢弃");
            return true;
        }

        public bool UsePotion()
        {
            if (equippedPotion == null || Count(equippedPotion) == 0 ||
                health.IsDead || health.CurrentHealth >= health.MaxHealth)
            {
                OnFeedback?.Invoke("当前无法使用药水");
                return false;
            }
            if (health.Heal(equippedPotion.HealAmount) <= 0) return false;
            TryRemove(equippedPotion);
            drinkVisual?.Play();
            OnFeedback?.Invoke("已使用药水");
            return true;
        }

        private ItemData FindWeaponItem(WeaponData weapon)
        {
            if (weapon == null || weaponItems == null) return null;
            foreach (ItemData item in weaponItems)
                if (item != null && item.Weapon == weapon) return item;
            return null;
        }

#if UNITY_EDITOR
        public void Configure(ItemData[] weapons, WorldItem worldPrefab, PotionDrinkVisual visual)
        {
            weaponItems = weapons;
            worldItemPrefab = worldPrefab;
            drinkVisual = visual;
        }
#endif
    }
}
