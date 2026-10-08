using Relicfall.Combat;
using UnityEngine;

namespace Relicfall.Items
{
    [System.Serializable]
    public struct DropEntry
    {
        public ItemData Item;
        [Range(0f, 1f)] public float Chance;
        [Min(1)] public int Minimum;
        [Min(1)] public int Maximum;
    }

    [RequireComponent(typeof(Health))]
    public sealed class DropTable : MonoBehaviour
    {
        [SerializeField] private WorldItem worldItemPrefab;
        [SerializeField] private DropEntry[] entries;
        private Health health;

        private void Awake() => health = GetComponent<Health>();
        private void OnEnable() => health.OnDeath += Drop;
        private void OnDisable() => health.OnDeath -= Drop;

        public void Drop()
        {
            if (worldItemPrefab == null || entries == null) return;
            foreach (DropEntry entry in entries)
            {
                if (entry.Item == null || Random.value > entry.Chance) continue;
                int amount = Random.Range(entry.Minimum, Mathf.Max(entry.Minimum, entry.Maximum) + 1);
                WorldItem.Spawn(worldItemPrefab, entry.Item, amount,
                    transform.position + new Vector3(Random.Range(-0.16f, 0.16f), -0.2f, 0f));
            }
        }

#if UNITY_EDITOR
        public void Configure(WorldItem prefab, DropEntry[] dropEntries)
        {
            worldItemPrefab = prefab;
            entries = dropEntries;
        }
#endif
    }
}
