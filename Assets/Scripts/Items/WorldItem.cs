using System.Collections.Generic;
using Relicfall.Player;
using Relicfall.Audio;
using UnityEngine;
using Relicfall.Save;

namespace Relicfall.Items
{
    [RequireComponent(typeof(CircleCollider2D), typeof(SpriteRenderer))]
    public sealed class WorldItem : MonoBehaviour
    {
        [SerializeField] private ItemData item;
        [SerializeField] private string saveId;
        [SerializeField] private string legacySaveId;
        [SerializeField, Min(1)] private int count = 1;
        [SerializeField, Min(0f)] private float floatHeight = 0.035f;
        [SerializeField, Min(0f)] private float floatSpeed = 2f;

        private SpriteRenderer display;
        private Vector3 origin;
        private static readonly List<WorldItem> activeItems = new();
        private static readonly HashSet<string> collected = new();
        private bool spawnedRuntime;

        public ItemData Item => item;
        public int Count => count;
        public Vector3 Origin => origin;
        public bool SpawnedRuntime => spawnedRuntime;
        public static IReadOnlyList<WorldItem> ActiveItems => activeItems;
        public string SaveId => !string.IsNullOrEmpty(saveId) ? saveId : LegacySaveId;
        public string LegacySaveId => !string.IsNullOrEmpty(legacySaveId) ? legacySaveId : item == null ? string.Empty :
            item.name + ":" + Mathf.RoundToInt(origin.x * 1000f) + ":" +
            Mathf.RoundToInt(origin.y * 1000f);
        public static string[] CollectedIds
        {
            get
            {
                var ids = new string[collected.Count];
                collected.CopyTo(ids);
                return ids;
            }
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { activeItems.Clear(); collected.Clear(); }

        public static void ResetCollected() => collected.Clear();

        public static void RestoreCollected(string[] ids)
        {
            collected.Clear();
            if (ids != null)
                foreach (string id in ids)
                    if (!string.IsNullOrEmpty(id)) collected.Add(id);
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                WorldItem worldItem = activeItems[i];
                if (worldItem != null && !worldItem.spawnedRuntime &&
                    (collected.Contains(worldItem.SaveId) || collected.Contains(worldItem.LegacySaveId)))
                {
                    collected.Add(worldItem.SaveId);
                    worldItem.gameObject.SetActive(false);
                    UnityEngine.Object.Destroy(worldItem.gameObject);
                }
            }
        }
        public static WorldItem Focused => PlayerPickupDetector.Current != null ? PlayerPickupDetector.Current.Focused : null;

        private void Awake()
        {
            display = GetComponent<SpriteRenderer>();
            var trigger = GetComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.35f;
        }

        private void OnEnable()
        {
            origin = transform.position;
            if (display == null) display = GetComponent<SpriteRenderer>();
            if (item != null) display.sprite = item.WorldSprite;
            if (!activeItems.Contains(this)) activeItems.Add(this);
        }

        private void OnDisable() => activeItems.Remove(this);

        private void Update()
        {
            transform.position = origin + Vector3.up *
                (Mathf.Sin(Time.time * floatSpeed) * floatHeight);
        }

        public bool TryPickup() => PlayerPickupDetector.Current != null && PlayerPickupDetector.Current.TryPickup(this);
        public void CompletePickup()
        {
            if (!isActiveAndEnabled) return;
            if (!spawnedRuntime) collected.Add(SaveId);
            activeItems.Remove(this);
            AudioDirector.Instance?.PlayPickup();
            gameObject.SetActive(false);
            Destroy(gameObject);
        }

        public static SavedWorldItem[] CaptureRuntimeItems()
        {
            var records = new List<SavedWorldItem>();
            foreach (WorldItem world in activeItems)
                if (world != null && world.spawnedRuntime && world.item != null)
                    records.Add(new SavedWorldItem { instanceId = world.SaveId, itemId = world.item.StableId,
                        count = world.count, position = world.origin });
            return records.ToArray();
        }

        public static void RestoreRuntimeItems(SavedWorldItem[] records, WorldItem prefab, System.Func<string, ItemData> find)
        {
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                WorldItem world = activeItems[i];
                if (world != null && world.spawnedRuntime) { world.gameObject.SetActive(false); Destroy(world.gameObject); }
            }
            if (records == null || prefab == null) return;
            var restored = new HashSet<string>();
            foreach (SavedWorldItem record in records)
            {
                if (record == null || record.count <= 0 || !restored.Add(record.instanceId)) continue;
                ItemData data = find(record.itemId);
                if (data == null) continue;
                WorldItem world = Spawn(prefab, data, record.count, record.position);
                world.saveId = record.instanceId;
            }
        }

        public void Initialize(ItemData data, int amount)
        {
            item = data;
            count = Mathf.Max(1, amount);
            if (display == null) display = GetComponent<SpriteRenderer>();
            display.sprite = data.WorldSprite;
            origin = transform.position;
        }

        public static WorldItem Spawn(WorldItem prefab, ItemData data, int amount, Vector3 position)
        {
            WorldItem instance = Instantiate(prefab, position, Quaternion.identity);
            instance.spawnedRuntime = true;
            instance.saveId = System.Guid.NewGuid().ToString("N");
            instance.legacySaveId = string.Empty;
            instance.Initialize(data, amount);
            return instance;
        }
#if UNITY_EDITOR
        public void ConfigureIdentity(string id, string legacy) { saveId = id; legacySaveId = legacy; }
#endif
    }
}
