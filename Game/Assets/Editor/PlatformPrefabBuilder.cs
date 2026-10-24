using System.IO;
using UnityEditor;
using UnityEngine;
using PlantGame.Core;
using PlantGame.Plants;

namespace PlantGame.Editor
{
    /// <summary>
    /// Turns the baked Blender platform plants (Art/Models/Plants/Plant_Platform_NN.fbx) into Prefabs/Plants/Plant_Platform_NN.prefab:
    /// M_BridgeGrow on the mesh, one COL_* BoxCollider per pad on layer Plant (enabled base to tip as growth passes), PlantInstance 4 s.
    /// Menu: PlantGame > 10 Build Platform Prefabs. Idempotent.
    /// </summary>
    public static class PlatformPrefabBuilder
    {
        public static string PrefabPath(int n) => $"{BridgePrefabBuilder.PrefabDir}/Plant_Platform_{n:00}.prefab";

        [MenuItem("PlantGame/10 Build Platform Prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(BridgePrefabBuilder.PrefabDir);
            if (MaterialBuilder.Load("M_PlantGrow") == null) MaterialBuilder.Build();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialBuilder.Dir + "/M_BridgeGrow.mat");
            if (mat == null) { BridgePrefabBuilder.Build(); mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialBuilder.Dir + "/M_BridgeGrow.mat"); }
            int layer = LayerMask.NameToLayer(Layers.PlantName);
            for (int n = 1; n <= 6; n++)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/Plants/Plant_Platform_{n:00}.fbx");
                if (fbx == null) { Debug.LogWarning($"[PlantGame] missing Plant_Platform_{n:00}.fbx (sync from Blender first)"); continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                inst.name = $"Plant_Platform_{n:00}";
                foreach (Transform c in inst.transform) c.localPosition = Vector3.zero;   // Blender row offset must not leak into the prefab
                foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                {
                    if (layer >= 0) t.gameObject.layer = layer;
                    if (t.name.StartsWith("COL_"))
                    {
                        var mf = t.GetComponent<MeshFilter>();
                        var box = t.gameObject.AddComponent<BoxCollider>();
                        if (mf != null && mf.sharedMesh != null) { box.center = mf.sharedMesh.bounds.center; box.size = mf.sharedMesh.bounds.size; }
                        Object.DestroyImmediate(t.GetComponent<MeshRenderer>());
                        Object.DestroyImmediate(mf);
                    }
                    else if (t.TryGetComponent<MeshRenderer>(out var mr)) mr.sharedMaterial = mat;
                }
                inst.AddComponent<PlantInstance>().growSeconds = 4f;
                PrefabUtility.SaveAsPrefabAsset(inst, PrefabPath(n));
                Object.DestroyImmediate(inst);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] Platform prefabs built in " + BridgePrefabBuilder.PrefabDir);
        }
    }
}
