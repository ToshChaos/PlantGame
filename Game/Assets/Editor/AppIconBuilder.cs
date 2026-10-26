using System.IO;
using UnityEditor;
using UnityEngine;
using PlantGame.UI;

namespace PlantGame.Editor
{
    /// <summary>PlantGame > Build App Icon: renders the title's vine "O" onto a dark rounded tile, saves Art/Textures/T_AppIcon.png and sets it as the default player icon.</summary>
    public static class AppIconBuilder
    {
        const string Path_ = "Assets/Art/Textures/T_AppIcon.png";

        [MenuItem("PlantGame/Build App Icon", false, 103)]
        public static void Build()
        {
            const int S = 1024;
            var o = TitleMenu.VineO().texture;
            var px = new Color[S * S];
            var top = new Color(0.10f, 0.25f, 0.38f); var bot = new Color(0.04f, 0.10f, 0.17f);
            const float margin = 0.04f, radius = 0.22f, oScale = 0.82f;
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S, v = (y + 0.5f) / S;
                float dx = Mathf.Max(Mathf.Abs(u - 0.5f) - (0.5f - margin - radius), 0), dy = Mathf.Max(Mathf.Abs(v - 0.5f) - (0.5f - margin - radius), 0);
                float a = Mathf.Clamp01((radius - Mathf.Sqrt(dx * dx + dy * dy)) * S + 0.5f);
                var c = Color.Lerp(bot, top, v);
                var oc = o.GetPixelBilinear((u - 0.5f) / oScale + 0.5f, (v - 0.5f) / oScale + 0.5f);
                if ((u - 0.5f) / oScale + 0.5f is < 0f or > 1f || (v - 0.5f) / oScale + 0.5f is < 0f or > 1f) oc.a = 0f;
                c = Color.Lerp(c, new Color(oc.r, oc.g, oc.b), oc.a);
                px[y * S + x] = new Color(c.r, c.g, c.b, a);
            }
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.SetPixels(px); tex.Apply();
            File.WriteAllBytes(Path_, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(Path_);
            var imp = (TextureImporter)AssetImporter.GetAtPath(Path_);
            imp.textureType = TextureImporterType.Default; imp.alphaIsTransparency = true; imp.mipmapEnabled = false; imp.npotScale = TextureImporterNPOTScale.None;
            imp.SaveAndReimport();
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(Path_);
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] App icon built: " + Path_);
        }
    }
}
