using Relicfall.Combat;
using Relicfall.Items;
using UnityEngine;

namespace Relicfall.UI
{
    public sealed class InventoryViewData
    {
        public int Health, MaxHealth, PotionCount, ActiveWeapon;
        public float DashReady;
        public WeaponData FirstWeapon, SecondWeapon;
        public string FirstName, SecondName;
        public ItemData Potion;
        public InventorySlot[] Slots;
        public Sprite Portrait;
        public int UsedSlots
        {
            get { int used = 0; foreach (var slot in Slots) if (!slot.IsEmpty) used++; return used; }
        }
    }

    public enum InventoryUIAction { None, Close, Equip, Drop, Unequip, FirstWeapon, SecondWeapon, Potion, Open }

    public static class RelicfallInventoryView
    {
        public static bool Matches(InventorySlot slot, int filter) => slot != null && (filter == 0 ||
            (!slot.IsEmpty && ((filter == 1 && slot.Item.Kind == ItemKind.Weapon) ||
                              (filter == 2 && slot.Item.Kind == ItemKind.Potion))));
    }
}
