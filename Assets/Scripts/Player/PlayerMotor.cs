using UnityEngine;
using Relicfall.Combat;
using Relicfall.Audio;

namespace Relicfall.Player
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(GroundDetector))]
    [RequireComponent(typeof(PlayerInputReader), typeof(PlayerAnimationController))]
    [RequireComponent(typeof(PlayerActionController), typeof(PlayerPickupDetector))]
    [RequireComponent(typeof(PlayerContext))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("Run")]
        [SerializeField, Min(0f)] private float runSpeed = 2.6f;
        [SerializeField, Min(0f)] private float groundAcceleration = 24f;
        [SerializeField, Min(0f)] private float airAcceleration = 13f;
        [SerializeField] private Transform visualRoot;

        [Header("Jump")]
        [SerializeField, Min(0f)] private float jumpSpeed = 7.5f;
        [SerializeField, Min(0f)] private float coyoteTime = 0.12f;
        [SerializeField, Min(0f)] private float jumpBufferTime = 0.12f;

        [Header("Dash")]
        [SerializeField, Min(0f)] private float dashSpeed = 6.5f;
        [SerializeField, Min(0f)] private float dashDuration = 0.16f;
        [SerializeField, Min(0f)] private float dashCooldown = 0.55f;

        private Rigidbody2D body;
        private GroundDetector ground;
        private PlayerInputReader input;
        private PlayerAnimationController animationController;
        private Health health;
        private PlayerActionController actions;
        private float normalGravity;
        private float coyoteRemaining;
        private float jumpBufferRemaining;
        private float dashRemaining;
        private float dashCooldownRemaining;
        private int facing = 1;
        private int dashDirection = 1;
        private bool dashQueued;
        private bool jumpedSinceGrounded;
        private bool wasGrounded;

        public bool IsDashing => dashRemaining > 0f;
        public float DashReadyRatio => IsDashing ? 0f : 1f - Mathf.Clamp01(dashCooldownRemaining / Mathf.Max(0.01f, dashCooldown));
        public bool IsGrounded => ground != null && ground.IsGrounded && !jumpedSinceGrounded;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            ground = GetComponent<GroundDetector>();
            input = GetComponent<PlayerInputReader>();
            animationController = GetComponent<PlayerAnimationController>();
            health = GetComponent<Health>();
            actions = GetComponent<PlayerActionController>();
            normalGravity = body.gravityScale;
        }

        private void OnEnable()
        {
            if (health != null) health.OnDeath += HandleDeath;
            actions.Changed += ActionChanged;
        }

        private void Update()
        {
            if (input.ConsumeJump() && actions.CanControl && !IsDashing) jumpBufferRemaining = jumpBufferTime;
            if (input.ConsumeDash() && actions.CanControl) dashQueued = true;

            if (actions.CanControl && !IsDashing && Mathf.Abs(input.Move) > 0.1f)
            {
                facing = input.Move > 0f ? 1 : -1;
                if (visualRoot != null)
                {
                    Vector3 scale = visualRoot.localScale;
                    scale.x = Mathf.Abs(scale.x) * facing;
                    visualRoot.localScale = scale;
                }
            }

            animationController.UpdateMovement(body.linearVelocity.x, IsGrounded,
                body.linearVelocity.y, IsDashing);
            actions.SetLocomotion(IsGrounded, body.linearVelocity.y);
        }

        private void FixedUpdate()
        {
            if (!actions.CanControl)
            {
                body.gravityScale = normalGravity;
                return; // 保留受击冲量，不用移动速度覆盖它。
            }
            ground.Refresh();
            if (!ground.IsGrounded) jumpedSinceGrounded = false;
            bool grounded = !jumpedSinceGrounded && ground.HasSupport &&
                (ground.IsGrounded || wasGrounded) && (wasGrounded || body.linearVelocity.y <= 0.1f);
            if (grounded && ground.Distance > 0.005f)
                body.position += Vector2.down * ground.Distance;
            if (grounded)
                coyoteRemaining = coyoteTime;
            else
                coyoteRemaining = Mathf.Max(0f, coyoteRemaining - Time.fixedDeltaTime);

            jumpBufferRemaining = Mathf.Max(0f, jumpBufferRemaining - Time.fixedDeltaTime);
            dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - Time.fixedDeltaTime);

            if (dashRemaining > 0f)
            {
                dashRemaining -= Time.fixedDeltaTime;
                body.linearVelocity = GroundVelocity(dashDirection * dashSpeed, grounded);
                wasGrounded = grounded;
                if (dashRemaining <= 0f)
                {
                    body.gravityScale = normalGravity;
                    actions.Finish(PlayerAction.Dash);
                }
                return;
            }

            if (dashQueued && dashCooldownRemaining <= 0f && actions.TryDash())
            {
                dashRemaining = dashDuration;
                dashCooldownRemaining = dashCooldown;
                dashDirection = facing;
                body.gravityScale = 0f;
                body.linearVelocity = GroundVelocity(dashDirection * dashSpeed, grounded);
                wasGrounded = grounded;
                dashQueued = false;
                AudioDirector.Instance?.PlayDash();
                return;
            }
            dashQueued = false;

            if (jumpBufferRemaining > 0f && coyoteRemaining > 0f)
            {
                body.linearVelocity = new Vector2(body.linearVelocity.x, jumpSpeed);
                jumpBufferRemaining = 0f;
                coyoteRemaining = 0f;
                jumpedSinceGrounded = true;
                grounded = false;
            }

            // 贴地移动沿坡面切线，腾空与跳跃恢复正常重力。
            body.gravityScale = grounded ? 0f : normalGravity;
            float acceleration = grounded ? groundAcceleration : airAcceleration;
            float targetSpeed = input.Move * runSpeed;
            Vector2 tangent = new Vector2(ground.Normal.y, -ground.Normal.x);
            float speed = grounded ? Vector2.Dot(body.linearVelocity, tangent) : body.linearVelocity.x;
            float nextSpeed = Mathf.MoveTowards(speed, targetSpeed, acceleration * Time.fixedDeltaTime);
            body.linearVelocity = grounded ? tangent * nextSpeed : new Vector2(nextSpeed, body.linearVelocity.y);
            wasGrounded = grounded;
        }

        private Vector2 GroundVelocity(float speed, bool grounded) => grounded ?
            new Vector2(ground.Normal.y, -ground.Normal.x) * speed : new Vector2(speed, 0);

        private void OnDisable()
        {
            if (health != null) health.OnDeath -= HandleDeath;
            actions.Changed -= ActionChanged;
            if (body != null) body.gravityScale = normalGravity;
            dashRemaining = 0f;
            dashQueued = false;
            wasGrounded = false;
            actions.Finish(PlayerAction.Dash);
        }

        private void ActionChanged(PlayerAction previous, PlayerAction next)
        {
            if (previous == PlayerAction.Dash && next != PlayerAction.Dash)
            {
                dashRemaining = 0;
                body.gravityScale = normalGravity;
            }
            if (next != PlayerAction.Hurt && next != PlayerAction.Dead) return;
            dashRemaining = 0;
            dashQueued = false;
            jumpBufferRemaining = coyoteRemaining = 0;
            jumpedSinceGrounded = wasGrounded = false;
            body.gravityScale = normalGravity;
        }

        public void ResetAfterRestore()
        {
            dashRemaining = dashCooldownRemaining = jumpBufferRemaining = coyoteRemaining = 0;
            dashQueued = jumpedSinceGrounded = wasGrounded = false;
            body.gravityScale = normalGravity;
            body.simulated = true;
            body.linearVelocity = Vector2.zero;
            enabled = true;
        }

        private void HandleDeath()
        {
            body.linearVelocity = Vector2.zero;
            body.simulated = false;
            enabled = false;
        }

#if UNITY_EDITOR
        public void ConfigureVisual(Transform visual) => visualRoot = visual;
#endif
    }
}
