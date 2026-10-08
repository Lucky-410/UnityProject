using System;
using System.Collections.Generic;
using Relicfall.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Relicfall.UI
{
    public static class UguiTheme
    {
        public static readonly Color Ink = new Color(0.025f, 0.045f, 0.10f, 0.96f);
        public static readonly Color Gold = new Color(0.94f, 0.78f, 0.43f);
        public static readonly Color Pale = new Color(0.90f, 0.94f, 0.97f);
        public static readonly Color Muted = new Color(0.55f, 0.69f, 0.80f);
        public static readonly Color Red = new Color(0.80f, 0.18f, 0.26f);
        public static readonly Color Blue = new Color(0.12f, 0.64f, 0.88f);
        private static Font font;
        private static readonly Dictionary<string, Sprite> sprites = new();
        public static readonly string[] StyleNames =
        {
            "Base Bg 1", "Base Bg 3", "Header", "Progress Bar Top", "Round Bg B", "Small Bg",
            "Icons/Heart", "Icons/Energy"
        };

        public static Vector4 Border(string name) => name switch
        {
            "Base Bg 1" => Vector4.one * 6,
            "Base Bg 3" => Vector4.one * 4,
            "Header" => Vector4.one * 4,
            "Small Bg" => Vector4.one * 5,
            "Progress Bar Top" => new Vector4(3, 3, 3, 3),
            _ => Vector4.zero
        };

        public static Font Font
        {
            get
            {
                if (!Application.isPlaying) return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null)
                    font = UnityEngine.Font.CreateDynamicFontFromOSFont(
                        new[] { "Microsoft YaHei", "Noto Sans CJK SC", "PingFang SC" }, 20);
                return font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
        }

        public static Sprite Style(string name)
        {
            if (sprites.TryGetValue(name, out Sprite cached) && cached != null) return cached;
            Sprite sprite = Resources.Load<Sprite>("UI/Styles/" + name);
            if (sprite == null)
            {
                Texture2D texture = Resources.Load<Texture2D>("PixelUIkit/Pixel UI kit/PNGs/" + name);
                if (texture != null)
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                        Vector2.one * 0.5f, 100f, 0, SpriteMeshType.FullRect, Border(name));
            }
            sprites[name] = sprite;
            return sprite;
        }

        public static RectTransform Node(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null) return (RectTransform)existing;
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(Transform parent, string name, float x, float y, float w, float h,
            Vector2? anchor = null)
        {
            bool exists = parent.Find(name) != null;
            RectTransform rect = Node(parent, name);
            if (exists) return rect;
            rect.anchorMin = rect.anchorMax = anchor ?? new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(w, h);
            return rect;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            bool exists = parent.Find(name) != null;
            RectTransform rect = Node(parent, name);
            if (exists) return rect;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        public static T Component<T>(GameObject go) where T : Component =>
            go.TryGetComponent(out T component) ? component : go.AddComponent<T>();

        public static Image Image(RectTransform rect, Color color, string style = null, bool raycast = false)
        {
            Image image = Component<Image>(rect.gameObject);
            image.color = color;
            image.sprite = style != null ? Style(style) : null;
            image.type = image.sprite != null && image.sprite.border != Vector4.zero ?
                UnityEngine.UI.Image.Type.Sliced : UnityEngine.UI.Image.Type.Simple;
            image.raycastTarget = raycast;
            return image;
        }

        public static RectTransform Panel(Transform parent, string name, float x, float y, float w, float h)
        {
            RectTransform rect = Place(parent, name, x, y, w, h);
            Image(rect, Color.white, "Base Bg 1", true);
            RectTransform interior = Stretch(rect, "Interior");
            interior.offsetMin = new Vector2(12, 12);
            interior.offsetMax = new Vector2(-12, -12);
            Image(interior, new Color(0, 0, 0, 0.12f));
            interior.SetAsFirstSibling();
            return rect;
        }

        public static Text Label(Transform parent, string name, float x, float y, float w, float h,
            string value = "", int size = 18, Color? color = null, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            Text text = Component<Text>(Place(parent, name, x, y, w, h).gameObject);
            text.font = Font;
            text.fontSize = size;
            text.color = color ?? Pale;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        public static Button Button(Transform parent, string name, float x, float y, float w, float h,
            string title, Action click)
        {
            RectTransform rect = Place(parent, name, x, y, w, h);
            Image image = Image(rect, Color.white, "Base Bg 3", true);
            UguiFeedbackButton button = Component<UguiFeedbackButton>(rect.gameObject);
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            Text label = Label(rect, "Label", 8, 2, w - 16, h - 4, title, 18, Pale, TextAnchor.MiddleCenter);
            RectTransform glowRect = Stretch(rect, "StateWash");
            glowRect.offsetMin = new Vector2(4, 4);
            glowRect.offsetMax = new Vector2(-4, -4);
            Image glow = Image(glowRect, Color.clear);
            glowRect.SetAsFirstSibling();
            RectTransform border = Stretch(rect, "StateFrame");
            border.offsetMin = new Vector2(2, 2);
            border.offsetMax = new Vector2(-2, -2);
            Image[] edges = new Image[4];
            for (int i = 0; i < edges.Length; i++)
            {
                RectTransform edge = Stretch(border, "Edge" + i);
                edge.anchorMin = i == 0 ? new Vector2(0, 1) : Vector2.zero;
                edge.anchorMax = i == 1 ? new Vector2(1, 0) : i == 2 ? new Vector2(0, 1) : Vector2.one;
                edge.pivot = i == 0 ? new Vector2(0.5f, 1) : i == 1 ? new Vector2(0.5f, 0) :
                    i == 2 ? new Vector2(0, 0.5f) : new Vector2(1, 0.5f);
                if (i == 3) edge.anchorMin = new Vector2(1, 0);
                edge.sizeDelta = i < 2 ? new Vector2(0, 2) : new Vector2(2, 0);
                edges[i] = Image(edge, Color.clear);
            }
            Image mark = Image(Place(rect, "ChosenMark", -15, 7, 7, 7, new Vector2(1, 1)), Gold);
            mark.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            mark.enabled = false;
            border.SetAsLastSibling();
            mark.transform.SetAsLastSibling();
            button.Bind(label, glow, edges, mark, Component<CanvasGroup>(rect.gameObject));
            button.onClick.RemoveAllListeners();
            if (click != null) button.onClick.AddListener(() =>
            {
                AudioDirector.Instance?.PlayClick();
                click();
            });
            return button;
        }

        public static Slider Bar(Transform parent, string name, float x, float y, float w, float h, Color color)
        {
            RectTransform rect = Place(parent, name, x, y, w, h);
            Image(rect, Color.white, "Progress Bar Top");
            RectTransform fillArea = Stretch(rect, "FillArea");
            fillArea.offsetMin = new Vector2(6, 6);
            fillArea.offsetMax = new Vector2(-6, -6);
            RectTransform fill = Stretch(fillArea, "Fill");
            Image(fill, color);
            Slider slider = Component<Slider>(rect.gameObject);
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.wholeNumbers = false;
            slider.fillRect = fill;
            slider.handleRect = null;
            slider.interactable = false;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }

        public static void Icon(Image image, Sprite sprite)
        {
            if (image.sprite == sprite && image.enabled == (sprite != null)) return;
            image.sprite = sprite;
            image.type = UnityEngine.UI.Image.Type.Simple;
            image.preserveAspect = true;
            image.enabled = sprite != null;
        }

        public static Image Art(Transform parent, string name, float x, float y, float w, float h, string style)
        {
            Image image = Image(Place(parent, name, x, y, w, h), Color.white, style);
            image.preserveAspect = image.type == UnityEngine.UI.Image.Type.Simple;
            return image;
        }

        public static Text Header(Transform parent, string name, float x, float y, float w, float h,
            string title, int size = 26)
        {
            RectTransform rect = Place(parent, name, x, y, w, h);
            Image(rect, Color.white, "Header");
            return Label(rect, "Label", 12, 4, w - 24, h - 8, title, size, Pale, TextAnchor.MiddleCenter);
        }

        public static CanvasGroup Page(Transform parent, string name, bool modal = false)
        {
            RectTransform rect = Stretch(parent, name);
            if (modal) Image(rect, new Color(0.01f, 0.02f, 0.05f, 0.72f), null, true);
            return Component<CanvasGroup>(rect.gameObject);
        }
    }
}
