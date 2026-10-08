using System.Collections.Generic;
using UnityEngine;

namespace Relicfall.Enemies
{
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private EnemyProjectile prefab;
        [SerializeField, Min(0)] private int prewarmCount = 8;
        private readonly Queue<EnemyProjectile> available = new();
        private readonly HashSet<EnemyProjectile> active = new();

        public static ProjectilePool Instance { get; private set; }
        public int ActiveCount => active.Count;
        public int AvailableCount => available.Count;
        public bool IsInitialized { get; private set; }

        private void Awake() => Initialize();

        public void Initialize()
        {
            if (IsInitialized) return;
            Instance = this;
            for (int i = 0; i < prewarmCount; i++) available.Enqueue(Create());
            IsInitialized = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private EnemyProjectile Create()
        {
            EnemyProjectile shot = Instantiate(prefab, transform);
            shot.gameObject.SetActive(false);
            shot.AttachPool(this);
            return shot;
        }

        public EnemyProjectile Fire(Vector3 position, int direction, int damage, GameObject source)
        {
            EnemyProjectile shot = available.Count > 0 ? available.Dequeue() : Create();
            shot.transform.SetParent(null);
            shot.transform.position = position;
            active.Add(shot);
            shot.gameObject.SetActive(true);
            shot.Launch(direction, damage, source);
            return shot;
        }

        public void Release(EnemyProjectile shot)
        {
            if (shot == null || !active.Remove(shot)) return;
            shot.gameObject.SetActive(false);
            shot.transform.SetParent(transform);
            available.Enqueue(shot);
        }

#if UNITY_EDITOR
        public void Configure(EnemyProjectile source, int prewarm)
        {
            prefab = source;
            prewarmCount = prewarm;
        }
#endif
    }
}
