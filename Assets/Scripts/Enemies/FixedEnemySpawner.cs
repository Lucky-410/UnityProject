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
            HasSpawned = true;
            foreach (EnemyBrain brain in spawned.Values) if (brain != null && brain.gameObject.activeSelf) pool.Release(brain);
            spawned.Clear(); SpawnedCount = 0;
            var saved = new Dictionary<string, SavedEnemy>();
            if (records != null) foreach (SavedEnemy record in records) if (record != null && !string.IsNullOrEmpty(record.id)) saved[record.id] = record;
            foreach (FixedEnemyPlacement placement in placements)
            {
                if (placement.Point == null) continue;
                string id = Id(placement);
                saved.TryGetValue(id, out SavedEnemy record);
                if (record != null && record.health <= 0) continue;
                EnemyBrain brain = pool.Get(placement.Kind, placement.Point.position);
                brain.AssignSaveId(id);
                if (record != null) brain.RestoreProgress(record.health, record.position, placement.Point.position.x);
                spawned[id] = brain;
                SpawnedCount++;
            }
        }
        public SavedEnemy[] CaptureProgress()
        {
            var records = new List<SavedEnemy>();
            foreach (FixedEnemyPlacement placement in placements)
            {
                if (placement.Point == null) continue;
                string id = Id(placement);
                spawned.TryGetValue(id, out EnemyBrain brain);
                bool alive = brain != null && brain.SaveId == id && brain.gameObject.activeInHierarchy && !brain.Health.IsDead;
                records.Add(new SavedEnemy { id = id, health = alive ? brain.Health.CurrentHealth : 0,
                    position = alive ? brain.transform.position : placement.Point.position });
            }
            return records.ToArray();
        }
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
