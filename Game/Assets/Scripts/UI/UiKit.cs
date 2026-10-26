using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using PlantGame.Core;

namespace PlantGame.UI
{
    /// <summary>Small uGUI builders shared by the title screen and the in-game menus (no prefabs).</summary>
    public static class UiKit
    {
        /// <summary>A labelled 0..1 slider: track, green vine fill, mushroom handle, and a caption above it.</summary>
        public static Slider Slider(Transform parent, Font font, string caption, Vector2 pos, float width, float value, UnityAction<float> onChange)
        {
            var root = Rect("Slider_" + caption, parent, new Vector2(width, 40), pos);
            var label = Rect("Caption", root, new Vector2(width, 36), new Vector2(0, 34)).gameObject.AddComponent<Text>();
            label.font = font; UiLayout.Fit(label, 28); label.alignment = TextAnchor.MiddleCenter; label.color = Palette.TextOrange; label.raycastTarget = false;

            var track = Rect("Track", root, new Vector2(width, 14), new Vector2(0, -16));
            var trackImg = track.gameObject.AddComponent<Image>(); trackImg.color = new Color(0.04f, 0.14f, 0.08f, 0.95f);

            var fillArea = Rect("FillArea", root, new Vector2(width - 20, 14), new Vector2(0, -16));
            var fill = Rect("Fill", fillArea, new Vector2(10, 14), Vector2.zero);
            fill.anchorMin = new Vector2(0, 0); fill.anchorMax = new Vector2(0, 1); fill.pivot = new Vector2(0, 0.5f); fill.sizeDelta = new Vector2(10, 0);
            var fillImg = fill.gameObject.AddComponent<Image>(); fillImg.color = Palette.LeafGreen;   // a vine growing along the track

            var handleArea = Rect("HandleArea", root, new Vector2(width - 20, 30), new Vector2(0, -16));
            var handle = Rect("Handle", handleArea, new Vector2(38, 38), Vector2.zero);
            var handleImg = handle.gameObject.AddComponent<Image>(); handleImg.color = Palette.CloudWhite; handleImg.sprite = UiSprites.RoundMushroom;   // a round mushroom riding the vine

            var s = root.gameObject.AddComponent<Slider>();
            s.fillRect = fill; s.handleRect = handle; s.targetGraphic = handleImg; s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.minValue = 0f; s.maxValue = 1f; s.wholeNumbers = false; s.SetValueWithoutNotify(value);
            void Show(float v) { label.text = caption; }
            Show(value);
            s.onValueChanged.AddListener(v => { Show(v); onChange(v); });
            return s;
        }

        static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            return rt;
        }
    }
}
