using UnityEngine;
using UnityEngine.UI;

namespace Relicfall.UI
{
    public sealed class DamageNumberPresenter
    {
        private const float Lifetime = 0.72f;
        private readonly RectTransform root;
        private readonly Number[] numbers;

        private sealed class Number
        {
            public Text Text;
            public RectTransform Rect;
            public Vector3 Point;
            public float Born;
            public bool Active;
        }

        public DamageNumberPresenter(RectTransform root, int capacity)
        {
            this.root = root;
            numbers = new Number[Mathf.Max(1, capacity)];
            for (int i = 0; i < numbers.Length; i++)
            {
                Text text = UguiTheme.Label(root, "Number" + i, 0, 0, 100, 40,
                    "", 26, UguiTheme.Gold, TextAnchor.MiddleCenter);
                var rect = (RectTransform)text.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
                text.gameObject.SetActive(false);
                numbers[i] = new Number { Text = text, Rect = rect };
            }
        }

        public void Show(Vector3 point, int damage, bool player)
        {
            Number chosen = numbers[0];
            foreach (Number number in numbers)
            {
                if (!number.Active) { chosen = number; break; }
                if (number.Born < chosen.Born) chosen = number;
            }
            chosen.Point = point;
            chosen.Born = Time.unscaledTime;
            chosen.Active = true;
            chosen.Text.text = damage.ToString();
            chosen.Text.color = player ? UguiTheme.Red : UguiTheme.Gold;
        }

        public void Clear()
        {
            foreach (Number number in numbers)
            {
                number.Active = false;
                number.Text.gameObject.SetActive(false);
            }
        }

        public void Tick(Camera camera, bool visible)
        {
            foreach (Number number in numbers)
            {
                if (!number.Active && !number.Text.gameObject.activeSelf) continue;
                float age = Time.unscaledTime - number.Born;
                if (age >= Lifetime) number.Active = false;
                bool show = number.Active && camera != null && visible;
                if (show)
                {
                    Vector3 screen = camera.WorldToScreenPoint(number.Point);
                    Vector2 local = Vector2.zero;
                    show = screen.z >= 0 && RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screen, null, out local);
                    if (show) number.Rect.anchoredPosition = local + Vector2.up * age * 55f;
                }
                if (number.Text.gameObject.activeSelf != show) number.Text.gameObject.SetActive(show);
            }
        }
    }
}
