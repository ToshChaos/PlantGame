using UnityEditor;
using UnityEditor.SceneManagement;

namespace PlantGame.Editor
{
    /// <summary>Pressing Play always starts at the Title scene (the real menu), whichever scene is open. Toggle: PlantGame > Play From Title.</summary>
    [InitializeOnLoad]
    public static class PlayFromTitle
    {
        const string Pref = "PlantGame.PlayFromTitle", TitlePath = "Assets/Scenes/Title.unity", Menu = "PlantGame/Play From Title";

        static PlayFromTitle() { EditorApplication.delayCall += Apply; }

        static bool On => EditorPrefs.GetBool(Pref, true);

        static void Apply()
        {
            var title = On ? AssetDatabase.LoadAssetAtPath<SceneAsset>(TitlePath) : null;
            EditorSceneManager.playModeStartScene = title;
        }

        [MenuItem(Menu)] static void Toggle() { EditorPrefs.SetBool(Pref, !On); Apply(); }
        [MenuItem(Menu, true)] static bool ToggleValidate() { UnityEditor.Menu.SetChecked(Menu, On); return true; }
    }
}
