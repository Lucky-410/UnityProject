using UnityEngine;

namespace Relicfall.Player
{
    public sealed class GroundDetector : MonoBehaviour
    {
        [SerializeField] private Transform checkPoint;
        [SerializeField, Min(0.01f)] private float radius = 0.045f;
        [SerializeField] private LayerMask groundLayers;
        [SerializeField, Range(1f, 75f)] private float maximumSlopeAngle = 55f;
        [SerializeField, Min(0.01f)] private float supportDistance = 0.10f;
        private BoxCollider2D bodyCollider;
        private readonly RaycastHit2D[] hits = new RaycastHit2D[12];
        private readonly ContactPoint2D[] contacts = new ContactPoint2D[12];

        public bool IsGrounded { get; private set; }
        public bool HasSupport { get; private set; }
        public Vector2 Normal { get; private set; } = Vector2.up;
        public float Distance { get; private set; }

        public void Refresh()
        {
            if (bodyCollider == null) bodyCollider = GetComponent<BoxCollider2D>();
            IsGrounded = HasSupport = false;
            Normal = Vector2.up;
            Distance = supportDistance;
            if (bodyCollider == null || !bodyCollider.enabled) return;
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(groundLayers);
            float minimumY = Mathf.Cos(maximumSlopeAngle * Mathf.Deg2Rad);
            float highestPoint = float.NegativeInfinity;
            int count = bodyCollider.GetContacts(filter, contacts);
            for (int i = 0; i < count; i++)
            {
                ContactPoint2D contact = contacts[i];
                Collider2D surface = contact.collider == bodyCollider ? contact.otherCollider : contact.collider;
                if (IgnoresOneWaySupport(surface, contact.point)) continue;
                // 墙面和头顶接触不能算作地面；转角处优先采用较高的脚部接触。
                if (contact.normal.y < minimumY || contact.point.y > bodyCollider.bounds.center.y + 0.015f ||
                    contact.point.y < highestPoint) continue;
                highestPoint = contact.point.y;
                Normal = contact.normal.normalized;
                Distance = 0;
                IsGrounded = HasSupport = true;
            }
            if (IsGrounded) return;
            // 用角色自身的脚部宽度向下扫掠，斜坡边缘也能正确检测支撑。
            count = bodyCollider.Cast(Vector2.down, filter, hits, supportDistance);
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = hits[i];
                if (hit.collider == null || hit.normal.y < minimumY || hit.distance > Distance) continue;
                // Cast 不会自动忽略单向平台：向上穿越时不能据此贴地或补发跳跃。
                if (IgnoresOneWaySupport(hit.collider, hit.point)) continue;
                Normal = hit.normal.normalized;
                Distance = hit.distance;
                HasSupport = true;
            }
            IsGrounded = HasSupport && Distance <= Mathf.Min(radius, 0.035f);
        }

        private bool IgnoresOneWaySupport(Collider2D surface, Vector2 point)
        {
            if (surface == null || !surface.TryGetComponent(out PlatformEffector2D platform) ||
                !platform.enabled || !platform.useOneWay) return false;
            // 复合瓦片碰撞的接触可能指向参与合并的 TilemapCollider2D。
            bool usesEffector = surface.usedByEffector ||
                (surface.TryGetComponent(out CompositeCollider2D composite) && composite.usedByEffector);
            if (!usesEffector) return false;
            return (bodyCollider.attachedRigidbody != null && bodyCollider.attachedRigidbody.linearVelocity.y > 0.1f) ||
                bodyCollider.bounds.min.y < point.y - 0.025f;
        }

#if UNITY_EDITOR
        public void Configure(Transform point, LayerMask layers)
        {
            checkPoint = point;
            groundLayers = layers;
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(checkPoint != null ? checkPoint.position : transform.position, radius);
        }
#endif
    }
}
