using System.IO;
using UnityEditor;
using UnityEngine;
using PlantGame.Core;
using PlantGame.Player;
using PlantGame.Plants;

namespace PlantGame.Editor
{
    /// <summary>
    /// Turns the baked Blender ladders (Art/Models/Plants/Plant_Ladder_NN.fbx) into Prefabs/Plants/Plant_Ladder_NN.prefab:
    /// M_BridgeGrow (part-id palette) on the mesh, COL_* become BoxColliders on layer Plant (enabled progressively),
    /// CLIMB_* become trigger boxes on layer Climbable with a ClimbableVolume (land = Land, climb line 0.5 m in front of the stalk),
    /// PlantInstance on the root. Menu: PlantGame > 8 Build Ladder Prefabs. Idempotent.
    /// </summary>
    public static class LadderPrefabBuilder
    {
        public static string PrefabPath(int n) => $"{BridgePrefabBuilder.PrefabDir}/Plant_Ladder_{n:00}.prefab";
        const float ClimbLineZ = 0.5f;   // (old: in front of the stalk) the line now sits this far BEHIND the plant origin, on the start-island side
        const float ClimbBoxBack = 1.2f, ClimbBoxFront = 1.25f;   // matches the Blender build: pad near edge is 0.9 m out

        [MenuItem("PlantGame/8 Build Ladder Prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory(BridgePrefabBuilder.PrefabDir);
            if (MaterialBuilder.Load("M_PlantGrow") == null) MaterialBuilder.Build();
            var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialBuilder.Dir + "/M_BridgeGrow.mat");
            if (mat == null) { BridgePrefabBuilder.Build(); mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialBuilder.Dir + "/M_BridgeGrow.mat"); }
            // T67: the beans on the grown beanstalk are purple like the bean seed (part id ~0.8 takes _FlowerColor); the bridge keeps its yellow flowers
            var lpath = MaterialBuilder.Dir + "/M_LadderGrow.mat";
            var lmat = AssetDatabase.LoadAssetAtPath<Material>(lpath);
            if (lmat == null) { lmat = new Material(mat) { name = "M_LadderGrow" }; AssetDatabase.CreateAsset(lmat, lpath); }
            lmat.CopyPropertiesFromMaterial(mat);
            lmat.SetColor("_FlowerColor", Palette.SeedBean);
            EditorUtility.SetDirty(lmat);
            mat = lmat;
            int plantLayer = LayerMask.NameToLayer(Layers.PlantName);
            int climbLayer = LayerMask.NameToLayer(Layers.ClimbableName);
            for (int n = 1; n <= 6; n++)
            {
                var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/Plants/Plant_Ladder_{n:00}.fbx");
                if (fbx == null) { Debug.LogWarning($"[PlantGame] missing Plant_Ladder_{n:00}.fbx (sync from Blender first)"); continue; }
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
                PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                inst.name = $"Plant_Ladder_{n:00}";
                foreach (Transform c in inst.transform) c.localPosition = Vector3.zero;   // Blender row offset (i * 4 m) must not leak into the prefab
                Transform land = null;
                foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Land") land = t;
                foreach (var t in inst.GetComponentsInChildren<Transform>(true))
                {
                    bool isCol = t.name.StartsWith("COL_"), isClimb = t.name.StartsWith("CLIMB_");
                    if (plantLayer >= 0) t.gameObject.layer = plantLayer;
                    if (isCol || isClimb)
                    {
                        var mf = t.GetComponent<MeshFilter>();
                        var box = t.gameObject.AddComponent<BoxCollider>();
                        if (mf != null && mf.sharedMesh != null) { box.center = mf.sharedMesh.bounds.center; box.size = mf.sharedMesh.bounds.size; }
                        Object.DestroyImmediate(t.GetComponent<MeshRenderer>());
                        Object.DestroyImmediate(mf);
                        if (isClimb)
                        {
                            box.isTrigger = true;
                            if (climbLayer >= 0) t.gameObject.layer = climbLayer;
                            var vol = t.gameObject.AddComponent<ClimbableVolume>();
                            vol.land = land;
                            // the player climbs behind the stalk (start-island side): the box spans ClimbBoxBack behind to ClimbBoxFront in front of the plant origin
                            float originZ = inst.transform.InverseTransformPoint(t.position).z;   // box transform's z in plant space
                            box.size = new Vector3(1.3f, box.size.y, ClimbBoxBack + ClimbBoxFront);
                            box.center = new Vector3(box.center.x, box.center.y, (ClimbBoxFront - ClimbBoxBack) * 0.5f - originZ);
                            vol.playerBehind = true;
                            float centreZ = originZ + box.center.z;
                            vol.standOff = centreZ + ClimbLineZ;   // normal points -Z: line z = centreZ - standOff = -ClimbLineZ
                        }
                    }
                    else if (t.TryGetComponent<MeshRenderer>(out var mr)) mr.sharedMaterial = mat;
                }
                var pi = inst.AddComponent<PlantInstance>();
                pi.growSeconds = 5f;
                PrefabUtility.SaveAsPrefabAsset(inst, PrefabPath(n));
                Object.DestroyImmediate(inst);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] Ladder prefabs built in " + BridgePrefabBuilder.PrefabDir);
        }
    }
}
