using UnityEngine;
using PlantGame.UI;

namespace PlantGame.Core
{
    /// <summary>
    /// T62: phone behaviour that has no desktop equivalent. Landscape only, screen never sleeps, 60 fps target, and the run
    /// auto-pauses when the app loses focus (home button, call, task switch) so the Challenge countdown stops and the player
    /// returns to the pause menu. Created once at startup; does nothing on desktop.
    /// </summary>
    public class MobileLifecycle : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (!Application.isMobilePlatform) return;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Application.targetFrameRate = 60;
            Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;   // landscape left / right only (the allowed set above)
            var go = new GameObject("MobileLifecycle");
            DontDestroyOnLoad(go);
            go.AddComponent<MobileLifecycle>();
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused) return;
            PlayerPrefs.Save();   // Android may kill the process while we are in the background: keep best times and the last seed
            var hud = FindAnyObjectByType<PlayerHud>();
            if (hud != null && !hud.Paused) hud.SetPaused(true);
        }
    }
}
