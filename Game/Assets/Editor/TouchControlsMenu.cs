using UnityEditor;
using PlantGame.Core;

namespace PlantGame.Editor
{
    /// <summary>T57: show the on-screen touch controls in the editor (the mouse then acts as one finger via TouchSimulation).</summary>
    public static class TouchControlsMenu
    {
        const string Path = "PlantGame/Touch Controls (editor test)";

        [MenuItem(Path, false, 900)]
        static void Toggle() { GameSettings.TouchControls = !GameSettings.TouchControls; }

        [MenuItem(Path, true)]
        static bool Validate() { Menu.SetChecked(Path, GameSettings.TouchControls); return true; }
    }
}
