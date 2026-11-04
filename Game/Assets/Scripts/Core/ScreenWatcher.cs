using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PlantGame.UI;

namespace PlantGame.Core
{
    /// <summary>
    /// T70: phones rotate and iPads (iPadOS 26) resize windows while the game runs, but menus are laid out once and the render scale is set on
    /// settings changes only. This watches the screen size on touch platforms: it re-fits every CanvasScaler, re-applies the rendered line count,
    /// and reloads the title scene when the phone column / tablet layout choice flips (the title keeps no state). The HUD only depends on the scaler.
    /// </summary>
    public class ScreenWatcher : MonoBehaviour
    {
        int _w, _h;
        bool _wide;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (!Application.isMobilePlatform) return;
            var go = new GameObject("ScreenWatcher");
            DontDestroyOnLoad(go);
            go.AddComponent<ScreenWatcher>();
        }

        void Start() { _w = Screen.width; _h = Screen.height; _wide = UiLayout.WideTouch; }

        void Update()
        {
            if (Screen.width == _w && Screen.height == _h) return;
            _w = Screen.width; _h = Screen.height;
            QualityTier.Rescale();
            foreach (var s in FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include))
                s.matchWidthOrHeight = UiLayout.MatchHeight;
            bool wide = UiLayout.WideTouch;
            if (wide != _wide)
            {
                _wide = wide;
                if (FindAnyObjectByType<TitleMenu>() != null) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }
}
