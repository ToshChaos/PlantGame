using UnityEngine;
using UnityEngine.UI;
using PlantGame.Player;

namespace PlantGame.UI
{
    /// <summary>Shared canvas rule: on phones the canvas height stays 1080 reference units (wide screens give extra width, not extra height), so nothing authored for +-540 is cut off.</summary>
    public static class UiLayout
    {
        /// <summary>CanvasScaler match value: height on wide touch screens, the old half-and-half on desktop and on near-square tablets.</summary>
        /// <summary>Text size multiplier: 1.5x on desktop, 2x on phones. Large titles (60 and up) keep their size.</summary>
        public static float TextScale => TouchInput.Active ? 2f : 1.5f;

        /// <summary>Menus (title, pause, end screens) scale less than the HUD: 1.2x desktop, 1.4x phones.</summary>
        public static float MenuScale => TouchInput.Active ? 1.4f : 1.2f;

        public static int Font(int size, bool menu = false) => size >= 60 ? size : Mathf.RoundToInt(size * (menu ? MenuScale : TextScale));

        /// <summary>Applies the scaled size; the text shrinks (never below the original size) when its box is too small, so buttons and cards still fit.</summary>
        public static void Fit(Text t, int baseSize, bool menu = false)
        {
            if (baseSize >= 60) { t.fontSize = baseSize; return; }
            t.fontSize = Font(baseSize, menu);
            t.resizeTextForBestFit = true; t.resizeTextMinSize = baseSize; t.resizeTextMaxSize = t.fontSize;
            t.verticalOverflow = VerticalWrapMode.Overflow;
        }

        public static float MatchHeight => TouchInput.Active && Screen.width >= Screen.height * 1.77f ? 1f : 0.5f;
    }
}
