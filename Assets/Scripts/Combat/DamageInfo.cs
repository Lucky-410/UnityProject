using UnityEngine;

namespace Relicfall.Combat
{
    public readonly struct DamageInfo
    {
        public readonly int Damage;
        public readonly Vector2 Knockback;
        public readonly GameObject Source;
        public readonly Vector2 HitPoint;

        public DamageInfo(int damage, Vector2 knockback, GameObject source, Vector2 hitPoint)
        {
            Damage = damage;
            Knockback = knockback;
            Source = source;
            HitPoint = hitPoint;
        }
    }
}
