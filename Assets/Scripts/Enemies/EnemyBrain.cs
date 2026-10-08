using Relicfall.Combat;
using Relicfall.Player;
using UnityEngine;

namespace Relicfall.Enemies
{
    public enum EnemyState { Patrol, Chase, Attack, Hurt, Death }
    public enum EnemyKind { Swordsman, Gunner }

    [RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(Animator))]
    [RequireComponent(typeof(EnemyPerception))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        [SerializeField] private EnemyKind kind;
        [SerializeField] private Transform visual;
        [SerializeField] private EnemyProjectile projectilePrefab;
        [SerializeField] private LayerMask groundMask;
        [SerializeField, Min(0.1f)] private float patrolHalfWidth = 1.5f;
        [SerializeField, Min(0.1f)] private float sightRange = 3.4f;
        [SerializeField, Min(0.1f)] private float attackRange = 0.55f;
        [SerializeField, Min(0.1f)] private float patrolSpeed = 0.7f;
        [SerializeField, Min(0.1f)] private float chaseSpeed = 1.25f;
        [SerializeField, Min(0.1f)] private float attackCooldown = 1.1f;
        [SerializeField, Min(1)] private int damage = 8;

        private Rigidbody2D body;
        private Health health;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private Sprite originalSprite;
        private Vector3 visualRestPosition;
        private Transform target;
        private Health targetHealth;
        private EnemyPerception perception;
        public string SaveId { get; private set; }
        public Health Health => health;
        private Collider2D[] colliders;
        private float homeX;
        private float nextAttack;
        private float stateUntil;
        private int direction = 1;
        private bool attackApplied;
        private float hurtPulseUntil;
        private float hurtDirection;

        public EnemyState State { get; private set; } = EnemyState.Patrol;
        public EnemyKind Kind => kind;
        public int AttackCount { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            perception = GetComponent<EnemyPerception>();
            animator = GetComponent<Animator>();
            spriteRenderer = visual != null ? visual.GetComponent<SpriteRenderer>() : null;
            originalSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            visualRestPosition = visual != null ? visual.localPosition : Vector3.zero;
            colliders = GetComponentsInChildren<Collider2D>();
        }

        private void OnEnable()
        {
            health.OnDamaged += HandleDamage;
            health.OnDeath += HandleDeath;
            homeX = transform.position.x;
            direction = 1;
            target = null;
            targetHealth = null;
            nextAttack = 0f;
            stateUntil = 0f;
            attackApplied = false;
            hurtPulseUntil = 0f;
            AttackCount = 0;
            State = EnemyState.Patrol;
            Face(1);
            if (spriteRenderer != null) spriteRenderer.sprite = originalSprite;
            if (visual != null) visual.localPosition = visualRestPosition;
            if (animator != null)
            {
                animator.Rebind();
                animator.Play("Patrol", 0, 0f);
            }
            if (body != null) body.linearVelocity = Vector2.zero;
        }

        private void OnDisable()
        {
            health.OnDamaged -= HandleDamage;
            health.OnDeath -= HandleDeath;
        }

        private void Update()
        {
            UpdateHurtPulse();
            if (State == EnemyState.Death) return;
            perception.RefreshTarget();
            target = perception.Target;
            targetHealth = perception.TargetHealth;

            if (State == EnemyState.Hurt)
            {
                if (Time.time >= stateUntil) SetState(EnemyState.Patrol);
                return;
            }
            if (State == EnemyState.Attack)
            {
                if (!attackApplied && Time.time >= stateUntil - 0.2f)
                {
                    attackApplied = true;
                    ApplyAttack();
                }
                if (Time.time >= stateUntil) SetState(CanSeePlayer() ? EnemyState.Chase : EnemyState.Patrol);
                return;
            }

            bool visible = CanSeePlayer();
            if (visible && Mathf.Abs(target.position.x - transform.position.x) <= attackRange)
            {
                Face(target.position.x > transform.position.x ? 1 : -1);
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + attackCooldown;
                    attackApplied = false;
                    stateUntil = Time.time + 0.42f;
                    SetState(EnemyState.Attack);
                }
                else SetState(EnemyState.Chase);
            }
            else if (visible) SetState(EnemyState.Chase);
            else SetState(EnemyState.Patrol);
        }

        private void FixedUpdate()
        {
            if (State != EnemyState.Patrol && State != EnemyState.Chase)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                return;
            }

            if (State == EnemyState.Chase && target != null)
                Face(target.position.x > transform.position.x ? 1 : -1);
            else if ((direction > 0 && transform.position.x >= homeX + patrolHalfWidth) ||
                     (direction < 0 && transform.position.x <= homeX - patrolHalfWidth))
                Face(-direction);

            if (!SafeAhead())
            {
                if (State == EnemyState.Patrol) Face(-direction);
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                return;
            }
            if (State == EnemyState.Chase && target != null &&
                Mathf.Abs(target.position.x - transform.position.x) <= attackRange)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                return;
            }
            body.linearVelocity = new Vector2(direction * (State == EnemyState.Chase ? chaseSpeed : patrolSpeed), body.linearVelocity.y);
        }

        private bool SafeAhead() => perception.SafeAhead(direction, groundMask);
        private bool CanSeePlayer() => perception.CanSee(homeX, patrolHalfWidth, sightRange, groundMask);

        private void ApplyAttack()
        {
            AttackCount++;
            if (target == null) return;
            if (kind == EnemyKind.Gunner)
            {
                if (projectilePrefab != null)
                {
                    Vector3 origin = transform.position + new Vector3(direction * 0.2f, -0.18f, 0f);
                    if (ProjectilePool.Instance != null)
                        ProjectilePool.Instance.Fire(origin, direction, damage, gameObject);
                    else
                    {
                        EnemyProjectile shot = Instantiate(projectilePrefab, origin, Quaternion.identity);
                        shot.Launch(direction, damage, gameObject);
                    }
                }
                return;
            }
            if (Mathf.Abs(target.position.x - transform.position.x) > attackRange + 0.12f ||
                Mathf.Abs(target.position.y - transform.position.y) > 0.55f) return;
            targetHealth?.TakeDamage(new DamageInfo(damage,
                new Vector2(direction * 1.5f, 0.5f), gameObject, target.position));
        }

        private void HandleDamage(DamageInfo info)
        {
            if (health.IsDead) return;
            hurtPulseUntil = Time.time + 0.26f;
            hurtDirection = info.Knockback.x >= 0f ? 1f : -1f;
            stateUntil = Time.time + 0.3f;
            SetState(EnemyState.Hurt);
        }

        private void UpdateHurtPulse()
        {
            if (visual == null) return;
            float pulse = Mathf.Clamp01((hurtPulseUntil - Time.time) / 0.26f);
            visual.localPosition = visualRestPosition + Vector3.right * (hurtDirection * pulse * 0.085f);
            visual.localScale = new Vector3(direction * (1f + pulse * 0.23f),
                1f - pulse * 0.16f, 1f);
        }

        private void HandleDeath()
        {
            State = EnemyState.Death;
            body.linearVelocity = Vector2.zero;
            foreach (Collider2D hit in GetComponentsInChildren<Collider2D>()) hit.enabled = false;
            animator.Play("Death", 0, 0f);
        }

        private void SetState(EnemyState next)
        {
            if (State == next) return;
            State = next;
            animator.Play(next == EnemyState.Chase ? "Move" : next.ToString(), 0, 0f);
        }

        private void Face(int newDirection)
        {
            direction = newDirection;
            if (visual != null) visual.localScale = new Vector3(direction, 1f, 1f);
        }

        public void ResetForSpawn()
        {
            health.ResetHealth();
            body.simulated = true;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            foreach (Collider2D hit in colliders) hit.enabled = true;
            target = null;
            targetHealth = null;
            SaveId = null;
            nextAttack = 0f;
            stateUntil = 0f;
            attackApplied = false;
            State = EnemyState.Patrol;
            if (spriteRenderer != null) spriteRenderer.sprite = originalSprite;
            if (visual != null) visual.localPosition = visualRestPosition;
            hurtPulseUntil = 0f;
            Face(1);
        }

        public void AssignSaveId(string id) => SaveId = id;
        public void RestoreProgress(int life, Vector3 position, float patrolHome)
        {
            health.Restore(life);
            transform.position = position;
            body.position = position;
            body.linearVelocity = Vector2.zero;
            homeX = patrolHome;
            State = EnemyState.Patrol;
            animator.Play("Patrol", 0, 0);
        }

#if UNITY_EDITOR
        public void Configure(EnemyKind enemyKind, Transform enemyVisual, LayerMask ground,
            EnemyProjectile shot, float range, float sight, float width, int attackDamage)
        {
            kind = enemyKind;
            visual = enemyVisual;
            groundMask = ground;
            projectilePrefab = shot;
            attackRange = range;
            sightRange = sight;
            patrolHalfWidth = width;
            damage = attackDamage;
        }
#endif
    }
}
