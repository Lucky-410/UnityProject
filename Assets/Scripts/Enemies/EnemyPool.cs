using System.Collections.Generic;
using Relicfall.Combat;
using Relicfall.Core;
using UnityEngine;

namespace Relicfall.Enemies
{
    public sealed class EnemyPool : MonoBehaviour
    {
        [SerializeField] private EnemyBrain[] prefabs;
        [SerializeField, Min(0)] private int prewarmPerKind = 3;
        [SerializeField, Min(0)] private int retainedPerKind = 8;

        private readonly Dictionary<EnemyKind, SceneObjectPool<EnemyBrain>> pools = new();
        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (var pool in pools.Values) count += pool.ActiveCount;
                return count;
            }
        }
        public bool IsInitialized { get; private set; }

        private void Awake() => Initialize();

        public void Initialize()
        {
            if (IsInitialized) return;
            if (prefabs == null) return;
            foreach (EnemyBrain prefab in prefabs)
            {
                if (prefab == null || pools.ContainsKey(prefab.Kind)) continue;
                pools.Add(prefab.Kind, new SceneObjectPool<EnemyBrain>(prefab, transform,
                    prewarmPerKind, retainedPerKind, BindEnemy));
            }
            IsInitialized = true;
        }

        private void BindEnemy(EnemyBrain brain)
        {
            if (!brain.TryGetComponent(out PooledEnemy pooled)) pooled = brain.gameObject.AddComponent<PooledEnemy>();
            pooled.Bind(this);
        }

        public EnemyBrain Get(EnemyKind kind, Vector3 position)
        {
            if (!pools.TryGetValue(kind, out SceneObjectPool<EnemyBrain> pool))
                throw new System.InvalidOperationException($"缺少敌人预制体：{kind}");
            EnemyBrain brain = pool.Rent(position);
            brain.ResetForSpawn();
            brain.gameObject.SetActive(true);
            return brain;
        }

        public void Release(EnemyBrain brain)
        {
            if (brain != null && pools.TryGetValue(brain.Kind, out var pool)) pool.Release(brain);
        }

        public int Available(EnemyKind kind) =>
            pools.TryGetValue(kind, out var pool) ? pool.AvailableCount : 0;

#if UNITY_EDITOR
        public void Configure(EnemyBrain[] enemyPrefabs, int prewarm)
        {
            prefabs = enemyPrefabs;
            prewarmPerKind = prewarm;
        }
#endif
    }

    [RequireComponent(typeof(Health), typeof(EnemyBrain))]
    public sealed class PooledEnemy : MonoBehaviour
    {
        private EnemyPool pool;
        private Health health;
        private EnemyBrain brain;
        private float releaseAt = float.PositiveInfinity;

        private void Awake()
        {
            health = GetComponent<Health>();
            brain = GetComponent<EnemyBrain>();
        }

        public void Bind(EnemyPool owner) => pool = owner;

        private void OnEnable() => health.OnDeath += HandleDeath;

        private void OnDisable()
        {
            health.OnDeath -= HandleDeath;
            releaseAt = float.PositiveInfinity;
        }

        private void HandleDeath() => releaseAt = Time.time + 0.75f;
        private void Update()
        {
            if (Time.time >= releaseAt && pool != null) pool.Release(brain);
        }
    }
}
