using UnityEditor;
using UnityEngine;

namespace PlantGame.Editor
{
    /// <summary>One-click rebuild of everything T00–T03 generate. Menu: PlantGame > Build All.</summary>
    public static class BuildAll
    {
        [MenuItem("PlantGame/Build All")]
        public static void Run()
        {
            ProjectSetup.Run();
            MaterialBuilder.Build();
            PlayerPrefabBuilder.Build();
            SandboxPlantsBuilder.Build();
            SandboxGameplayBuilder.Build();
            ProjectSetup.RegisterScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] Build All finished (T00–T03 assets).");
        }
    }
}
