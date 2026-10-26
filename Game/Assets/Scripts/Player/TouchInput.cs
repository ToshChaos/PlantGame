using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Player
{
    /// <summary>
    /// T57: per-frame state of the on-screen touch controls. TouchControls fills it (before everything else runs),
    /// PlayerInputReader is the only reader. Pressed flags live for exactly one frame.
    /// </summary>
    public static class TouchInput
    {
        /// <summary>Touch controls are shown: a phone / tablet build, or the editor with GameSettings.TouchControls on.</summary>
        public static bool Active => Application.isMobilePlatform || GameSettings.TouchControls;

        public static Vector2 Move;          // left stick, 0..1 length, dead zone already removed
        public static bool Run;              // stick pushed to the edge
        public static Vector2 Look;          // this frame's right-side drag, in mouse counts (PlayerController.lookSensitivity scales it)
        public static bool JumpPressed, ContextPressed, WaterPressed, CyclePressed, PausePressed;

        public static void ClearFrame()
        {
            Look = Vector2.zero;
            JumpPressed = ContextPressed = WaterPressed = CyclePressed = PausePressed = false;
        }

        public static void ClearAll() { ClearFrame(); Move = Vector2.zero; Run = false; }
    }
}
