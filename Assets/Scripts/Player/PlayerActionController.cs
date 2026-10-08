using System;
using Relicfall.Combat;
using UnityEngine;

namespace Relicfall.Player
{
    public enum PlayerAction { Free, Attack, Dash, Hurt, Dead }
    public enum PlayerLocomotion { Grounded, Rising, Falling }

    // 动作状态控制互斥与中断；移动状态独立，允许地面/空中攻击。
    [DefaultExecutionOrder(-150), RequireComponent(typeof(Health))]
    public sealed class PlayerActionController : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float hurtDuration = 0.28f;
        private Health health;
        private float hurtUntil;
        public PlayerAction Action { get; private set; }
        public PlayerLocomotion Locomotion { get; private set; }
        public bool CanControl => Action != PlayerAction.Hurt && Action != PlayerAction.Dead;
        public bool CanSwitchWeapon => Action == PlayerAction.Free;
        public event Action<PlayerAction, PlayerAction> Changed;

        private void Awake() => health = GetComponent<Health>();
        private void OnEnable()
        {
            health.OnDamaged += Damaged;
            health.OnDeath += Died;
        }
        private void OnDisable()
        {
            health.OnDamaged -= Damaged;
            health.OnDeath -= Died;
        }
        private void Update()
        {
            if (Action == PlayerAction.Hurt && Time.time >= hurtUntil) Change(PlayerAction.Free);
        }
        public bool TryAttack()
        {
            if (Action != PlayerAction.Free && Action != PlayerAction.Attack) return false;
            Change(PlayerAction.Attack);
            return true;
        }
        public bool TryDash()
        {
            if (Action != PlayerAction.Free && Action != PlayerAction.Attack) return false;
            Change(PlayerAction.Dash);
            return true;
        }
        public void Finish(PlayerAction owner)
        {
            if (Action == owner) Change(PlayerAction.Free);
        }
        public void SetLocomotion(bool grounded, float verticalSpeed) => Locomotion = grounded ?
            PlayerLocomotion.Grounded : verticalSpeed > 0.05f ? PlayerLocomotion.Rising : PlayerLocomotion.Falling;
        private void Damaged(DamageInfo info)
        {
            if (health.IsDead) { Died(); return; }
            hurtUntil = Time.time + hurtDuration;
            Change(PlayerAction.Hurt);
        }
        private void Died() => Change(PlayerAction.Dead);
        public void ResetAfterRestore() { hurtUntil = 0; Change(PlayerAction.Free); }
        public void CancelForInterface()
        {
            if (Action == PlayerAction.Attack || Action == PlayerAction.Dash) Change(PlayerAction.Free);
        }
        private void Change(PlayerAction next)
        {
            if (Action == next) return;
            PlayerAction previous = Action;
            Action = next;
            Changed?.Invoke(previous, next);
        }
    }
}
