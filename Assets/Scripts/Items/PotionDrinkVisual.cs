using System.Collections;
using UnityEngine;

namespace Relicfall.Items
{
    public sealed class PotionDrinkVisual : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private SpriteRenderer overlay;
        [SerializeField, Min(1f)] private float framesPerSecond = 18f;
        private Coroutine routine;

        public bool IsPlaying => routine != null;

        private void Awake()
        {
            if (overlay != null) overlay.enabled = false;
        }

        public void Play()
        {
            if (frames == null || frames.Length == 0 || overlay == null) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            overlay.enabled = true;
            foreach (Sprite frame in frames)
            {
                overlay.sprite = frame;
                yield return new WaitForSeconds(1f / framesPerSecond);
            }
            overlay.enabled = false;
            routine = null;
        }

        private void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            if (overlay != null) overlay.enabled = false;
            routine = null;
        }

#if UNITY_EDITOR
        public void Configure(Sprite[] animationFrames, SpriteRenderer target)
        {
            frames = animationFrames;
            overlay = target;
        }
#endif
    }
}
