using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PlantGame.Editor
{
    /// <summary>
    /// Imports the Kenney CC0 props (Nature Kit, Fantasy Town Kit; see docs/asset-sources.md and ThirdParty/LICENSES.md):
    /// the FBX files under Art/Models/Kenney keep their embedded materials, which are re-mapped to URP Lit materials
    /// (Nature = one flat colour per material, Town = the shared colour atlas). Menu: PlantGame > 17 Build Kenney Props.
    /// Then rerun PlantGame > 14 Build World Kit so WorldKit.props lists the models.
    /// </summary>
    public static class KenneyPropBuilder
    {
        public const string ModelDir = "Assets/Art/Models/Kenney", MatDir = "Assets/Art/Materials/Kenney";

        // art-bible green instead of Kenney's teal (docs/art-bible.md: leaf green #5DB46B)
        static readonly System.Collections.Generic.Dictionary<string, Color> Tint = new System.Collections.Generic.Dictionary<string, Color>
        {
            { "K_leafsGreen", new Color32(0x5D, 0xB4, 0x6B, 255) }, { "K_grass", new Color32(0x5D, 0xB4, 0x6B, 255) },
            { "K_woodBark", new Color32(0x8A, 0x5E, 0x3E, 255) },
        };

        [MenuItem("PlantGame/17 Build Kenney Props")]
        public static void Build()
        {
            Directory.CreateDirectory(MatDir);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var paths = AssetDatabase.FindAssets("t:Model", new[] { ModelDir }).Select(AssetDatabase.GUIDToAssetPath).ToArray();

            // pass 1: import the embedded materials so their colours can be read
            foreach (var p in paths)
            {
                var imp = (ModelImporter)AssetImporter.GetAtPath(p);
                Configure(imp);
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
                imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
                imp.SaveAndReimport();
            }

            // pass 2: one URP material per distinct source material, remap every model to it
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(ModelDir + "/Town/Textures/variation-a.png");
            int mats = 0;
            foreach (var p in paths)
            {
                var imp = (ModelImporter)AssetImporter.GetAtPath(p);
                foreach (var src in AssetDatabase.LoadAllAssetsAtPath(p).OfType<Material>().ToArray())
                {
                    bool town = p.Contains("/Town/");
                    string name = town ? "K_TownAtlas" : "K_" + src.name;
                    string mp = $"{MatDir}/{name}.mat";
                    var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                    if (m == null)
                    {
                        m = new Material(lit) { name = name };
                        if (town && atlas != null) { m.SetTexture("_BaseMap", atlas); m.SetColor("_BaseColor", Color.white); }
                        else m.SetColor("_BaseColor", src.HasProperty("_Color") ? src.color : Color.white);
                        m.SetFloat("_Smoothness", 0.12f); m.SetFloat("_Metallic", 0f);
                        AssetDatabase.CreateAsset(m, mp); mats++;
                    }
                    imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src.name), m);
                }
                imp.SaveAndReimport();
            }
            foreach (var kv in Tint)   // separate pass: models already remapped no longer expose their embedded materials
            {
                var tm = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{kv.Key}.mat");
                if (tm != null) { tm.SetColor("_BaseColor", kv.Value); EditorUtility.SetDirty(tm); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[PlantGame] Kenney props: {paths.Length} models, {mats} new materials.");
        }

        static void Configure(ModelImporter m)
        {
            m.globalScale = 1f; m.useFileScale = true; m.bakeAxisConversion = false;
            m.meshCompression = ModelImporterMeshCompression.Off; m.isReadable = false;
            m.importBlendShapes = false; m.importCameras = false; m.importLights = false; m.importAnimation = false;
            m.animationType = ModelImporterAnimationType.None; m.addCollider = false;
        }
    }
}
