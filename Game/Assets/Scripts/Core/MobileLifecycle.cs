using UnityEngine;
using PlantGame.UI;

namespace PlantGame.Core
{
    /// <summary>
    /// T62: phone behaviour that has no desktop equivalent. Landscape only, screen never sleeps, 60 fps target, and the run
    /// auto-pauses when the app loses focus (home, lock, call, Control Center, task switch; there is no Back button, so the Pause button is the way back in) so the run clock stops and the player
    /// returns to the pause menu. Created once at startup; does nothing on desktop.
    /// </summary>
    public class MobileLifecycle : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (!Application.isMobilePlatform) return;
            Debug.developerConsoleVisible = false;   // the on-screen Development Console is for developers; errors still go to the device log
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = GameSettings.Fps30 ? 30 : 60;
            GameSettings.Changed += () => Application.targetFrameRate = GameSettings.Fps30 ? 30 : 60;
            Screen.orientation = ScreenOrientation.AutoRotation;   // the allowed set comes from Player Settings (IosBuild / AndroidBuild: landscape left / right only)
            var go = new GameObject("MobileLifecycle");
            DontDestroyOnLoad(go);
            go.AddComponent<MobileLifecycle>();
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
