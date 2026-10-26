using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace PlantGame.Editor
{
    /// <summary>
    /// Imports the Quaternius Stylized Nature MegaKit (Standard, CC0; see docs/asset-sources.md and ThirdParty/LICENSES.md):
    /// trees, pines, bushes, ferns, flowers, rocks, mushrooms under Art/Models/Quaternius. The embedded textured materials are
    /// re-created as project URP Lit materials (Art/Materials/Quaternius); leaf / flower / fern cards get alpha clip + two-sided.
    /// Menu: PlantGame > 18 Build Quaternius Props, then PlantGame > 14 Build World Kit.
    /// </summary>
    public static class QuaterniusPropBuilder
    {
        public const string ModelDir = "Assets/Art/Models/Quaternius", MatDir = "Assets/Art/Materials/Quaternius";

        [MenuItem("PlantGame/20 Build Quaternius Props", false, 20)]
        public static void Build()
        {
            Directory.CreateDirectory(MatDir);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var paths = AssetDatabase.FindAssets("t:Model", new[] { ModelDir }).Select(AssetDatabase.GUIDToAssetPath).ToArray();

            foreach (var p in paths)   // pass 1: embedded materials so textures can be read
            {
                var imp = (ModelImporter)AssetImporter.GetAtPath(p);
                imp.globalScale = 1f; imp.useFileScale = true; imp.bakeAxisConversion = false; imp.isReadable = false;
                imp.meshCompression = ModelImporterMeshCompression.Off; imp.importCameras = false; imp.importLights = false;
                imp.importAnimation = false; imp.animationType = ModelImporterAnimationType.None; imp.addCollider = false;
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
                foreach (var r in imp.GetExternalObjectMap().Where(kv => kv.Key.type == typeof(Material)).Select(kv => kv.Key).ToArray()) imp.RemoveRemap(r);
                imp.SaveAndReimport();
            }

            int created = 0;
            foreach (var p in paths)   // pass 2: project materials, remap
            {
                var imp = (ModelImporter)AssetImporter.GetAtPath(p);
                foreach (var src in AssetDatabase.LoadAllAssetsAtPath(p).OfType<Material>().ToArray())
                {
                    string mp = $"{MatDir}/Q_{src.name}.mat";
                    var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                    if (m == null)
                    {
                        m = new Material(lit) { name = "Q_" + src.name };
                        var tex = src.HasProperty("_BaseMap") ? src.GetTexture("_BaseMap") : src.GetTexture("_MainTex");
                        if (tex != null) m.SetTexture("_BaseMap", tex);
                        m.SetColor("_BaseColor", Color.white);
                        m.SetFloat("_Smoothness", 0.1f); m.SetFloat("_Metallic", 0f);
                        bool card = src.name.StartsWith("Leaves") || src.name.StartsWith("Leaf") || src.name == "Flowers";
                        if (card)
                        {
                            m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON");
                            m.SetFloat("_Cull", (float)CullMode.Off); m.renderQueue = (int)RenderQueue.AlphaTest;
                        }
                        AssetDatabase.CreateAsset(m, mp); created++;
                    }
                    imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src.name), m);
                }
                imp.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[PlantGame] Quaternius props: {paths.Length} models, {created} new materials.");
        }
    }
}
