using UnityEngine;

namespace PlantGame.World
{
    /// <summary>
    /// Textures and materials for the palms OrganicProps.Palm builds: a procedural ringed bark and a leaflet-striped frond texture (the palm mesh
    /// brings the UVs: ring bands up the trunk, leaflet stripes along the blade).
    /// </summary>
    public static class PalmLook
    {
        static Texture2D _bark, _frond;
        static Material _barkMat, _frondMat;

        static float Hash(int x, int y, int period)
        {
            x = ((x % period) + period) % period; y = ((y % period) + period) % period;
            int h = x * 374761393 + y * 668265263; h = (h ^ (h >> 13)) * 1274126177; return ((h ^ (h >> 16)) & 0xffff) / 65535f;
        }

        // tileable value noise, `cells` lattice cells per texture
        static float Noise(float u, float v, int cells)
        {
            float x = u * cells, y = v * cells; int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y); float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a = Hash(xi, yi, cells), b = Hash(xi + 1, yi, cells), c = Hash(xi, yi + 1, cells), d = Hash(xi + 1, yi + 1, cells);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Sstep(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        static Texture2D BarkTexture()
        {
            if (_bark != null) return _bark;
            const int N = 128;
            var t = new Texture2D(N, N, TextureFormat.RGB24, true) { name = "T_PalmBark", wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.DontSave };
            var px = new Color[N * N];
            var light = new Color(0.62f, 0.45f, 0.30f); var dark = new Color(0.30f, 0.19f, 0.12f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;
                    float ring = Mathf.Abs(Mathf.Sin(v * Mathf.PI * 4f + Noise(u, v, 8) * 1.4f));      // 4 scar bands per tile, wobbling
                    float scar = Sstep(0.55f, 0.95f, ring);                                   // dark thin line at the band edge
                    float fibre = Noise(u, v * 0.25f, 16) * 0.6f + Noise(u, v, 32) * 0.4f;              // vertical fibres: stretched in v
                    float k = Mathf.Clamp01(0.75f - scar * 0.55f + (fibre - 0.5f) * 0.45f);
                    px[y * N + x] = Color.Lerp(dark, light, k);
                }
            t.SetPixels(px); t.Apply(true); return _bark = t;
        }

        static Texture2D FrondTexture()
        {
            if (_frond != null) return _frond;
            const int N = 128;
            var t = new Texture2D(N, N, TextureFormat.RGB24, true) { name = "T_PalmFrond", wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.DontSave };
            var px = new Color[N * N];
            var lo = new Color(0.42f, 0.74f, 0.30f); var hi = new Color(0.78f, 1.0f, 0.46f); var gap = new Color(0.08f, 0.28f, 0.10f);
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N;            // u runs along the frond, v across it
                    float diag = Mathf.Repeat(u * 4f + v * 2f, 1f);        // slanted leaflet lines
                    float line = Sstep(0.0f, 0.2f, diag) * (1f - Sstep(0.8f, 1f, diag));   // 0 in the gap, 1 on the leaflet
                    float tone = Noise(u, v, 8) * 0.5f + Noise(u, v, 24) * 0.5f;
                    var leaflet = Color.Lerp(lo, hi, tone);
                    px[y * N + x] = Color.Lerp(gap, leaflet, line);
                }
            t.SetPixels(px); t.Apply(true); return _frond = t;
        }

        static Material MakeMat(string name, Texture2D tex, Color tint, float smooth)
        {
            var m = Core.RuntimeMats.NewLit(name, tint);
            m.SetTexture("_BaseMap", tex); m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);   // fronds are thin blades: draw both sides
            return m;
        }

        internal static Material BarkMat => _barkMat != null ? _barkMat : _barkMat = MakeMat("M_PalmBark", BarkTexture(), Color.white, 0.1f);
        internal static Material FrondMat => _frondMat != null ? _frondMat : _frondMat = MakeMat("M_PalmFrond", FrondTexture(), Color.white, 0.25f);
    }
}
