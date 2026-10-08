using System.Collections.Generic;
using UnityEngine;

namespace Relicfall.Combat
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class AttackHitbox2D : MonoBehaviour
    {
        private readonly HashSet<Health> hitThisAttack = new HashSet<Health>();
        private BoxCollider2D hitCollider;
        private GameObject owner;
        private int damage;
        private float knockback;
        [SerializeField] private float verticalOffset = -0.22f;

        private void Awake()
        {
            hitCollider = GetComponent<BoxCollider2D>();
            hitCollider.isTrigger = true;
            hitCollider.enabled = false;
        }

        public void Begin(GameObject source, int attackDamage, float range, float knockbackForce)
        {
            owner = source;
            damage = attackDamage;
            knockback = knockbackForce;
            hitThisAttack.Clear();
            hitCollider.size = new Vector2(range, 0.22f);
            transform.localPosition = new Vector3(0.15f + range * 0.5f, verticalOffset, 0f);
            hitCollider.enabled = true;
        }

        public void End()
        {
            if (hitCollider != null) hitCollider.enabled = false;
        }

        private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
        private void OnTriggerStay2D(Collider2D other) => TryHit(other);

        private void TryHit(Collider2D other)
        {
            if (owner == null || other.transform.root == owner.transform.root) return;
            Health target = other.GetComponentInParent<Health>();
            if (target == null || hitThisAttack.Contains(target)) return;

            float direction = Mathf.Sign(target.transform.position.x - owner.transform.position.x);
            var info = new DamageInfo(damage, new Vector2(direction * knockback, knockback * 0.3f),
                owner, other.ClosestPoint(transform.position));
            if (target.TakeDamage(info)) hitThisAttack.Add(target);
        }

#if UNITY_EDITOR
        public void ConfigureVerticalOffset(float offset) => verticalOffset = offset;
#endif
    }
}
