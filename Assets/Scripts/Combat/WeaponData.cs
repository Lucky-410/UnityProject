using UnityEngine;

namespace Relicfall.Combat
{
    public enum WeaponType { Sword, Axe, HeavyBlade }

    [CreateAssetMenu(menuName = "Relicfall/Weapon", fileName = "Weapon")]
    public sealed class WeaponData : ScriptableObject
    {
        [SerializeField] private string weaponName;
        [SerializeField] private string stableId, legacyName;
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite worldSprite;
        [SerializeField] private Sprite equippedSprite;
        [SerializeField, Min(1)] private int damage = 10;
        [SerializeField, Min(0.1f)] private float attackSpeed = 1f;
        [SerializeField, Min(0.1f)] private float attackRange = 0.4f;
        [SerializeField, Min(0f)] private float knockback = 1f;
        [SerializeField] private WeaponType weaponType;
        [SerializeField] private GameObject hitVFX;
        [SerializeField] private AnimatorOverrideController animatorOverride;

        public string WeaponName => weaponName;
        public string StableId => string.IsNullOrEmpty(stableId) ? name : stableId;
        public string LegacyName => string.IsNullOrEmpty(legacyName) ? name : legacyName;
        public Sprite Icon => icon;
        public Sprite WorldSprite => worldSprite;
        public Sprite EquippedSprite => equippedSprite;
        public int Damage => damage;
        public float AttackSpeed => attackSpeed;
        public float AttackRange => attackRange;
        public float Knockback => knockback;
        public WeaponType WeaponType => weaponType;
        public GameObject HitVFX => hitVFX;
        public AnimatorOverrideController AnimatorOverride => animatorOverride;

#if UNITY_EDITOR
        public void ConfigureIdentity(string id, string oldName) { stableId = id; legacyName = oldName; }
        public void Configure(string displayName, Sprite artwork, int newDamage, float speed,
            float range, float force, WeaponType type)
        {
            weaponName = displayName;
            icon = artwork;
            worldSprite = artwork;
            equippedSprite = artwork;
            damage = Mathf.Max(1, newDamage);
            attackSpeed = Mathf.Max(0.1f, speed);
            attackRange = Mathf.Max(0.1f, range);
            knockback = Mathf.Max(0f, force);
            weaponType = type;
        }
#endif
    }
}
