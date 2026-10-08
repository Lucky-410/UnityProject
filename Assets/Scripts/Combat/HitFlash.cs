using System.Collections;
using UnityEngine;

namespace Relicfall.Combat
{
    [RequireComponent(typeof(Health))]
    public sealed class HitFlash : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private SpriteRenderer[] additionalTargets;
        [SerializeField] private Color flashColor = new Color(1f, 0.35f, 0.42f, 1f);
        [SerializeField, Min(0f)] private float duration = 0.1f;

        private Health health;
        private Color baseColor;
        private Color[] additionalBaseColors;
        private Coroutine flashRoutine;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (target == null) target = GetComponentInChildren<SpriteRenderer>();
            if (target != null) baseColor = target.color;
            additionalBaseColors = new Color[additionalTargets != null ? additionalTargets.Length : 0];
            for (int i = 0; i < additionalBaseColors.Length; i++)
                if (additionalTargets[i] != null) additionalBaseColors[i] = additionalTargets[i].color;
        }

        private void OnEnable() => health.OnDamaged += Flash;

        private void OnDisable()
        {
            health.OnDamaged -= Flash;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            if (target != null) target.color = baseColor;
            for (int i = 0; i < additionalBaseColors.Length; i++)
                if (additionalTargets[i] != null) additionalTargets[i].color = additionalBaseColors[i];
        }

        private void Flash(DamageInfo info)
        {
            if (target == null) return;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            target.color = flashColor;
            for (int i = 0; i < additionalBaseColors.Length; i++)
                if (additionalTargets[i] != null) additionalTargets[i].color = flashColor;
            yield return new WaitForSecondsRealtime(duration);
            if (target != null) target.color = baseColor;
            for (int i = 0; i < additionalBaseColors.Length; i++)
                if (additionalTargets[i] != null) additionalTargets[i].color = additionalBaseColors[i];
            flashRoutine = null;
        }

#if UNITY_EDITOR
        public void ConfigureAppearance(Color color, float seconds)
        {
            flashColor = color;
            duration = seconds;
        }

        public void Configure(SpriteRenderer renderer, params SpriteRenderer[] others)
        {
            target = renderer;
            additionalTargets = others;
        }
#endif
    }
}
