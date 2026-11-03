using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlantGame.Core;
using PlantGame.Inventory;
using PlantGame.Player;
using PlantGame.World;

namespace PlantGame.Editor
{
    /// <summary>
    /// T03: builds Sandbox with the tuning course from the task:
    /// start floor, 5 m gap (jumpable, +Z), 7 m gap (not jumpable, +X), 1.6 m ledge (-Z), stairs, a 30 degree ramp
    /// and a 6 m tower with a ladder (W/S climb, Space jumps off). Menu: PlantGame > 5 Build Sandbox.
    /// </summary>
    public static class SandboxGameplayBuilder
    {
        [MenuItem("PlantGame/5 Build Sandbox", false, 5)]
        public static void Build()
        {
            if (MaterialBuilder.Load("M_Terrain") == null) MaterialBuilder.Build();
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabBuilder.PrefabPath) ?? PlayerPrefabBuilder.Build();

            var before = BuildFingerprint.OfSavedScene(ProjectSetup.ScenePath("Sandbox"));   // unchanged rebuilds are not re-saved (new random file IDs)
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            ProjectSetup.ApplyLightingPreset();
            ProjectSetup.AddSun();

            var terrain = MaterialBuilder.Load("M_Terrain");
            var stone = MaterialBuilder.Load("M_Stone");
            var foliage = MaterialBuilder.Load("M_Foliage");
            var crystal = MaterialBuilder.Load("M_Crystal");

            // Floors (top surface at y = 0)
            TexturedBox.Create("Floor_Start", new Vector3(-7.5f, -0.5f, -3f), new Vector3(25f, 1f, 16f), terrain, stone);      // x -20..5, z -11..5

            // 1.6 m ledge (jump height) at -Z
            Box("Ledge_1.6m", new Vector3(0f, 0.8f, -8f), new Vector3(6f, 1.6f, 2f), stone);

            // Stairs: 4 steps, 0.3 m rise each (step offset 0.35)
            for (int i = 0; i < 4; i++)
            {
                float h = 0.3f * (i + 1);
                Box("Stair_" + i, new Vector3(-3f, h * 0.5f, 1.7f + i * 0.6f), new Vector3(2f, h, 0.6f), stone);
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

            // test props: one of every item on the start floor (more than the 4 slots hold: the extra pickups are refused), a plant target and a water sign
            var seedAssets = new[] { "Item_VineSeed", "Item_BeanstalkSeed", "Item_ShelfSeed", "Item_DewSeed", "Item_HeartSeed", "Item_HeatSeed", "Item_ParachuteLeaf", "Item_Coconut" };
            if (ItemBuilder.Load(seedAssets[0]) == null) ItemBuilder.Build();
            for (int i = 0; i < seedAssets.Length; i++)
            {
                var it = ItemBuilder.Load(seedAssets[i]);
                if (it == null || it.worldPrefab == null) continue;
                var seed = (GameObject)PrefabUtility.InstantiatePrefab(it.worldPrefab);
                seed.transform.position = new Vector3(-5f + i * 1.2f, 0.4f, -4f);
            }
            // Soil patch on the edge of the first island, facing the 7 m gap (+X): E plants, the bridge grows across to the far floor
            var target = Box("Sandbox_PlantTarget", new Vector3(3.9f, 0.15f, -2.2f), new Vector3(1.2f, 0.3f, 1.2f), terrain);
            SoilPatchMesh.Apply(target);
            var st = target.AddComponent<SandboxTarget>();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BridgePrefabBuilder.PrefabPath(1)) == null) BridgePrefabBuilder.Build();
            var bridges = new System.Collections.Generic.List<GameObject>();
            for (int b = 1; b <= 6; b++) { var bp = AssetDatabase.LoadAssetAtPath<GameObject>(BridgePrefabBuilder.PrefabPath(b)); if (bp != null) bridges.Add(bp); }
            st.plantPrefabs = bridges.ToArray();
            st.growDirection = Vector3.right;
            int interactable = LayerMask.NameToLayer(Layers.InteractableName);
            if (interactable >= 0) target.layer = interactable;

            // Water puddle for testing the refill (F): flat disc on the start floor, layer Water, trigger
            var puddle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            puddle.name = "Water_Puddle";
            puddle.transform.position = new Vector3(-4f, 0.03f, -9f);
            puddle.transform.localScale = new Vector3(3f, 0.03f, 3f);
            var wmat = MaterialBuilder.Load("M_Water");
            if (wmat == null)
            {
                wmat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_Water" };
                wmat.SetColor("_BaseColor", Palette.SkyBlue); wmat.SetFloat("_Smoothness", 0.95f);
                MaterialBuilder.SetEmission(wmat, Palette.SkyBlue * 0.35f);
                MaterialBuilder.ValidateUrp(wmat);
                AssetDatabase.CreateAsset(wmat, MaterialBuilder.Dir + "/M_Water.mat");
            }
            puddle.GetComponent<Renderer>().sharedMaterial = wmat;
            Object.DestroyImmediate(puddle.GetComponent<Collider>());
            var pc = puddle.AddComponent<CapsuleCollider>(); pc.isTrigger = true; pc.radius = 0.5f; pc.height = 40f;   // tall trigger: reachable standing next to it
            int water = LayerMask.NameToLayer(Layers.WaterName);
            if (water >= 0) puddle.layer = water;
            puddle.AddComponent<PlantGame.Plants.WaterSource>().kind = PlantGame.Plants.WaterKind.Lake;

            // Respawn + player
            var respawn = new GameObject("Respawn") { tag = Layers.RespawnTag };
            respawn.transform.position = new Vector3(0f, 0.05f, 0f);
            var p = (GameObject)PrefabUtility.InstantiatePrefab(player);
            p.transform.position = new Vector3(0f, 0.05f, 0f);

            BuildFingerprint.SaveIfChanged(scene, ProjectSetup.ScenePath("Sandbox"), before);
            ProjectSetup.RegisterScenes();
            Debug.Log("[PlantGame] Sandbox built. WASD/mouse/Space; tower+ladder to the left (-X).");
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
    }
}
