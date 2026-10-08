using Relicfall.Combat;
using UnityEngine;

namespace Relicfall.Items
{
    public enum ItemKind { Weapon, Potion, Material }

    [CreateAssetMenu(menuName = "Relicfall/物品数据", fileName = "Item")]
    public sealed class ItemData : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private string stableId, legacyName;
        [SerializeField] private ItemKind kind;
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite worldSprite;
        [SerializeField, Min(1)] private int maxStack = 1;
        [SerializeField] private WeaponData weapon;
        [SerializeField, Min(0)] private int healAmount;

        public string DisplayName => displayName;
        public string StableId => string.IsNullOrEmpty(stableId) ? name : stableId;
        public string LegacyName => string.IsNullOrEmpty(legacyName) ? name : legacyName;
        public ItemKind Kind => kind;
        public Sprite Icon => icon;
        public Sprite WorldSprite => worldSprite != null ? worldSprite : icon;
        public int MaxStack => maxStack;
        public WeaponData Weapon => weapon;
        public int HealAmount => healAmount;

#if UNITY_EDITOR
        public void ConfigureIdentity(string id, string oldName) { stableId = id; legacyName = oldName; }
        public void Configure(string title, ItemKind itemKind, Sprite artwork,
            int stackLimit, WeaponData weaponData = null, int healing = 0)
        {
            displayName = title;
            kind = itemKind;
            icon = artwork;
            worldSprite = artwork;
            maxStack = Mathf.Max(1, stackLimit);
            weapon = weaponData;
            healAmount = Mathf.Max(0, healing);
        }
#endif
    }
}
