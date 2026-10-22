using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlantGame.Core;
using PlantGame.Player;

namespace PlantGame.Editor
{
    /// <summary>
    /// T03: builds Sandbox_Gameplay with the tuning course from the task:
    /// start floor, 5 m gap (jumpable, +Z), 7 m gap (not jumpable, +X), 1.6 m ledge (-Z), stairs, a 30 degree ramp
    /// and a 6 m tower with a ladder (W/S climb, Space jumps off). Menu: PlantGame > 5 Build Sandbox_Gameplay.
    /// </summary>
    public static class SandboxGameplayBuilder
    {
        [MenuItem("PlantGame/5 Build Sandbox_Gameplay")]
        public static void Build()
        {
            if (MaterialBuilder.Load("M_Terrain") == null) MaterialBuilder.Build();
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabBuilder.PrefabPath) ?? PlayerPrefabBuilder.Build();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SandboxPlantsBuilder.ApplyLightingPreset();
            ProjectSetup.AddSun();

            var terrain = MaterialBuilder.Load("M_Terrain");
            var stone = MaterialBuilder.Load("M_Stone");
            var foliage = MaterialBuilder.Load("M_Foliage");
            var crystal = MaterialBuilder.Load("M_Crystal");

            // Floors (top surface at y = 0)
            Box("Floor_Start", new Vector3(-7.5f, -0.5f, -3f), new Vector3(25f, 1f, 16f), terrain);      // x -20..5, z -11..5
            Box("Floor_Gap5_Far", new Vector3(0f, -0.5f, 15f), new Vector3(10f, 1f, 10f), terrain);       // z 10..20  (5 m gap from z=5)
            Box("Floor_Gap7_Far", new Vector3(17f, -0.5f, 0f), new Vector3(10f, 1f, 10f), terrain);       // x 12..22  (7 m gap from x=5)
            Marker("Marker_Gap5m", new Vector3(0f, 0.5f, 4.6f), crystal);
            Marker("Marker_Gap7m", new Vector3(4.6f, 0.5f, 0f), crystal);

            // 1.6 m ledge (jump height) at -Z
            Box("Ledge_1.6m", new Vector3(0f, 0.8f, -8f), new Vector3(6f, 1.6f, 2f), stone);

            // Stairs: 4 steps, 0.3 m rise each (step offset 0.35)
            for (int i = 0; i < 4; i++)
            {
                float h = 0.3f * (i + 1);
                Box("Stair_" + i, new Vector3(-3f, h * 0.5f, 3.5f - i * 0.6f), new Vector3(2f, h, 0.6f), stone);
            }

            // 30 degree ramp
            var ramp = Box("Ramp_30deg", new Vector3(3.5f, 1.0f, 1.5f), new Vector3(2f, 0.2f, 4f), stone);
            ramp.transform.rotation = Quaternion.Euler(-30f, 0f, 0f);

            // Tower 6 m with a ladder on its +X face
            Box("Tower_6m", new Vector3(-12f, 3f, 0f), new Vector3(6f, 6f, 6f), stone);   // x -15..-9, top y = 6
            var visual = Box("Ladder_Visual", new Vector3(-8.85f, 3.3f, 0f), new Vector3(0.15f, 6.6f, 0.8f), foliage);
            Object.DestroyImmediate(visual.GetComponent<Collider>());                      // the climb volume is the only physics

            var land = new GameObject("Ladder_Land");
            land.transform.position = new Vector3(-9.6f, 6f, 0f);

            var volGo = new GameObject("Ladder_ClimbVolume");
            volGo.transform.position = new Vector3(-8.6f, 3.4f, 0f);
            volGo.transform.rotation = Quaternion.LookRotation(Vector3.right);             // forward = away from the tower face
            var box = volGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.4f, 6.8f, 2.4f);
            var layer = LayerMask.NameToLayer(Layers.ClimbableName);
            if (layer >= 0) volGo.layer = layer;
            var vol = volGo.AddComponent<ClimbableVolume>();
            vol.land = land.transform;

            // Respawn + player
            var respawn = new GameObject("Respawn") { tag = Layers.RespawnTag };
            respawn.transform.position = new Vector3(0f, 0.05f, 0f);
            var p = (GameObject)PrefabUtility.InstantiatePrefab(player);
            p.transform.position = new Vector3(0f, 0.05f, 0f);

            EditorSceneManager.SaveScene(scene, ProjectSetup.ScenePath("Sandbox_Gameplay"));
            ProjectSetup.RegisterScenes();
            Debug.Log("[PlantGame] Sandbox_Gameplay built. WASD/mouse/Space; 5 m gap ahead (+Z), 7 m gap to the right (+X), tower+ladder to the left (-X).");
        }

        static GameObject Box(string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = size;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            int ground = LayerMask.NameToLayer(Layers.GroundName);
            if (ground >= 0) go.layer = ground;
            return go;
        }

        static void Marker(string name, Vector3 pos, Material mat)
        {
            var m = Box(name, pos, new Vector3(0.3f, 1f, 0.3f), mat);
            Object.DestroyImmediate(m.GetComponent<Collider>());
        }
    }
}
