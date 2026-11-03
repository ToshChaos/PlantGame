using UnityEditor;
using UnityEngine;

namespace PlantGame.Editor
{
    /// <summary>One-click rebuild of everything T00–T03 generate. Menu: PlantGame > Rebuild Core Assets.</summary>
    public static class BuildAll
    {
        [MenuItem("PlantGame/Rebuild Core Assets", false, 100)]
        public static void Run()
        {
            ProjectSetup.Run();
            MaterialBuilder.Build();
            ItemBuilder.Build();
            PlayerPrefabBuilder.Build();
            SandboxGameplayBuilder.Build();
            ProjectSetup.RegisterScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] Rebuild Core Assets finished (T00–T03 assets).");
        }
    }
}
