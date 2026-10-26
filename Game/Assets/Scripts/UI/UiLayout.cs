using UnityEngine;
using PlantGame.Player;

namespace PlantGame.UI
{
    /// <summary>Shared canvas rule: on phones the canvas height stays 1080 reference units (wide screens give extra width, not extra height), so nothing authored for +-540 is cut off.</summary>
    public static class UiLayout
    {
        /// <summary>CanvasScaler match value: height on wide touch screens, the old half-and-half on desktop and on near-square tablets.</summary>
        public static float MatchHeight => TouchInput.Active && Screen.width >= Screen.height * 1.77f ? 1f : 0.5f;
    }
}
