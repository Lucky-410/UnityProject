using UnityEngine;

namespace Relicfall.Items
{
    public sealed class PotionDrinkVisual : MonoBehaviour
    {
        [SerializeField] private Sprite[] frames;
        [SerializeField] private SpriteRenderer overlay;
        [SerializeField, Min(1f)] private float framesPerSecond = 18f;
        private bool playing;
        private float startedAt;
        private int displayedFrame = -1;

        public bool IsPlaying => playing;

        private void Awake()
        {
            if (overlay != null) overlay.enabled = false;
        }

        public void Play()
        {
            if (frames == null || frames.Length == 0 || overlay == null) return;
            playing = true;
            startedAt = Time.time;
            displayedFrame = 0;
            overlay.sprite = frames[0];
            overlay.enabled = true;
        }

        private void Update()
        {
            if (!playing) return;
            int frame = Mathf.FloorToInt((Time.time - startedAt) * framesPerSecond);
            if (frame >= frames.Length)
            {
                playing = false;
                overlay.enabled = false;
                return;
            }
            if (frame == displayedFrame) return;
            displayedFrame = frame;
            overlay.sprite = frames[frame];
        }

        private void OnDisable()
        {
            if (overlay != null) overlay.enabled = false;
            playing = false;
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
