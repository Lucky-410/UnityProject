using System;
using UnityEngine;
using System.Collections.Generic;
using Relicfall.Save;
using Relicfall.Environment;

namespace Relicfall.Enemies
{
    [Serializable]
    public struct FixedEnemyPlacement
    {
        public EnemyKind Kind;
        public Transform Point;
        public string StableId;
    }

    public sealed class FixedEnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyPool pool;
        [SerializeField] private FixedEnemyPlacement[] placements;

        public int SpawnedCount { get; private set; }
        public bool HasSpawned { get; private set; }
        private readonly Dictionary<string, EnemyBrain> spawned = new();
        private readonly HashSet<string> defeated = new();
        private readonly HashSet<EnemyBrain> tracked = new();

        private void Start()
        {
            if (pool == null || placements == null)
            {
                Debug.LogError("固定敌人生成配置缺失。", this);
                return;
            }
            GameSaveData save = GameSaveController.Instance != null ? GameSaveController.Instance.PendingData : null;
            RestoreProgress(save != null && save.levelRevision == ReferenceLevelLayout.Revision ? save.enemies : null);
        }

        public void RestoreProgress(SavedEnemy[] records)
        {
            UntrackEnemies();
            HasSpawned = true;
            foreach (var entry in spawned)
                if (entry.Value != null && entry.Value.SaveId == entry.Key && entry.Value.gameObject.activeSelf)
                    pool.Release(entry.Value);
            spawned.Clear();
            defeated.Clear();
            SpawnedCount = 0;
            var saved = new Dictionary<string, SavedEnemy>();
            if (records != null) foreach (SavedEnemy record in records) if (record != null && !string.IsNullOrEmpty(record.id)) saved[record.id] = record;
            foreach (FixedEnemyPlacement placement in placements)
            {
                if (placement.Point == null) continue;
                string id = Id(placement);
                saved.TryGetValue(id, out SavedEnemy record);
                if (record != null && (record.defeated || record.health <= 0))
                {
                    defeated.Add(id);
                    continue;
                }
                EnemyBrain brain = pool.Get(placement.Kind, placement.Point.position);
                brain.AssignSaveId(id);
                if (record != null) brain.RestoreProgress(record.health, record.position, placement.Point.position.x);
                spawned[id] = brain;
                brain.Died += RecordDefeat;
                tracked.Add(brain);
                SpawnedCount++;
            }
        }
        public SavedEnemy[] CaptureProgress()
        {
            if (!HasSpawned) return Array.Empty<SavedEnemy>();
            var records = new List<SavedEnemy>();
            foreach (FixedEnemyPlacement placement in placements)
            {
                if (placement.Point == null) continue;
                string id = Id(placement);
                spawned.TryGetValue(id, out EnemyBrain brain);
                bool matches = brain != null && brain.SaveId == id;
                if (matches && brain.Health.IsDead) defeated.Add(id);
                bool dead = defeated.Contains(id);
                bool alive = !dead && matches && brain.gameObject.activeInHierarchy;
                records.Add(new SavedEnemy { id = id, defeated = dead, health = alive ? brain.Health.CurrentHealth : 0,
                    position = alive ? brain.transform.position : placement.Point.position });
            }
            return records.ToArray();
        }

        private void RecordDefeat(EnemyBrain brain)
        {
            string id = brain.SaveId;
            if (!string.IsNullOrEmpty(id) && spawned.TryGetValue(id, out EnemyBrain registered) && registered == brain)
                defeated.Add(id);
            brain.Died -= RecordDefeat;
            tracked.Remove(brain);
        }

        private void UntrackEnemies()
        {
            foreach (EnemyBrain brain in tracked)
                if (brain != null) brain.Died -= RecordDefeat;
            tracked.Clear();
        }

        private void OnDestroy() => UntrackEnemies();
        private static string Id(FixedEnemyPlacement placement) => !string.IsNullOrEmpty(placement.StableId) ?
            placement.StableId : placement.Point.name;

#if UNITY_EDITOR
        public void Configure(EnemyPool enemyPool, FixedEnemyPlacement[] fixedPlacements)
        {
            pool = enemyPool;
            placements = fixedPlacements;
        }
#endif
    }
}
