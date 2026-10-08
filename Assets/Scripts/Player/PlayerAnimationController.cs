using UnityEngine;
using Relicfall.Combat;

namespace Relicfall.Player
{
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerAnimationController : MonoBehaviour
    {
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int VerticalSpeed = Animator.StringToHash("VerticalSpeed");
        private static readonly int Dashing = Animator.StringToHash("Dashing");
        private static readonly int Hurt = Animator.StringToHash("Hurt");
        private static readonly int Dead = Animator.StringToHash("Dead");
        private Animator animator;
        private Health health;
        private PlayerActionController actions;
        private float lastSpeed, lastVertical;
        private bool lastGrounded;
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Locomotion.Idle");
        private static readonly int RunState = Animator.StringToHash("Base Layer.Locomotion.Run");
        private static readonly int JumpState = Animator.StringToHash("Base Layer.Locomotion.Jump");
        private static readonly int FallState = Animator.StringToHash("Base Layer.Locomotion.Fall");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            health = GetComponent<Health>();
            actions = GetComponent<PlayerActionController>();
        }

        private void OnEnable()
        {
            if (health == null) return;
            health.OnDamaged += PlayHurt;
            health.OnDeath += PlayDeath;
            if (actions != null) actions.Changed += ActionChanged;
        }

        private void OnDisable()
        {
            if (health == null) return;
            health.OnDamaged -= PlayHurt;
            health.OnDeath -= PlayDeath;
            if (actions != null) actions.Changed -= ActionChanged;
        }

        private void PlayHurt(DamageInfo info)
        {
            if (!health.IsDead) animator.SetTrigger(Hurt);
        }

        private void PlayDeath() => animator.SetBool(Dead, true);
        private void ActionChanged(PlayerAction previous, PlayerAction next)
        {
            if (previous == PlayerAction.Hurt && next == PlayerAction.Free) ResumeLocomotion();
        }
        public void ResumeLocomotion()
        {
            animator.Play(lastGrounded ? (Mathf.Abs(lastSpeed) > 0.1f ? RunState : IdleState) :
                lastVertical > 0 ? JumpState : FallState, 0, 0);
        }
        public void ResetAfterRestore()
        {
            animator.SetBool(Dead, false);
            animator.SetBool(Dashing, false);
            animator.ResetTrigger(Hurt);
            ResumeLocomotion();
        }

        public void UpdateMovement(float horizontalSpeed, bool grounded, float verticalSpeed, bool dashing)
        {
            lastSpeed = horizontalSpeed;
            lastGrounded = grounded;
            lastVertical = verticalSpeed;
            animator.SetFloat(Speed, Mathf.Abs(horizontalSpeed));
            animator.SetBool(Grounded, grounded);
            animator.SetFloat(VerticalSpeed, verticalSpeed);
            animator.SetBool(Dashing, dashing);
        }
    }
}
