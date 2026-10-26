using System.IO;
using UnityEditor;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Editor
{
    /// <summary>T30: ripple texture and the three water materials (light, mid, dark) used by ponds, streams and the collector reservoir.</summary>
    public static class WaterMaterialBuilder
    {
        public static Material Light => Load("M_Water"); public static Material Mid => Load("M_WaterMid"); public static Material Dark => Load("M_WaterDeep");
        static Material Load(string n) { var m = MaterialBuilder.Load(n); if (m == null || m.shader == null || m.shader.name != "PlantGame/OvergrowthWater") { Build(); m = MaterialBuilder.Load(n); } return m; }

        [MenuItem("PlantGame/13 Build Water Materials", false, 13)]
        public static void Build()
        {
            var tex = EnsureRipple();
            var sh = Shader.Find("PlantGame/OvergrowthWater");
            if (sh == null) { Debug.LogError("[PlantGame] Shader PlantGame/OvergrowthWater not found"); return; }
            Make("M_Water", sh, tex, Color.Lerp(Palette.MistBlue, Palette.GlowCyan, 0.3f), Color.Lerp(Palette.SkyBlue, Palette.DeepBlue, 0.45f), 0.2f);
            Make("M_WaterMid", sh, tex, Color.Lerp(Palette.SkyBlue, Palette.DeepBlue, 0.25f), Palette.DeepBlue, 0.45f);
            Make("M_WaterDeep", sh, tex, Palette.SkyBlue, Color.Lerp(Palette.DeepBlue, Color.black, 0.6f), 0.7f);
            AssetDatabase.SaveAssets();
        }

        static void Make(string name, Shader sh, Texture2D tex, Color shallow, Color deep, float depth)
        {
            string path = MaterialBuilder.Dir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); } else m.shader = sh;
            m.SetTexture("_Ripple", tex); m.SetColor("_ShallowColor", shallow); m.SetColor("_DeepColor", deep);
            m.SetColor("_RimColor", Palette.GlowCyan); m.SetFloat("_Depth", depth);
            EditorUtility.SetDirty(m);
        }

        /// <summary>Tileable soft ripple noise (alpha): value noise with a cellular ridge so crests look like little waves.</summary>
        static Texture2D EnsureRipple()
        {
            const string path = "Assets/Art/Textures/T_WaterRipple.png";
            if (!File.Exists(path))
            {
                const int S = 256; var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
                var rnd = new System.Random(311);
                int[] res = { 6, 12, 24 }; var grids = new float[3][];
                for (int o = 0; o < 3; o++) { grids[o] = new float[res[o] * res[o]]; for (int i = 0; i < grids[o].Length; i++) grids[o][i] = (float)rnd.NextDouble(); }
                float Sample(int o, float u, float v)
                {
                    int n = res[o]; float x = u * n, y = v * n; int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
                    float fx = x - x0, fy = y - y0; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
                    float a = grids[o][(y0 % n) * n + x0 % n], b = grids[o][(y0 % n) * n + (x0 + 1) % n], c = grids[o][((y0 + 1) % n) * n + x0 % n], d = grids[o][((y0 + 1) % n) * n + (x0 + 1) % n];
                    return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
                }
                for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float n = Sample(0, u, v) * 0.5f + Sample(1, u, v) * 0.32f + Sample(2, u, v) * 0.18f;
                    float ridge = 1f - Mathf.Abs(n * 2f - 1f);                                      // crest lines where the noise crosses 0.5
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(Mathf.Pow(ridge, 4f))));
                }
                tex.Apply(); File.WriteAllBytes(path, tex.EncodeToPNG()); Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp != null && (imp.wrapMode != TextureWrapMode.Repeat || imp.sRGBTexture))
            { imp.wrapMode = TextureWrapMode.Repeat; imp.sRGBTexture = false; imp.alphaIsTransparency = false; imp.mipmapEnabled = true; imp.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
