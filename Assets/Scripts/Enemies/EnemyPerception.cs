using Relicfall.Combat;
using Relicfall.Player;
using UnityEngine;

namespace Relicfall.Enemies
{
    public sealed class EnemyPerception : MonoBehaviour
    {
        public Transform Target { get; private set; }
        public Health TargetHealth { get; private set; }
        public void RefreshTarget()
        {
            PlayerContext player = PlayerContext.Current;
            bool alive = player != null && player.isActiveAndEnabled && player.Health != null && !player.Health.IsDead;
            Target = alive ? player.transform : null;
            TargetHealth = alive ? player.Health : null;
        }
        public bool CanSee(float homeX, float patrolHalfWidth, float sightRange, LayerMask ground)
        {
            if (Target == null || Mathf.Abs(Target.position.x - homeX) > patrolHalfWidth + sightRange) return false;
            Vector2 delta = Target.position - transform.position;
            if (Mathf.Abs(delta.x) > sightRange || Mathf.Abs(delta.y) > 0.65f) return false;
            return !Physics2D.Raycast((Vector2)transform.position + Vector2.up * 0.08f,
                delta.normalized, delta.magnitude, ground);
        }
        public bool SafeAhead(int direction, LayerMask ground)
        {
            Vector2 origin = (Vector2)transform.position + new Vector2(direction * 0.16f, -0.08f);
            return Physics2D.Raycast(origin, Vector2.down, 0.48f, ground) &&
                !Physics2D.Raycast(origin + Vector2.up * 0.2f, Vector2.right * direction, 0.18f, ground);
        }
    }
}
