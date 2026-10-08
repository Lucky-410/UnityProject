using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Relicfall.UI
{
    // 使用 UGUI 的原生状态和点击判定，额外表现业务选中状态。
    public sealed class UguiFeedbackButton : Button
    {
        public enum VisualState { Normal, Hover, Focus, Chosen, Pressed, Disabled }

        [Header("Interaction feedback")]
        [SerializeField] private Color normalTint = new(0.78f, 0.86f, 0.98f, 1);
        [SerializeField] private Color hoverColor = new(0.38f, 0.88f, 1f, 1);
        [SerializeField] private Color chosenColor = new(1f, 0.82f, 0.32f, 1);
        [SerializeField, Range(0.02f, 0.3f)] private float fadeDuration = 0.10f;
        [SerializeField, Range(1f, 1.08f)] private float hoverScale = 1.025f;
        [SerializeField, Range(0.85f, 1f)] private float pressedScale = 0.96f;
        [SerializeField] private Text caption;
        [SerializeField] private Image wash, chosenMark;
        [SerializeField] private Image[] frame;
        [SerializeField] private CanvasGroup opacity;
        private SelectionState nativeState;
        private bool chosen, pointerInside, bound;
        private Coroutine animation;
        private Vector3 restScale = Vector3.one;
        public bool IsChosen => chosen;
        public VisualState State { get; private set; }

        public void Bind(Text text, Image glow, Image[] edges, Image mark, CanvasGroup fade)
        {
            caption = text;
            wash = glow;
            frame = edges;
            chosenMark = mark;
            opacity = fade;
            restScale = transform.localScale;
            bound = true;
            Refresh(true);
        }

        public void SetChosen(bool value)
        {
            if (chosen == value) return;
            chosen = value;
            Refresh(!Application.isPlaying);
        }

        protected override void DoStateTransition(SelectionState state, bool instant)
        {
            nativeState = state;
            Refresh(instant);
        }

        public override void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            base.OnPointerEnter(eventData);
            Refresh(false);
        }
        public override void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            base.OnPointerExit(eventData);
            Refresh(false);
        }

        protected override void OnEnable()
        {
            if (caption != null && wash != null && frame != null && opacity != null)
            {
                bound = true;
                restScale = transform.localScale;
            }
            pointerInside = false;
            base.OnEnable();
        }
        protected override void OnDisable()
        {
            if (animation != null) StopCoroutine(animation);
            animation = null;
            pointerInside = false;
            base.OnDisable();
            nativeState = SelectionState.Normal;
            Refresh(true);
        }

        private void Refresh(bool instant)
        {
            if (!bound || targetGraphic == null) return;
            bool disabled = nativeState == SelectionState.Disabled || !IsInteractable();
            State = disabled ? VisualState.Disabled : nativeState == SelectionState.Pressed ? VisualState.Pressed :
                pointerInside ? VisualState.Hover : chosen ? VisualState.Chosen :
                nativeState == SelectionState.Selected ? VisualState.Focus : VisualState.Normal;
            Color border = Color.clear, glow = Color.clear, tint = normalTint, text = UguiTheme.Pale;
            float scale = 1f, alpha = disabled ? 0.42f : 1f;
            if (State == VisualState.Hover)
            {
                border = chosen ? chosenColor : hoverColor;
                glow = new Color(border.r, border.g, border.b, 0.18f);
                tint = Color.white;
                text = Color.white;
                scale = hoverScale;
            }
            else if (State == VisualState.Pressed)
            {
                border = chosenColor;
                glow = new Color(1, 0.6f, 0.2f, 0.18f);
                tint = new Color(0.50f, 0.62f, 0.80f);
                text = chosenColor;
                scale = pressedScale;
            }
            else if (State == VisualState.Chosen || State == VisualState.Focus)
            {
                border = State == VisualState.Chosen ? chosenColor : hoverColor;
                glow = new Color(border.r, border.g, border.b, 0.10f);
                tint = Color.white;
                text = State == VisualState.Chosen ? chosenColor : Color.white;
            }
            if (chosenMark != null) chosenMark.enabled = chosen && !disabled;
            if (animation != null) StopCoroutine(animation);
            animation = null;
            if (instant || !Application.isPlaying || !isActiveAndEnabled)
                Apply(tint, border, glow, text, scale, alpha);
            else animation = StartCoroutine(Animate(tint, border, glow, text, scale, alpha));
        }

        private IEnumerator Animate(Color tint, Color border, Color glow, Color text, float scale, float alpha)
        {
            Color fromTint = targetGraphic.color, fromBorder = frame[0].color, fromGlow = wash.color;
            Color fromText = caption.color;
            Vector3 fromScale = transform.localScale;
            float fromAlpha = opacity.alpha;
            float started = Time.unscaledTime;
            while (Time.unscaledTime - started < fadeDuration)
            {
                float t = Mathf.SmoothStep(0, 1, (Time.unscaledTime - started) / fadeDuration);
                targetGraphic.color = Color.Lerp(fromTint, tint, t);
                wash.color = Color.Lerp(fromGlow, glow, t);
                foreach (Image edge in frame) edge.color = Color.Lerp(fromBorder, border, t);
                caption.color = Color.Lerp(fromText, text, t);
                transform.localScale = Vector3.Lerp(fromScale, restScale * scale, t);
                opacity.alpha = Mathf.Lerp(fromAlpha, alpha, t);
                yield return null;
            }
            Apply(tint, border, glow, text, scale, alpha);
            animation = null;
        }

        private void Apply(Color tint, Color border, Color glow, Color text, float scale, float alpha)
        {
            targetGraphic.color = tint;
            wash.color = glow;
            foreach (Image edge in frame) edge.color = border;
            caption.color = text;
            transform.localScale = restScale * scale;
            opacity.alpha = alpha;
        }
    }
}
