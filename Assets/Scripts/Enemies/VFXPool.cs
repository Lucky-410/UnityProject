using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Relicfall.Enemies
{
    public sealed class VFXPool : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;
        [SerializeField, Min(0)] private int prewarmCount = 6;
        [SerializeField, Min(0.01f)] private float duration = 0.35f;
        private readonly Queue<GameObject> available = new();
        private readonly HashSet<GameObject> active = new();

        public static VFXPool Instance { get; private set; }
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

        private GameObject Create()
        {
            GameObject effect = Instantiate(prefab, transform);
            effect.SetActive(false);
            return effect;
        }

        public GameObject Play(Vector3 position)
        {
            GameObject effect = available.Count > 0 ? available.Dequeue() : Create();
            effect.transform.SetParent(null);
            effect.transform.position = position;
            active.Add(effect);
            effect.SetActive(true);
            Animator animator = effect.GetComponent<Animator>();
            if (animator != null) animator.Play("Explode", 0, 0f);
            StartCoroutine(ReleaseLater(effect));
            return effect;
        }

        private IEnumerator ReleaseLater(GameObject effect)
        {
            yield return new WaitForSeconds(duration);
            if (!active.Remove(effect)) yield break;
            effect.SetActive(false);
            effect.transform.SetParent(transform);
            available.Enqueue(effect);
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
