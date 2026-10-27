using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlantGame.Core;
using PlantGame.World;

namespace PlantGame.Editor
{
    /// <summary>
    /// Scenes that run the runtime WorldGenerator. Main.unity takes its RunConfig from GameBootstrap (the menu); Sandbox_PlantTest is
    /// Level 1's puzzle built collector-only (no water source: the Water Collector plant is the only water). Menu: PlantGame > 9 / 12.
    /// </summary>
    public static class SandboxPlantTestBuilder
    {
        /// <summary>Main.unity: generator + bootstrap, atmosphere, audio and VFX (menu: PlantGame > 12 Build Main).</summary>
        public static void BuildMain() { Create("Main", true); }

        static void Create(string sceneName, bool main)
        {
            WorldKitBuilder.Build();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ProjectSetup.ApplyLightingPreset();
            ProjectSetup.AddSun();
            var runGo = new GameObject("RunSession");
            var run = runGo.AddComponent<RunSession>();
            run.levelId = main ? GameBootstrap.Level1Id : "sandbox_plants";
            var gen = new GameObject("World").AddComponent<WorldGenerator>();
            if (!main)
            {
                gen.useBootstrapConfig = false;
                gen.sandboxConfig = GameBootstrap.DefaultConfig();
                gen.sandboxConfig.waterMode = WaterMode.CollectorOnly;
                gen.sandboxConfig.levelId = GameBootstrap.HandLevelId;
            }
            MainSceneBuilder.AddAtmosphere();
            if (main) MainSceneBuilder.AddSystems();
            EditorSceneManager.SaveScene(scene, ProjectSetup.ScenePath(sceneName));
            ProjectSetup.RegisterScenes();
            Debug.Log($"[PlantGame] {sceneName} built (world is generated at runtime).");
        }
    }
}
