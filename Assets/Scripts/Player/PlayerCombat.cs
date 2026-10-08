using Relicfall.Combat;
using UnityEngine;
using Relicfall.Audio;

namespace Relicfall.Player
{
    [RequireComponent(typeof(Animator), typeof(PlayerInputReader), typeof(Health))]
    [RequireComponent(typeof(PlayerActionController))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private AttackHitbox2D hitbox;
        [SerializeField, Min(1)] private int baseDamage = 14;
        [SerializeField, Min(0.1f)] private float attackRange = 0.42f;
        [SerializeField, Min(0f)] private float knockback = 1.6f;

        private Animator animator;
        private PlayerInputReader input;
        private Health health;
        private PlayerActionController actions;
        private PlayerAnimationController animationController;
        private static readonly int[] AttackStates = { Animator.StringToHash("Base Layer.Combat.Attack1"),
            Animator.StringToHash("Base Layer.Combat.Attack2"), Animator.StringToHash("Base Layer.Combat.Attack3") };
        private static readonly int[] AttackNames = { Animator.StringToHash("Attack1"), Animator.StringToHash("Attack2"), Animator.StringToHash("Attack3") };
        private bool attacking;
        private bool comboOpen;
        private int bufferedAttacks;
        private float attackBeganAt;
        private int comboStep;
        private WeaponData weapon;

        public int ComboStep => comboStep;
        public bool IsAttacking => attacking;
        public int AttackCount { get; private set; }
        public WeaponData CurrentWeapon => weapon;
        public int CurrentDamage => weapon != null ? weapon.Damage : baseDamage;
        public float CurrentAttackRange => weapon != null ? weapon.AttackRange : attackRange;
        public float CurrentAttackSpeed => weapon != null ? weapon.AttackSpeed : 1f;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            input = GetComponent<PlayerInputReader>();
            health = GetComponent<Health>();
            actions = GetComponent<PlayerActionController>();
            animationController = GetComponent<PlayerAnimationController>();
        }

        private void OnEnable() => actions.Changed += ActionChanged;

        private void OnDisable()
        {
            actions.Changed -= ActionChanged;
            CancelAttack();
            actions.Finish(PlayerAction.Attack);
        }

        private void Update()
        {
            if (health.IsDead || !actions.CanControl) { input.ConsumeAttack(); return; }
            if (input.ConsumeAttack())
            {
                if (!attacking) StartAttack(1);
                else
                {
                    bufferedAttacks = Mathf.Min(bufferedAttacks + 1, 4);
                    if (comboOpen && comboStep < 3)
                    {
                        bufferedAttacks--;
                        StartAttack(comboStep + 1);
                    }
                }
            }
            if (!attacking || Time.time <= attackBeganAt) return;
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            bool animationEnded = comboStep > 0 && state.shortNameHash == AttackNames[comboStep - 1] && state.normalizedTime >= 1f;
            float latestFinish = 0.85f / Mathf.Max(0.25f, CurrentAttackSpeed);
            if (animationEnded || Time.time - attackBeganAt > latestFinish) AttackFinished();
        }

        private void StartAttack(int step)
        {
            if (!actions.TryAttack()) return;
            if (hitbox != null) hitbox.End();
            attacking = true;
            comboOpen = false;
            comboStep = step;
            attackBeganAt = Time.time;
            AttackCount++;
            animator.Play(AttackStates[step - 1], 0, 0f);
            AudioDirector.Instance?.PlayAttack();
        }

        // 由三段攻击动画的事件调用。
        public void EnableHitbox()
        {
            if (!attacking || actions.Action != PlayerAction.Attack || hitbox == null) return;
            int damage = Mathf.RoundToInt(CurrentDamage * (comboStep == 3 ? 1.35f : 1f));
            hitbox.Begin(gameObject, damage, CurrentAttackRange,
                weapon != null ? weapon.Knockback : knockback);
        }

        public void DisableHitbox() => hitbox?.End();

        public void OpenComboWindow()
        {
            if (!attacking || actions.Action != PlayerAction.Attack) return;
            comboOpen = true;
            if (bufferedAttacks > 0 && comboStep < 3)
            {
                bufferedAttacks--;
                StartAttack(comboStep + 1);
            }
        }

        public void CloseComboWindow() => comboOpen = false;

        public void AttackFinished()
        {
            if (!attacking) return;
            hitbox?.End();
            if (bufferedAttacks > 0)
            {
                bufferedAttacks--;
                StartAttack(comboStep < 3 ? comboStep + 1 : 1);
                return;
            }
            CancelAttack();
            actions.Finish(PlayerAction.Attack);
            animationController.ResumeLocomotion();
        }

        private void ActionChanged(PlayerAction previous, PlayerAction next)
        {
            if (previous == PlayerAction.Attack && next != PlayerAction.Attack) CancelAttack();
        }
        private void CancelAttack()
        {
            attacking = false;
            comboOpen = false;
            bufferedAttacks = 0;
            comboStep = 0;
            hitbox?.End();
        }

        public void SetWeapon(WeaponData newWeapon)
        {
            weapon = newWeapon;
            if (animator == null) animator = GetComponent<Animator>();
            animator.SetFloat("AttackSpeed", CurrentAttackSpeed);
        }

#if UNITY_EDITOR
        public void Configure(AttackHitbox2D target) => hitbox = target;
#endif
    }
}
