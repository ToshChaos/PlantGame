using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Player
{
    /// <summary>
    /// T57: per-frame state of the on-screen touch controls. TouchControls fills it (before everything else runs),
    /// PlayerInputReader and PlayerInteraction are the readers. Pressed flags live for exactly one frame.
    /// </summary>
    public static class TouchInput
    {
        /// <summary>Touch controls are shown: a phone / tablet build, or the editor with GameSettings.TouchControls on.</summary>
        public static bool Active => Application.isMobilePlatform || GameSettings.TouchControls;

        public static Vector2 Move;          // tap-to-walk (camera-relative, length 1) or the climb direction (0, +-1)
        public static Vector2 Look;          // this frame's drag, in mouse counts (PlayerController.lookSensitivity scales it)
        public static Vector3 WorldMove;     // tap-to-walk: walk along this world direction (no camera-yaw round trip, so the camera follow cannot feed back into it)
        public static Vector3 FaceDir;       // tap-to-walk: turn the standing player to this world direction first (zero = none)
        public static bool JumpPressed, PausePressed;

        /// <summary>Set for one frame when the player tapped a marker (circle / diamond) in reach: PlayerInteraction acts on it.</summary>
        public static bool HasTap;
        public static InteractionSensor.Target Tap;

        public static void ClearFrame()
        {
            Look = Vector2.zero;
            JumpPressed = PausePressed = HasTap = false;
            Tap = default;
        }

        public static void ClearAll() { ClearFrame(); Move = Vector2.zero; WorldMove = Vector3.zero; FaceDir = Vector3.zero; }
    }
}
