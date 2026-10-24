using System.IO;
using UnityEditor;
using UnityEngine;

namespace PlantGame.Editor
{
    /// <summary>
    /// Procedural, tileable detail textures (no sourced assets): grass-top and rock for the islands, a ridged seed coat.
    /// Albedo textures are luminance/hue variation that multiplies the palette colour of the material; each has a matching
    /// normal map made from the same height field. Written to Assets/Art/Textures (committed). Menu: PlantGame > 11 Build Textures.
    /// </summary>
    public static class TextureBuilder
    {
        public const string Dir = "Assets/Art/Textures";

        [MenuItem("PlantGame/11 Build Textures")]
        public static void Build()
        {
            Directory.CreateDirectory(Dir);
            const int N = 256;
            // grass top: dark undercoat with rows of leaning, tapering blades (lighter tips), broad patches of colour and a few bright specks
            Make("T_Terrain", 512, (u, v, c, h) =>
            {
                float x = u * 160f, y = v * 28f;
                int col = Mathf.FloorToInt(x);
                float yy = y + Hash(col, 0, 61), fy = yy - Mathf.Floor(yy);
                int row = Mod(Mathf.FloorToInt(yy), 28);
                float tone = Hash(col, row, 62), lean = (Hash(col, row, 63) - 0.5f) * 1.3f;
                float half = 0.5f * (1f - fy * 0.9f), dx = x - col - (0.5f + lean * fy);
                float cover = Mathf.Clamp01((half - Mathf.Abs(dx)) * 7f);
                float patch = Fbm(u, v, 4, 3, 64), dry = Fbm(u, v, 3, 2, 65);
                float under = 0.30f + 0.12f * patch;
                float blade = (0.50f + 0.45f * fy) * (0.78f + 0.4f * tone);
                float lum = Mathf.Lerp(under, blade, cover) * (0.82f + 0.4f * patch);
                if (Hash(Mathf.FloorToInt(u * 512), Mathf.FloorToInt(v * 512), 66) > 0.994f) lum += 0.25f;   // dew / seed heads
                lum = Mathf.Clamp01(lum);
                h = cover * (0.3f + 0.7f * fy) * 0.8f + patch * 0.2f;
                c = new Color(lum * (0.92f + 0.2f * dry), lum, lum * (0.84f + 0.05f * dry), 1f);   // yellower where it is dry
                return (c, h);
            });
            // mud: wet dark patches, dry cracked plates, straw and pebbles
            Make("T_Mud", 512, (u, v, c, h) =>
            {
                Worley(u, v, 7, 71, out float f1, out float f2);
                float wet = Mathf.Clamp01((Fbm(u, v, 4, 4, 72) - 0.5f) * 3.5f);
                float crack = (1f - Mathf.Clamp01((f2 - f1) * 9f)) * (1f - wet);
                float clod = Fbm(u, v, 24, 3, 73), speck = Hash(Mathf.FloorToInt(u * 512), Mathf.FloorToInt(v * 512), 74);
                float lum = (0.70f + 0.28f * clod) * Mathf.Lerp(1f, 0.62f, wet) * (1f - 0.3f * crack);
                if (speck > 0.985f) lum += 0.22f;
                h = (1f - crack) * 0.5f + clod * 0.35f - wet * 0.15f + (speck > 0.985f ? 0.2f : 0f);
                lum = Mathf.Clamp01(lum);
                c = new Color(lum, lum * 0.95f, lum * 0.88f, 1f);
                return (c, h);
            });
            // flagstones and gravel: rounded plates, each its own shade, dark joints, moss patches
            Make("T_Pebbles", 512, (u, v, c, h) =>
            {
                WorleyCell(u, v, 6, 81, out float f1, out float f2, out float cell);
                float joint = Mathf.Clamp01((f2 - f1) * 7f);
                float grain = Fbm(u, v, 40, 2, 82), moss = Mathf.Clamp01((Fbm(u, v, 5, 3, 83) - 0.55f) * 3f);
                float lum = (0.6f + 0.25f * cell) * Mathf.Lerp(0.6f, 1f, joint) * (0.85f + 0.3f * grain);
                h = joint * (1f - f1 * 0.6f) * 0.7f + grain * 0.25f;
                lum = Mathf.Clamp01(lum);
                c = new Color(lum * (1f - 0.18f * moss), lum, lum * (1f - 0.3f * moss), 1f);
                return (c, h);
            });
            // rock: cracks from cell edges + mottling
            Make("T_Rock", N, (u, v, c, h) =>
            {
                Worley(u, v, 6, 21, out float f1, out float f2);
                float crack = Mathf.Clamp01((f2 - f1) * 16f);
                float m = Fbm(u, v, 8, 4, 22);
                h = crack * 0.6f + m * 0.25f + f1 * 0.15f;
                float lum = Mathf.Clamp01((0.62f + 0.4f * m) * Mathf.Lerp(0.6f, 1f, crack));
                c = new Color(lum * 0.96f, lum, lum * 1.04f, 1f);
                return (c, h);
            });
            // bark: vertical streaks (noise stretched along v), knotty dark fissures
            Make("T_Bark", N, (u, v, c, h) =>
            {
                float streak = Fbm(u, v, 12, 3, 41);                      // fine in u ...
                float along = ValueNoise(u * 12f, v * 3f, 12, 42);         // ... slow in v: long grain
                float fis = Mathf.Clamp01((0.5f - Mathf.Abs(ValueNoise(u * 9f, v * 2f, 9, 43) - 0.5f)) * 5f);
                h = streak * 0.4f + along * 0.3f + (1f - fis) * 0.3f;
                float lum = Mathf.Clamp01((0.72f + 0.35f * along) * Mathf.Lerp(0.55f, 1f, 1f - fis * 0.0f - (fis > 0.8f ? 0.45f : 0f)));
                c = new Color(lum, lum * 0.97f, lum * 0.92f, 1f);
                return (c, h);
            });
            // furrowed earth: ridges along v (u repeats 6x), dark furrow bottoms, clods and pebbles
            Make("T_Soil", N, (u, v, c, h) =>
            {
                float wob = Fbm(u, v, 4, 3, 51);
                float ridge = 0.5f + 0.5f * Mathf.Cos((u * 6f + wob * 0.25f) * Mathf.PI * 2f);          // 1 on the ridge crest, 0 in the furrow
                float clod = Fbm(u, v, 16, 3, 52), speck = Hash(Mathf.FloorToInt(u * N), Mathf.FloorToInt(v * N), 53);
                h = ridge * 0.6f + clod * 0.3f + (speck > 0.97f ? 0.25f : 0f);
                float lum = Mathf.Clamp01((0.55f + 0.5f * ridge) * (0.82f + 0.3f * clod) - (speck > 0.97f ? 0.0f : 0f));
                c = new Color(lum, lum * 0.96f, lum * 0.9f, 1f);
                return (c, h);
            });
            // seed coat: longitudinal ridges (u wraps round the sphere) + speckle
            Make("T_Seed", 128, (u, v, c, h) =>
            {
                float wob = Fbm(u, v, 4, 3, 31);
                float s = 0.5f + 0.5f * Mathf.Sin((u * 8f + wob * 0.6f) * Mathf.PI * 2f);
                float speck = Hash(Mathf.FloorToInt(u * 128), Mathf.FloorToInt(v * 128), 32);
                h = s * 0.7f + wob * 0.2f;
                float lum = Mathf.Clamp01(0.7f + 0.3f * s - (speck > 0.9f ? 0.22f : 0f));
                c = new Color(lum, lum, lum, 1f);
                return (c, h);
            });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlantGame] Textures built in " + Dir);
        }

