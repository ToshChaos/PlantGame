using UnityEditor;
using UnityEngine;

namespace PlantGame.Editor
{
    /// <summary>One-click rebuild of the core assets (project setup, materials, items, player prefab, sandbox). Menu: PlantGame > Rebuild Core Assets.</summary>
    public static class BuildAll
    {
        [MenuItem("PlantGame/Rebuild Core Assets", false, 1)]
        public static void Run()
        {
            ProjectSetup.Run();
            MaterialBuilder.Build();
            ItemBuilder.Build();
            PlayerPrefabBuilder.Build();
            SandboxGameplayBuilder.Build();
            ProjectSetup.RegisterScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] Rebuild Core Assets finished.");
        }
    }
}
