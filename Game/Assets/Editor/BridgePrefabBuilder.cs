using System.IO;
using UnityEditor;
using UnityEngine;
using PlantGame.Core;
using PlantGame.Plants;

namespace PlantGame.Editor
{
    /// <summary>
    /// Turns the baked Blender bridges (Art/Models/Plants/Plant_Bridge_NN.fbx) into Prefabs/Plants/Plant_Bridge_NN.prefab:
    /// M_BridgeGrow on the mesh (part-id palette), COL_* boxes become BoxColliders on layer Plant (disabled until grown),
    /// PlantInstance on the root. Menu: PlantGame > 7 Build Bridge Prefabs. Idempotent.
    /// </summary>
    public static class BridgePrefabBuilder
    {
        public const string PrefabDir = "Assets/Prefabs/Plants";
        public static string PrefabPath(int n) => $"{PrefabDir}/Plant_Bridge_{n:00}.prefab";

        [MenuItem("PlantGame/7 Build Bridge Prefabs", false, 7)]
        public static void Build()
        {
            Directory.CreateDirectory(PrefabDir);
            var mat = BridgeMaterial();
            int layer = LayerMask.NameToLayer(Layers.PlantName);
            for (int n = 1; n <= 6; n++)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/Plants/Plant_Bridge_{n:00}.fbx");
                if (fbx == null) { Debug.LogWarning("[PlantGame] missing Plant_Bridge_" + n.ToString("00") + ".fbx (sync from Blender first)"); continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                inst.name = $"Plant_Bridge_{n:00}";
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
                    else if (t.TryGetComponent<MeshRenderer>(out var mr)) { mr.sharedMaterial = mat; }
                }
                inst.AddComponent<PlantInstance>();
                PrefabUtility.SaveAsPrefabAsset(inst, PrefabPath(n));
                Object.DestroyImmediate(inst);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] Bridge prefabs built in " + PrefabDir);
        }

        static Material BridgeMaterial()
        {
            if (MaterialBuilder.Load("M_PlantGrow") == null) MaterialBuilder.Build();
            var path = MaterialBuilder.Dir + "/M_BridgeGrow.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(MaterialBuilder.Load("M_PlantGrow")) { name = "M_BridgeGrow" }; AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_CapColor", Palette.MushroomRed);
            m.SetColor("_FlowerColor", Palette.FlowerYellow);
            m.SetFloat("_PartColors", 1f);
            m.EnableKeyword("_PARTCOLORS_ON");
            m.SetFloat("_Growth", 1f);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
