using Relicfall.Combat;
using UnityEngine;

namespace Relicfall.Player
{
    public sealed class WeaponRenderer : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer equippedRenderer;

        public Sprite CurrentSprite => equippedRenderer != null ? equippedRenderer.sprite : null;

        public void SetWeapon(WeaponData weapon)
        {
            if (equippedRenderer == null) return;
            equippedRenderer.sprite = weapon != null ? weapon.EquippedSprite : null;
            equippedRenderer.enabled = weapon != null;
        }

#if UNITY_EDITOR
        public void Configure(SpriteRenderer renderer) => equippedRenderer = renderer;
#endif
    }
}
