using System;
using Relicfall.Combat;
using Relicfall.Player;
using UnityEngine;
using Relicfall.Save;

namespace Relicfall.Boss
{
    public enum BossState { Dormant, Chase, SlashWindup, Slash, FlameWindup, Flame, Recover, PhaseChange, Hurt, Dead }

    [RequireComponent(typeof(Health), typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class DragonKnightBoss : MonoBehaviour
    {
        [SerializeField] private DragonKnightFrames frames;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float chaseSpeed = 1.25f;
        [SerializeField] private BossAttackSettings attackSettings;
        private Health health;
        private Rigidbody2D body;
        private BoxCollider2D hitCollider;
        private PlayerMotor player;
        private Health playerHealth;
        private int facing = -1;
        private int attackSequence;
        private float enteredAt, stateUntil, nextAttack, nextFlameTick;
        private bool slashApplied, phaseTwo, victorySent;

        public BossState State { get; private set; } = BossState.Dormant;
        public bool PhaseTwo => phaseTwo;
        public Health Health => health;
        public int SlashCount { get; private set; }
        public int FlameCount { get; private set; }
        public event Action OnVictory;

        private void Awake()
        {
            health = GetComponent<Health>();
            body = GetComponent<Rigidbody2D>();
            hitCollider = GetComponent<BoxCollider2D>();
            if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
        }

        private void OnEnable()
        {
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
        }

        private void OnDisable()
        {
            health.OnDamaged -= OnDamaged;
            health.OnDeath -= OnDeath;
        }

        public void Activate(PlayerMotor target)
        {
            if (State != BossState.Dormant || target == null) return;
            player = target;
            playerHealth = target.GetComponent<Health>();
            nextAttack = Time.time + 0.65f;
            ChangeState(BossState.Chase, 0f);
        }

        private void Update()
        {
            Animate();
            if (State == BossState.Dormant) return;
            if (State == BossState.Dead)
            {
                if (!victorySent && Time.time >= stateUntil)
                {
                    victorySent = true;
                    OnVictory?.Invoke();
                }
                return;
            }
            if (player == null || !player.gameObject.activeInHierarchy ||
                playerHealth == null || playerHealth.IsDead)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                return;
            }
            float delta = player.transform.position.x - transform.position.x;
            Face(delta >= 0f ? 1 : -1);
            if (!phaseTwo && health.CurrentHealth <= health.MaxHealth / 2)
            {
                phaseTwo = true;
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                ChangeState(BossState.PhaseChange, 0.72f);
            }
            switch (State)
            {
                case BossState.Chase:
                    if (Time.time < nextAttack) break;
                    if (attackSequence % 2 == 1 && Mathf.Abs(delta) <= 0.75f)
                    {
                        attackSequence++;
                        slashApplied = false;
                        ChangeState(BossState.SlashWindup, attackSettings != null ?
                            (phaseTwo ? attackSettings.enragedSlashWindup : attackSettings.slashWindup) : (phaseTwo ? 0.20f : 0.30f));
                    }
                    else if (attackSequence % 2 == 0 && Mathf.Abs(delta) <= 2.1f)
                    {
                        attackSequence++;
                        ChangeState(BossState.FlameWindup, attackSettings != null ?
                            (phaseTwo ? attackSettings.enragedFlameWindup : attackSettings.flameWindup) : (phaseTwo ? 0.33f : 0.48f));
                    }
                    break;
                case BossState.SlashWindup:
                    if (Time.time >= stateUntil) ChangeState(BossState.Slash, attackSettings != null ? attackSettings.slashDuration : 0.38f);
                    break;
                case BossState.Slash:
                    if (!slashApplied && Time.time >= stateUntil - 0.23f)
                    {
                        slashApplied = true;
                        HitPlayer(attackSettings != null ? attackSettings.slashReach : 0.90f, 0.48f,
                            attackSettings != null ? (phaseTwo ? attackSettings.enragedSlashDamage : attackSettings.slashDamage) : (phaseTwo ? 22 : 17));
                    }
                    if (Time.time >= stateUntil) ChangeState(BossState.Recover, phaseTwo ? 0.22f : 0.38f);
                    break;
                case BossState.FlameWindup:
                    if (Time.time >= stateUntil) ChangeState(BossState.Flame, attackSettings != null ? attackSettings.flameDuration : 0.85f);
                    break;
                case BossState.Flame:
                    if (Time.time >= nextFlameTick)
                    {
                        nextFlameTick = Time.time + (attackSettings != null ? attackSettings.flameInterval : 0.23f);
                        HitPlayer(attackSettings != null ? (phaseTwo ? attackSettings.enragedFlameReach : attackSettings.flameReach) :
                            (phaseTwo ? 2.25f : 1.85f), 0.55f,
                            attackSettings != null ? (phaseTwo ? attackSettings.enragedFlameDamage : attackSettings.flameDamage) : (phaseTwo ? 9 : 7));
                    }
                    if (Time.time >= stateUntil) ChangeState(BossState.Recover, phaseTwo ? 0.26f : 0.42f);
                    break;
                case BossState.Hurt:
                case BossState.PhaseChange:
                case BossState.Recover:
                    if (Time.time >= stateUntil)
                    {
                        if (State == BossState.Recover)
                            nextAttack = Time.time + (phaseTwo ? 0.18f : 0.42f);
                        ChangeState(BossState.Chase, 0f);
                    }
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (State != BossState.Chase || player == null || playerHealth == null || playerHealth.IsDead)
            {
                body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
                return;
            }
            float delta = player.transform.position.x - transform.position.x;
            body.linearVelocity = new Vector2(Mathf.Abs(delta) > 0.58f ?
                Mathf.Sign(delta) * (phaseTwo ? chaseSpeed * 1.3f : chaseSpeed) : 0f,
                body.linearVelocity.y);
        }

        private void ChangeState(BossState state, float duration)
        {
            State = state;
            enteredAt = Time.time;
            stateUntil = Time.time + duration;
            if (state == BossState.Slash) SlashCount++;
            if (state == BossState.Flame) FlameCount++;
            if (state == BossState.Flame) nextFlameTick = Time.time + 0.10f;
            Animate();
        }

        private void HitPlayer(float reach, float height, int damage)
        {
            if (player == null) return;
            Vector2 offset = player.transform.position - transform.position;
            if (offset.x * facing < -0.10f || offset.x * facing > reach ||
                Mathf.Abs(offset.y) > height) return;
            playerHealth?.TakeDamage(new DamageInfo(damage,
                new Vector2(facing * 1.7f, 0.45f), gameObject, player.transform.position));
        }

        private void OnDamaged(DamageInfo info)
        {
            if (health.IsDead || State == BossState.PhaseChange || State == BossState.Dormant) return;
            if (State == BossState.Chase || State == BossState.Recover)
                ChangeState(BossState.Hurt, 0.18f);
        }

        private void OnDeath()
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            hitCollider.enabled = false;
            ChangeState(BossState.Dead, 1.15f);
        }

        private void Face(int direction)
        {
            facing = direction;
            if (visualRoot != null)
            {
                visualRoot.localPosition = new Vector3(facing * 0.55f, 0.65f, 0f);
                visualRoot.localScale = new Vector3(facing * 3f, 3f, 1f);
            }
        }

        private void Animate()
        {
            if (frames == null || visual == null) return;
            Sprite[] sequence;
            float fps;
            switch (State)
            {
                case BossState.Chase: sequence = frames.Run; fps = phaseTwo ? 13f : 10f; break;
                case BossState.SlashWindup:
                case BossState.Slash: sequence = frames.Slash; fps = phaseTwo ? 16f : 13f; break;
                case BossState.FlameWindup:
                case BossState.Flame: sequence = frames.Flame; fps = 16f; break;
                case BossState.Hurt:
                case BossState.PhaseChange: sequence = frames.Hurt; fps = 9f; break;
                case BossState.Dead: sequence = frames.Death; fps = 12f; break;
                default: sequence = frames.Idle; fps = 4f; break;
            }
            if (sequence == null || sequence.Length == 0) return;
            bool repeat = State == BossState.Dormant || State == BossState.Chase ||
                          State == BossState.Recover;
            int index = Mathf.FloorToInt((repeat ? Time.time : Time.time - enteredAt) * fps);
            visual.sprite = sequence[repeat ? index % sequence.Length :
                Mathf.Clamp(index, 0, sequence.Length - 1)];
        }

#if UNITY_EDITOR
        public void ConfigureAttacks(BossAttackSettings settings) => attackSettings = settings;
        public void Configure(DragonKnightFrames animationFrames, SpriteRenderer renderer,
            Transform spriteRoot) { frames = animationFrames; visual = renderer; visualRoot = spriteRoot; }
#endif

        public SavedBoss CaptureProgress(bool started) => new SavedBoss { health = health.CurrentHealth,
            phaseTwo = phaseTwo, started = started || State != BossState.Dormant, defeated = State == BossState.Dead,
            position = transform.position };
        public void RestoreProgress(SavedBoss save, PlayerMotor target)
        {
            if (save == null) return;
            player = target;
            playerHealth = target != null ? target.GetComponent<Health>() : null;
            transform.position = save.position;
            body.position = save.position;
            body.linearVelocity = Vector2.zero;
            health.Restore(save.defeated ? 0 : save.health, save.defeated);
            phaseTwo = save.phaseTwo || (!save.defeated && health.CurrentHealth <= health.MaxHealth / 2);
            victorySent = false;
            slashApplied = false;
            attackSequence = 0;
            body.simulated = !save.defeated;
            hitCollider.enabled = !save.defeated;
            nextAttack = Time.time + 0.65f;
            ChangeState(save.defeated ? BossState.Dead : save.started ? BossState.Chase : BossState.Dormant,
                save.defeated ? 1.15f : 0);
        }
    }
}
