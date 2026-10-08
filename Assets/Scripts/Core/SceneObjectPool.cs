using System;
using System.Collections.Generic;
using UnityEngine;

namespace Relicfall.Core
{
    public sealed class SceneObjectPool<T> where T : Component
    {
        private readonly T prefab;
        private readonly Transform root;
        private readonly int retainedCapacity;
        private readonly Action<T> initialize;
        private readonly Queue<T> available;
        private readonly HashSet<T> active = new();

        public int ActiveCount => active.Count;
        public int AvailableCount => available.Count;

        public SceneObjectPool(T prefab, Transform root, int prewarm, int retainedCapacity,
            Action<T> initialize = null)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            if (root == null) throw new ArgumentNullException(nameof(root));
            this.prefab = prefab;
            this.root = root;
            this.retainedCapacity = Mathf.Max(0, retainedCapacity);
            this.initialize = initialize;
            available = new Queue<T>(this.retainedCapacity);
            for (int i = 0; i < Mathf.Min(prewarm, this.retainedCapacity); i++)
                available.Enqueue(Create());
        }

        private T Create()
        {
            T instance = UnityEngine.Object.Instantiate(prefab, root);
            instance.gameObject.SetActive(false);
            initialize?.Invoke(instance);
            return instance;
        }

        // 保持在池的层级内，场景卸载或销毁池时一并销毁活动实例。
        public T Rent(Vector3 position)
        {
            T instance = null;
            while (available.Count > 0 && instance == null) instance = available.Dequeue();
            if (instance == null) instance = Create();
            instance.transform.position = position;
            active.Add(instance);
            return instance;
        }

        public bool Release(T instance)
        {
            if (instance == null || !active.Remove(instance)) return false;
            instance.gameObject.SetActive(false);
            if (available.Count < retainedCapacity) available.Enqueue(instance);
            else UnityEngine.Object.Destroy(instance.gameObject);
            return true;
        }
    }
}
