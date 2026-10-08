using Relicfall.Core;
using UnityEngine;

namespace Relicfall.Enemies
{
    public sealed class ProjectilePool : MonoBehaviour
    {
        [SerializeField] private EnemyProjectile prefab;
        [SerializeField, Min(0)] private int prewarmCount = 8;
        [SerializeField, Min(0)] private int retainedCapacity = 16;
        private SceneObjectPool<EnemyProjectile> pool;

        public static ProjectilePool Instance { get; private set; }
        public int ActiveCount => pool != null ? pool.ActiveCount : 0;
        public int AvailableCount => pool != null ? pool.AvailableCount : 0;
        public bool IsInitialized { get; private set; }

        private void Awake() => Initialize();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        public void Initialize()
        {
            if (IsInitialized) return;
            if (prefab == null) return;
            Instance = this;
            pool = new SceneObjectPool<EnemyProjectile>(prefab, transform, prewarmCount,
                retainedCapacity, shot => shot.AttachPool(this));
            IsInitialized = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public EnemyProjectile Fire(Vector3 position, int direction, int damage, GameObject source)
        {
            EnemyProjectile shot = pool.Rent(position);
            shot.Launch(direction, damage, source);
            shot.gameObject.SetActive(true);
            return shot;
        }

        public void Release(EnemyProjectile shot)
        {
            pool?.Release(shot);
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
