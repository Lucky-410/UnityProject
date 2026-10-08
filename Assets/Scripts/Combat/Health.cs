using System;
using UnityEngine;

namespace Relicfall.Combat
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField, Min(0f)] private float invulnerabilitySeconds = 0.2f;

        private Rigidbody2D body;
        private float invulnerableUntil;

        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0;

        public event Action<int, int> OnHealthChanged;
        public event Action<DamageInfo> OnDamaged;
        public event Action OnDeath;
        public static event Action<Health, DamageInfo> OnAnyDamaged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => OnAnyDamaged = null;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            CurrentHealth = maxHealth;
        }

        public bool TakeDamage(DamageInfo info)
        {
            if (IsDead || info.Damage <= 0 || Time.time < invulnerableUntil) return false;

            CurrentHealth = Mathf.Max(0, CurrentHealth - info.Damage);
            invulnerableUntil = Time.time + invulnerabilitySeconds;
            if (body != null && body.bodyType == RigidbodyType2D.Dynamic)
                body.AddForce(info.Knockback, ForceMode2D.Impulse);
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            OnDamaged?.Invoke(info);
            OnAnyDamaged?.Invoke(this, info);
            if (IsDead) OnDeath?.Invoke();
            return true;
        }

        public int Heal(int amount)
        {
            if (IsDead || amount <= 0) return 0;
            int before = CurrentHealth;
            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            if (CurrentHealth != before) OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
            return CurrentHealth - before;
        }

        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
            invulnerableUntil = 0f;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        public void Restore(int value, bool allowDead = false)
        {
            CurrentHealth = Mathf.Clamp(value, allowDead ? 0 : 1, maxHealth);
            invulnerableUntil = 0f;
            OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

#if UNITY_EDITOR
        public void Configure(int maximum, float invulnerability)
        {
            maxHealth = Mathf.Max(1, maximum);
            invulnerabilitySeconds = Mathf.Max(0f, invulnerability);
        }
#endif
    }
}
