using System.IO;
using UnityEditor;
using UnityEngine;
using PlantGame.Core;
using PlantGame.Plants;

namespace PlantGame.Editor
{
    /// <summary>
    /// Plant_Collector_NN.fbx -> Prefabs/Plants/Plant_Collector_NN.prefab: M_CollectorGrow (bowl mist blue, petals white, dew dots cyan),
    /// Reservoir with the water material, INTERACT_NN as a trigger on layer Interactable, COL_NN_0 body collider on layer Plant,
    /// PlantInstance (4 s, kind Collector) + WaterCollector. Menu: PlantGame > 14 Build Collector Prefabs.
    /// </summary>
    public static class CollectorPrefabBuilder
    {
        public static string PrefabPath(int n) => $"{BridgePrefabBuilder.PrefabDir}/Plant_Collector_{n:00}.prefab";

        [MenuItem("PlantGame/14 Build Collector Prefabs", false, 14)]
        public static void Build()
        {
            Directory.CreateDirectory(BridgePrefabBuilder.PrefabDir);
            if (MaterialBuilder.Load("M_PlantGrow") == null) MaterialBuilder.Build();
            var water = WaterMaterialBuilder.Mid;
            string mpath = MaterialBuilder.Dir + "/M_CollectorGrow.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(mpath);
            if (mat == null) { mat = new Material(MaterialBuilder.Load("M_PlantGrow")) { name = "M_CollectorGrow" }; AssetDatabase.CreateAsset(mat, mpath); }
            mat.SetColor("_CapColor", Palette.SeedSucculent);   // T67: the rosette leaves and basin are the same teal as the dew-seed source plant
            mat.SetColor("_BaseColor", Palette.LeafGreen); mat.SetColor("_TipColor", Color.Lerp(Palette.LeafGreen, Palette.CloudWhite, 0.2f)); mat.SetColor("_ShadeColor", Palette.Moss); mat.SetColor("_RimColor", Palette.GlowCyan); mat.SetFloat("_RimStrength", 0.25f);
            mat.SetColor("_FlowerColor", Palette.GlowCyan);                                       // dew dots
            mat.SetFloat("_PartColors", 1f); mat.EnableKeyword("_PARTCOLORS_ON"); mat.SetFloat("_Growth", 1f);
            EditorUtility.SetDirty(mat);

            int plantLayer = LayerMask.NameToLayer(Layers.PlantName), interact = LayerMask.NameToLayer(Layers.InteractableName);
            for (int n = 1; n <= 6; n++)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/Plants/Plant_Collector_{n:00}.fbx");
                if (fbx == null) { Debug.LogWarning($"[PlantGame] missing Plant_Collector_{n:00}.fbx (sync from Blender first)"); continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                inst.name = $"Plant_Collector_{n:00}";
                foreach (Transform c in inst.transform) if (c.name.StartsWith("Plant_Collector")) c.localPosition = Vector3.zero;   // Blender row offset must not leak
                Transform reservoir = null;
                foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "Reservoir")
                    {
                        reservoir = t; t.gameObject.layer = 0;
                        if (t.TryGetComponent<MeshRenderer>(out var rr)) { rr.sharedMaterial = water; rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; }
                        continue;
                    }
                    if (plantLayer >= 0 && !t.name.StartsWith("INTERACT_")) t.gameObject.layer = plantLayer;
                    if (t.name.StartsWith("COL_") || t.name.StartsWith("INTERACT_"))
                    {
                        var mf = t.GetComponent<MeshFilter>();
                        var box = t.gameObject.AddComponent<BoxCollider>();
                        if (mf != null && mf.sharedMesh != null) { box.center = mf.sharedMesh.bounds.center; box.size = mf.sharedMesh.bounds.size; }
                        if (t.name.StartsWith("INTERACT_")) { box.isTrigger = true; if (interact >= 0) t.gameObject.layer = interact; }
                        Object.DestroyImmediate(t.GetComponent<MeshRenderer>());
                        Object.DestroyImmediate(mf);
                    }
                    else if (t.TryGetComponent<MeshRenderer>(out var mr)) mr.sharedMaterial = mat;
                }
                var pi = inst.AddComponent<PlantInstance>(); pi.growSeconds = 4f; pi.kind = PlantKind.Collector;
                var wc = inst.AddComponent<WaterCollector>(); wc.reservoir = reservoir;
                PrefabUtility.SaveAsPrefabAsset(inst, PrefabPath(n));
                Object.DestroyImmediate(inst);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] Collector prefabs built in " + BridgePrefabBuilder.PrefabDir);
        }
    }
}
