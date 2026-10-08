using System.Collections.Generic;
using Relicfall.Combat;
using UnityEngine;

namespace Relicfall.Enemies
{
    public sealed class EnemyPool : MonoBehaviour
    {
        [SerializeField] private EnemyBrain[] prefabs;
        [SerializeField, Min(0)] private int prewarmPerKind = 3;

        private readonly Dictionary<EnemyKind, Queue<EnemyBrain>> available = new();
        private readonly HashSet<EnemyBrain> active = new();
        public int ActiveCount => active.Count;
        public bool IsInitialized { get; private set; }

        private void Awake() => Initialize();

        public void Initialize()
        {
            if (IsInitialized) return;
            foreach (EnemyBrain prefab in prefabs)
            {
                available[prefab.Kind] = new Queue<EnemyBrain>();
                for (int i = 0; i < prewarmPerKind; i++)
                    available[prefab.Kind].Enqueue(Create(prefab));
            }
            IsInitialized = true;
        }

        private EnemyBrain Create(EnemyBrain prefab)
        {
            EnemyBrain brain = Instantiate(prefab, transform);
            brain.gameObject.SetActive(false);
            brain.gameObject.AddComponent<PooledEnemy>().Bind(this);
            return brain;
        }

        public EnemyBrain Get(EnemyKind kind, Vector3 position)
        {
            if (!available.TryGetValue(kind, out Queue<EnemyBrain> queue))
                throw new System.InvalidOperationException($"缺少敌人预制体：{kind}");
            EnemyBrain brain = queue.Count > 0 ? queue.Dequeue() : Create(FindPrefab(kind));
            brain.transform.SetParent(null);
            brain.transform.position = position;
            brain.ResetForSpawn();
            active.Add(brain);
            brain.gameObject.SetActive(true);
            return brain;
        }

        public void Release(EnemyBrain brain)
        {
            if (brain == null || !active.Remove(brain)) return;
            brain.gameObject.SetActive(false);
            brain.transform.SetParent(transform);
            available[brain.Kind].Enqueue(brain);
        }

        public int Available(EnemyKind kind) =>
            available.TryGetValue(kind, out Queue<EnemyBrain> queue) ? queue.Count : 0;

        private EnemyBrain FindPrefab(EnemyKind kind)
        {
            foreach (EnemyBrain prefab in prefabs)
                if (prefab.Kind == kind) return prefab;
            throw new System.InvalidOperationException($"缺少敌人预制体：{kind}");
        }

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
        private Coroutine releaseRoutine;

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
            if (releaseRoutine != null) StopCoroutine(releaseRoutine);
            releaseRoutine = null;
        }

        private void HandleDeath() => releaseRoutine = StartCoroutine(ReleaseAfterAnimation());

        private System.Collections.IEnumerator ReleaseAfterAnimation()
        {
            yield return new WaitForSeconds(0.75f);
            pool.Release(brain);
        }
    }
}
