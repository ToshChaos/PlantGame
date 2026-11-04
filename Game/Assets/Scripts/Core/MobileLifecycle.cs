using UnityEngine;
using PlantGame.UI;

namespace PlantGame.Core
{
    /// <summary>
    /// T62: phone behaviour that has no desktop equivalent. Landscape only, screen never sleeps, 30 or 60 fps target (GameSettings.Fps30), memory warnings free what they can, and the run
    /// auto-pauses when the app loses focus (home, lock, call, Control Center, task switch; there is no Back button, so the Pause button is the way back in) so the run clock stops and the player
    /// returns to the pause menu. Created once at startup; does nothing on desktop.
    /// </summary>
    public class MobileLifecycle : MonoBehaviour
    {
        const float LowMemoryCooldown = 10f;
        static float _nextFree;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            _nextFree = 0f;
            if (!Application.isMobilePlatform) return;
            Application.lowMemory += OnLowMemory;
            Debug.developerConsoleVisible = false;   // the on-screen Development Console is for developers; errors still go to the device log
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = GameSettings.Fps30 ? 30 : 60;
            GameSettings.Changed += () => Application.targetFrameRate = GameSettings.Fps30 ? 30 : 60;
            Screen.orientation = ScreenOrientation.AutoRotation;   // the allowed set comes from Player Settings (IosBuild / AndroidBuild: landscape left / right only)
            var go = new GameObject("MobileLifecycle");
            DontDestroyOnLoad(go);
            go.AddComponent<MobileLifecycle>();
        }

        // T72: iOS warns before it kills an app over its memory limit, and the kill itself has no callback. A warning can repeat quickly and each pass hitches, hence the cooldown.
        static void OnLowMemory()
        {
            if (Time.realtimeSinceStartup < _nextFree) return;
            _nextFree = Time.realtimeSinceStartup + LowMemoryCooldown;
            Debug.LogWarning("[PlantGame] Low memory warning: unloading unused assets.");
            Resources.UnloadUnusedAssets();
            System.GC.Collect();
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            PlayerPrefs.Save();   // Android and iOS may kill the process while we are in the background: keep best times and the last seed
            PauseRun();
        }

        // iOS: the Control Center / Notification Center pull-downs, a call banner, Siri and the app switcher take focus without pausing the app
        // (OnApplicationPause fires only for home, lock and the switcher), so the run would keep going behind them. Pause on focus loss too.
        void OnApplicationFocus(bool focus)
        {
            if (focus) return;
            PlayerPrefs.Save();
            PauseRun();
        }

        static void PauseRun()
        {
            var hud = FindAnyObjectByType<PlayerHud>();
            if (hud != null && !hud.Paused) hud.SetPaused(true);
        }
    }
}
