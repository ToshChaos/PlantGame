using System.Collections.Generic;
using UnityEngine;

namespace PlantGame.World
{
    /// <summary>
    /// Textures for the imported Kenney palms (their own UVs only address a flat colour palette): a procedural ringed bark and a leaflet-striped frond texture,
    /// plus new UVs (bark: ring bands up the trunk; fronds: radial from the crown, leaflet stripes along the blade). Meshes are copied once per source mesh.
    /// </summary>
    public static class PalmLook
    {
        static Texture2D _bark, _frond;
        static Material _barkMat, _frondMat;
        static readonly Dictionary<Mesh, Mesh> Cache = new Dictionary<Mesh, Mesh>();

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

        static Material BarkMat => _barkMat != null ? _barkMat : _barkMat = MakeMat("M_PalmBark", BarkTexture(), Color.white, 0.1f);
        static Material FrondMat => _frondMat != null ? _frondMat : _frondMat = MakeMat("M_PalmFrond", FrondTexture(), Color.white, 0.25f);

        /// <summary>Swaps the palm's bark and frond materials for the textured ones and rewrites the UVs. Models without readable meshes keep the flat colours.</summary>
        public static void Apply(GameObject model)
        {
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
            {
                var mr = mf.GetComponent<MeshRenderer>(); var src = mf.sharedMesh;
                if (mr == null || src == null || !src.isReadable) continue;
                var mats = mr.sharedMaterials; var kinds = new int[mats.Length];   // 1 = bark, 2 = frond
                for (int i = 0; i < mats.Length; i++) kinds[i] = mats[i] == null ? 0 : mats[i].name.Contains("wood") ? 1 : mats[i].name.Contains("leaf") ? 2 : 0;
                if (!Cache.TryGetValue(src, out var m) || m == null) Cache[src] = m = Retarget(src, kinds);
                mf.sharedMesh = m;
                for (int i = 0; i < mats.Length; i++) mats[i] = kinds[i] == 1 ? BarkMat : kinds[i] == 2 ? FrondMat : mats[i];
                mr.sharedMaterials = mats;
            }
        }

        static Mesh Retarget(Mesh src, int[] kinds)
        {
            var m = Object.Instantiate(src); m.name = src.name + "_Palm";
            var verts = src.vertices; var uv = new Vector2[verts.Length];
            // which submesh owns each vertex
            var owner = new int[verts.Length];
            for (int s = 0; s < src.subMeshCount; s++) foreach (int idx in src.GetIndices(s)) owner[idx] = s;
            // crown centre = centre of the frond vertices; the angular seam sits in the emptiest direction so no blade straddles it
            Vector2 c = Vector2.zero; int n = 0; var bins = new int[12];
            for (int i = 0; i < verts.Length; i++) if (owner[i] < kinds.Length && kinds[owner[i]] == 2) { c += new Vector2(verts[i].x, verts[i].z); n++; }
            if (n > 0) c /= n;
            for (int i = 0; i < verts.Length; i++) if (owner[i] < kinds.Length && kinds[owner[i]] == 2)
                { float a = Mathf.Atan2(verts[i].z - c.y, verts[i].x - c.x); bins[Mathf.Clamp(Mathf.FloorToInt((a + Mathf.PI) / (2 * Mathf.PI) * 12), 0, 11)]++; }
            int best = 0; for (int b = 1; b < 12; b++) if (bins[b] < bins[best]) best = b;
            float seam = -Mathf.PI + (best + 0.5f) * (2 * Mathf.PI / 12);
            for (int i = 0; i < verts.Length; i++)
            {
                int kind = owner[i] < kinds.Length ? kinds[owner[i]] : 0;
                var p = verts[i];
                if (kind == 1) uv[i] = new Vector2((p.x + p.z) * 2.2f, p.y * 2.4f);     // bark: bands climb with height (model is about 1.4 tall)
                else if (kind == 2)
                {
                    float a = Mathf.Repeat(Mathf.Atan2(p.z - c.y, p.x - c.x) - seam, 2 * Mathf.PI);
                    uv[i] = new Vector2(new Vector2(p.x - c.x, p.z - c.y).magnitude * 5f, a / (2 * Mathf.PI) * 12f);
                }
                else uv[i] = Vector2.zero;
            }
            m.uv = uv; return m;
        }
    }
}
