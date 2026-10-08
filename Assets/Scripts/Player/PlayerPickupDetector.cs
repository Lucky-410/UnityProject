using Relicfall.Combat;
using Relicfall.Items;
using Relicfall.UI;
using UnityEngine;

namespace Relicfall.Player
{
    [DefaultExecutionOrder(100)]
    public sealed class PlayerPickupDetector : MonoBehaviour
    {
        private const float PickupDistance = 0.36f;
        private BoxCollider2D bodyCollider;
        private Health health;
        private Inventory inventory;
        private InventoryUI bag;
        private int scannedFrame = -1;
        private WorldItem focused;
        public static PlayerPickupDetector Current { get; private set; }
        public PlayerMotor Motor { get; private set; }
        public int ScanCount { get; private set; }
        public WorldItem Focused { get { ScanOnce(); return focused != null && focused.isActiveAndEnabled ? focused : null; } }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;
        private void Awake()
        {
            Motor = GetComponent<PlayerMotor>();
            health = GetComponent<Health>();
            inventory = GetComponent<Inventory>();
            bag = GetComponent<InventoryUI>();
            bodyCollider = GetComponent<BoxCollider2D>();
        }
        private void OnEnable() { Current = this; scannedFrame = -1; }
        private void OnDisable() { if (Current == this) Current = null; focused = null; }
        private void Update()
        {
            ScanOnce();
            if (UIManager.Existing != null && UIManager.Existing.Inputs.ConsumePickup() && CanPickup()) TryPickupFocused();
        }
        private bool CanPickup() => health != null && !health.IsDead && Motor != null && Motor.isActiveAndEnabled &&
            !GameUIScreen.BlocksGameplay && !SceneTransition.IsLoading && (bag == null || !bag.IsOpen);
        private void ScanOnce()
        {
            if (scannedFrame == Time.frameCount) return;
            scannedFrame = Time.frameCount;
            ScanCount++;
            focused = null;
            if (!CanPickup()) return;
            float nearest = PickupDistance * PickupDistance;
            foreach (WorldItem item in WorldItem.ActiveItems)
            {
                if (item == null || !item.isActiveAndEnabled || item.Item == null) continue;
                float distance = Distance(item.transform.position);
                if (distance >= nearest) continue;
                nearest = distance;
                focused = item;
            }
        }
        private float Distance(Vector3 position) => ((Vector2)position -
            (bodyCollider != null ? bodyCollider.ClosestPoint(position) : (Vector2)transform.position)).sqrMagnitude;
        public bool TryPickupFocused() => Focused != null && TryPickup(Focused);
        public bool TryPickup(WorldItem item)
        {
            if (!CanPickup() || item == null || item != Focused || inventory == null ||
                Distance(item.transform.position) > PickupDistance * PickupDistance) return false;
            if (!inventory.TryAdd(item.Item, item.Count)) return false;
            item.CompletePickup();
            focused = null;
            return true;
        }
        public void Invalidate() { scannedFrame = -1; focused = null; }
    }
}
