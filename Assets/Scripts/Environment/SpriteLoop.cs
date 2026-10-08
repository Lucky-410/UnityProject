using UnityEngine;

namespace Relicfall.Environment
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteLoop : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField, Min(1f)] private float framesPerSecond = 8f;
        private SpriteRenderer display;

        private void Awake() => display = GetComponent<SpriteRenderer>();

        private void Update()
        {
            if (frames == null || frames.Length == 0) return;
            int index = Mathf.FloorToInt(Time.time * framesPerSecond) % frames.Length;
            display.sprite = frames[index];
        }

#if UNITY_EDITOR
        public void Configure(Sprite[] animationFrames, float fps)
        {
            frames = animationFrames;
            framesPerSecond = Mathf.Max(1f, fps);
        }
#endif
    }
}
