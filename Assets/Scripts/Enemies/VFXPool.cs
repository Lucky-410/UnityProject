using Relicfall.Core;
using UnityEngine;

namespace Relicfall.Enemies
{
    public sealed class VFXPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(0)] private int prewarmCount = 6;
        [SerializeField, Min(0)] private int retainedCapacity = 12;
        [SerializeField, Min(1)] private int maxConcurrent = 24;
        [SerializeField, Min(0.01f)] private float duration = 0.35f;
        private SceneObjectPool<Transform> pool;
        private Effect[] effects;
        private int effectCount;
        private static readonly int ExplodeState = Animator.StringToHash("Explode");

        private struct Effect
        {
            public Transform Transform;
            public float Expires;
        }

        public static VFXPool Instance { get; private set; }
        public int ActiveCount => effectCount;
        public int AvailableCount => pool != null ? pool.AvailableCount : 0;
        public bool IsInitialized { get; private set; }

        private void Awake() => Initialize();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        public void Initialize()
        {
            if (IsInitialized || prefab == null) return;
            Instance = this;
            effects = new Effect[Mathf.Max(1, maxConcurrent)];
            pool = new SceneObjectPool<Transform>(prefab.transform, transform, prewarmCount, retainedCapacity);
            IsInitialized = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public GameObject Play(Vector3 position)
        {
            if (!IsInitialized) return null;
            if (effectCount == effects.Length)
            {
                int oldest = 0;
                for (int i = 1; i < effectCount; i++)
                    if (effects[i].Expires < effects[oldest].Expires) oldest = i;
                ReleaseAt(oldest);
            }
            Transform effect = pool.Rent(position);
            effect.gameObject.SetActive(true);
            if (effect.TryGetComponent(out Animator animator)) animator.Play(ExplodeState, 0, 0f);
            effects[effectCount++] = new Effect { Transform = effect, Expires = Time.time + duration };
            return effect.gameObject;
        }

        private void Update()
        {
            for (int i = effectCount - 1; i >= 0; i--)
                if (effects[i].Transform == null || Time.time >= effects[i].Expires) ReleaseAt(i);
        }

        private void ReleaseAt(int index)
        {
            pool.Release(effects[index].Transform);
            effects[index] = effects[--effectCount];
            effects[effectCount] = default;
        }

#if UNITY_EDITOR
        public void Configure(GameObject source, int prewarm)
        {
            prefab = source;
            prewarmCount = prewarm;
        }
#endif
    }
}