        public static Texture2D Albedo(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/{name}.png");
        public static Texture2D Normal(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{Dir}/{name}_N.png");

        delegate (Color, float) Gen(float u, float v, Color c, float h);

        static void Make(string name, int n, Gen gen)
        {
            var albedo = new Color[n * n];
            var height = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    var (c, h) = gen((x + 0.5f) / n, (y + 0.5f) / n, Color.white, 0f);
                    albedo[y * n + x] = c; height[y * n + x] = h;
                }
            var normal = new Color[n * n];
            const float strength = 3.0f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float hl = height[y * n + (x + n - 1) % n], hr = height[y * n + (x + 1) % n];
                    float hd = height[((y + n - 1) % n) * n + x], hu = height[((y + 1) % n) * n + x];
                    var nv = new Vector3((hl - hr) * strength, (hd - hu) * strength, 1f).normalized;
                    normal[y * n + x] = new Color(nv.x * 0.5f + 0.5f, nv.y * 0.5f + 0.5f, nv.z * 0.5f + 0.5f, 1f);
                }
            Write(name, n, albedo, false);
            Write(name + "_N", n, normal, true);
        }

        static void Write(string name, int n, Color[] px, bool isNormal)
        {
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false, true);
            tex.SetPixels(px);
            var path = $"{Dir}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.sRGBTexture = !isNormal;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.filterMode = FilterMode.Bilinear;
            imp.mipmapEnabled = true;
            imp.anisoLevel = 4;
            imp.SaveAndReimport();
        }

        // ---------------------------------------------------------------- tileable noise (u, v in 0..1)
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393) + (uint)(y * 668265263) + (uint)(seed * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177u;
                return ((h ^ (h >> 16)) & 0xFFFFFF) / (float)0x1000000;
            }
        }

        static int Mod(int a, int m) => ((a % m) + m) % m;

        static float ValueNoise(float x, float y, int period, int seed)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a = Hash(Mod(xi, period), Mod(yi, period), seed), b = Hash(Mod(xi + 1, period), Mod(yi, period), seed);
            float c = Hash(Mod(xi, period), Mod(yi + 1, period), seed), d = Hash(Mod(xi + 1, period), Mod(yi + 1, period), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Fbm(float u, float v, int baseFreq, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, norm = 0f; int f = baseFreq;
            for (int o = 0; o < octaves; o++)
            {
                sum += amp * ValueNoise(u * f, v * f, f, seed + o * 7);
                norm += amp; amp *= 0.5f; f *= 2;
            }
            return sum / norm;
        }

        static void WorleyCell(float u, float v, int cells, int seed, out float f1, out float f2, out float cellValue)
        {
            float x = u * cells, y = v * cells;
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            f1 = f2 = 9f; cellValue = 0f;
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    int cx = xi + i, cy = yi + j;
                    float px = cx + Hash(Mod(cx, cells), Mod(cy, cells), seed), py = cy + Hash(Mod(cx, cells), Mod(cy, cells), seed + 5);
                    float d = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                    if (d < f1) { f2 = f1; f1 = d; cellValue = Hash(Mod(cx, cells), Mod(cy, cells), seed + 9); } else if (d < f2) f2 = d;
                }
        }

        static void Worley(float u, float v, int cells, int seed, out float f1, out float f2)
        {
            float x = u * cells, y = v * cells;
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            f1 = f2 = 9f;
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    int cx = xi + i, cy = yi + j;
                    float px = cx + Hash(Mod(cx, cells), Mod(cy, cells), seed), py = cy + Hash(Mod(cx, cells), Mod(cy, cells), seed + 5);
                    float d = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                    if (d < f1) { f2 = f1; f1 = d; } else if (d < f2) f2 = d;
                }
        }
    }
}
