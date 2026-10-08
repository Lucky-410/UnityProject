using Relicfall.Player;
using UnityEngine;

namespace Relicfall.Feedback
{
    public sealed class DashTrailController : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private TrailRenderer trail;
        [SerializeField] private SpriteRenderer character;
        [SerializeField] private Transform visualRoot;

        private void LateUpdate()
        {
            if (trail == null || motor == null || visualRoot == null) return;
            Vector3 position;
            if (character != null && character.sprite != null)
            {
                Bounds bounds = character.bounds;
                float facing = visualRoot.lossyScale.x < 0f ? -1f : 1f;
                float behind = Mathf.Clamp(bounds.extents.x * 0.65f, 0.13f, 0.34f);
                position = new Vector3(bounds.center.x - facing * behind,
                    bounds.center.y + bounds.extents.y * 0.05f, transform.position.z);
            }
            else
            {
                position = visualRoot.TransformPoint(new Vector3(-0.18f, 0.35f, 0f));
            }
            transform.position = position;
            trail.emitting = motor.IsDashing;
        }

#if UNITY_EDITOR
        public void Configure(PlayerMotor player, TrailRenderer renderer,
            SpriteRenderer sprite, Transform visual)
        {
            motor = player;
            trail = renderer;
            character = sprite;
            visualRoot = visual;
        }
#endif
    }
}
