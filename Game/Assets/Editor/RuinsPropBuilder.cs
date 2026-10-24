using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PlantGame.Editor
{
    /// <summary>
    /// Imports FreeStylized "Ruins Modular Kit 01" (royalty free, see docs/asset-sources.md and ThirdParty/LICENSES.md): 21 stone pieces
    /// under Art/Models/Ruins/A1..A3, one baked texture atlas per folder (BaseColor + GL normal). Builds one URP Lit material per atlas
    /// (Art/Materials/Ruins) and maps every piece of a folder to it. Menu: PlantGame > 19 Build Ruins Props, then 14 Build World Kit.
    /// </summary>
    public static class RuinsPropBuilder
    {
        public const string ModelDir = "Assets/Art/Models/Ruins", MatDir = "Assets/Art/Materials/Ruins";

        [MenuItem("PlantGame/19 Build Ruins Props")]
        public static void Build()
        {
            Directory.CreateDirectory(MatDir);
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            for (int n = 1; n <= 3; n++)
            {
                var normalPath = $"{ModelDir}/Textures/AP4_Atlas_{n}_Normal_GL.png";
                var ti = AssetImporter.GetAtPath(normalPath) as TextureImporter;
                if (ti != null && ti.textureType != TextureImporterType.NormalMap) { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
                var colorPath = $"{ModelDir}/Textures/AP4_Atlas_{n}_BaseColor.png";
                var ci = AssetImporter.GetAtPath(colorPath) as TextureImporter;
                if (ci != null && ci.maxTextureSize > 1024) { ci.maxTextureSize = 1024; ci.SaveAndReimport(); }

                string mp = $"{MatDir}/R_Atlas{n}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(mp);
                if (m == null) { m = new Material(lit) { name = $"R_Atlas{n}" }; AssetDatabase.CreateAsset(m, mp); }
                m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath));
                m.SetColor("_BaseColor", Color.white);
                var nm = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                if (nm != null) { m.SetTexture("_BumpMap", nm); m.SetFloat("_BumpScale", 0.8f); m.EnableKeyword("_NORMALMAP"); }
                m.SetFloat("_Smoothness", 0.15f); m.SetFloat("_Metallic", 0f);
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();

            var paths = AssetDatabase.FindAssets("t:Model", new[] { ModelDir }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
            foreach (var p in paths)
            {
                var imp = (ModelImporter)AssetImporter.GetAtPath(p);
                imp.globalScale = 1f; imp.useFileScale = true; imp.bakeAxisConversion = false; imp.isReadable = false;
                imp.meshCompression = ModelImporterMeshCompression.Off; imp.importCameras = false; imp.importLights = false;
                imp.importAnimation = false; imp.animationType = ModelImporterAnimationType.None; imp.addCollider = false;
                imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; imp.materialLocation = ModelImporterMaterialLocation.InPrefab;
                imp.SaveAndReimport();
            }
            foreach (var p in paths)
            {
                int atlas = p.Contains("/A2/") ? 2 : p.Contains("/A3/") ? 3 : 1;
                var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/R_Atlas{atlas}.mat");
                var imp = (ModelImporter)AssetImporter.GetAtPath(p);
                foreach (var src in AssetDatabase.LoadAllAssetsAtPath(p).OfType<Material>().ToArray())
                    imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), src.name), mat);
                imp.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[PlantGame] Ruins props: {paths.Length} models.");
        }
    }
}
