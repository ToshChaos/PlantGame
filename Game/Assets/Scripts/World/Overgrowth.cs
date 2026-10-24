using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.World
{
    /// <summary>
    /// T39: the "overgrown" dressing of an island: leaf litter under the trees (tinted by theme), moss patches on the ground and around
    /// ruins, and hanging vines on the rim and on ruin walls. Everything is merged into a few small static meshes per island (one per
    /// colour), has no collider, and is deterministic from the island index. Lives under a child called "Overgrowth" that the island's
    /// terrain Lift skips (the meshes carry their own heights from the terrain).
    /// </summary>
    public static class Overgrowth
    {
        internal class Batch
        {
            public readonly List<Vector3> v = new List<Vector3>(); public readonly List<Vector3> n = new List<Vector3>(); public readonly List<int> t = new List<int>(); public readonly List<Vector2> uv = new List<Vector2>();
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d); for (int k = 0; k < 4; k++) n.Add(normal);
                t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3, i + 2, i + 1, i, i + 3, i + 2, i });   // both sides
            }
            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c); for (int k = 0; k < 3; k++) n.Add(normal);
                t.AddRange(new[] { i, i + 1, i + 2, i + 2, i + 1, i });
            }
        }

        static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();
        // ---- leaves: a folded, pointed leaf with a veined alpha-cut texture (tinted per colour)
        static Texture2D _leafTex;
        static Texture2D LeafTex()
        {
            if (_leafTex != null) return _leafTex;
            const int W = 64, Hh = 128;
            var tex = new Texture2D(W, Hh, TextureFormat.RGBA32, true) { name = "T_LeafLitter", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color32[W * Hh];
            for (int y = 0; y < Hh; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W * 2f - 1f, v = (y + 0.5f) / Hh;          // u across (-1..1), v along the leaf (0 stem .. 1 tip)
                    float half = Mathf.Sin(Mathf.Pow(v, 0.75f) * Mathf.PI) * (1f - 0.25f * v) * 0.95f;   // wide belly, pointed tip, narrow stem end
                    float edge = half - Mathf.Abs(u);
                    if (edge <= 0f || v < 0.03f) { px[y * W + x] = new Color32(255, 255, 255, 0); continue; }
                    float shade = 0.82f + 0.18f * Mathf.Clamp01(edge * 4f);            // darker rim
                    float mid = Mathf.Abs(u) < 0.045f ? 1f : 0f;                       // midrib
                    // side veins: diagonal lines every ~0.12 along the leaf
                    float d = Mathf.Abs(Mathf.Repeat(v * 7f - Mathf.Abs(u) * 2.2f, 1f) - 0.5f);
                    float vein = d < 0.06f && Mathf.Abs(u) < half * 0.9f ? 1f : 0f;
                    float g = shade * (1f - 0.28f * Mathf.Max(mid, vein)) * (0.9f + 0.1f * Mathf.PerlinNoise(x * 0.3f, y * 0.3f));
                    byte c = (byte)Mathf.Clamp(g * 255f, 0f, 255f);
                    px[y * W + x] = new Color32(c, c, c, 255);
                }
            tex.SetPixels32(px); tex.Apply(true);
            return _leafTex = tex;
        }

        // ---- grass blades: a sprite of a few tapering, leaning blades (alpha-cut), drawn as crossed quads
        static Texture2D _bladeTex;
        static Texture2D BladeTex()
        {
            if (_bladeTex != null) return _bladeTex;
            const int W = 64, Hh = 64;
            var tex = new Texture2D(W, Hh, TextureFormat.RGBA32, true) { name = "T_GrassBlades", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color32[W * Hh];
            var r = new System.Random(4242);
            float[] bx = new float[7], lean = new float[7], hgt = new float[7], wid = new float[7];
            for (int i = 0; i < 7; i++) { bx[i] = 0.12f + 0.76f * (i + (float)r.NextDouble() * 0.6f) / 7f; lean[i] = ((float)r.NextDouble() - 0.5f) * 0.35f; hgt[i] = 0.55f + (float)r.NextDouble() * 0.45f; wid[i] = 0.035f + (float)r.NextDouble() * 0.02f; }
            for (int y = 0; y < Hh; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W, v = (y + 0.5f) / Hh; float a = 0f, shade = 0f;
                    for (int i = 0; i < 7; i++)
                    {
                        if (v > hgt[i]) continue;
                        float t = v / hgt[i], centre = bx[i] + lean[i] * t * t, half = wid[i] * (1f - t * 0.92f);
                        if (Mathf.Abs(u - centre) < half) { a = 1f; shade = Mathf.Max(shade, 0.62f + 0.38f * t); }
                    }
                    byte c = (byte)Mathf.Clamp(shade * 255f, 0f, 255f);
                    px[y * W + x] = new Color32(c, c, c, (byte)(a * 255f));
                }
            tex.SetPixels32(px); tex.Apply(true);
            return _bladeTex = tex;
        }

        static Material _bladeMat;
        static Material BladeMat()
        {
            if (_bladeMat != null) return _bladeMat;
            var m = RuntimeMats.NewLit("M_GrassBlades", new Color(0.45f, 0.78f, 0.35f));
            m.SetTexture("_BaseMap", BladeTex()); m.SetTexture("_MainTex", BladeTex());
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            if (m.HasProperty("_AlphaClip")) { m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450; }
            return _bladeMat = m;
        }

        static Material _purpleBladeMat;
        /// <summary>A few rare purple tufts among the green ones: the same blade sprite tinted violet.</summary>
        static Material PurpleBladeMat()
        {
            if (_purpleBladeMat != null) return _purpleBladeMat;
            var m = new Material(BladeMat()) { name = "M_GrassBladesPurple", hideFlags = HideFlags.DontSave };
            var c = new Color(0.62f, 0.38f, 0.85f);
            m.SetColor("_BaseColor", c); m.SetColor("_Color", c);
            return _purpleBladeMat = m;
        }
        /// <summary>About 3 percent of tufts, picked by position so the random stream (and so the island layout) is untouched.</summary>
        static bool PurpleTuft(Vector2 p) => Mathf.Repeat(Mathf.Sin(p.x * 91.7f + p.y * 12.3f) * 43758.5453f, 1f) < 0.03f;

        static readonly Dictionary<string, Material> LeafMats = new Dictionary<string, Material>();
        static Material LeafMat(Color c)
        {
            string key = ColorUtility.ToHtmlStringRGB(c);
            if (LeafMats.TryGetValue(key, out var m) && m != null) return m;
            m = RuntimeMats.NewLit("M_LeafLitter_" + key, c);
            m.SetTexture("_BaseMap", LeafTex()); m.SetTexture("_MainTex", LeafTex());
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.12f);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            if (m.HasProperty("_AlphaClip")) { m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.5f); m.EnableKeyword("_ALPHATEST_ON"); m.renderQueue = 2450; }
            return LeafMats[key] = m;
        }

        /// <summary>A leaf lying on the ground: folded a little along the midrib, tips curled up, UVs for the leaf texture. `len` in metres.</summary>
        static void AddLeaf(Batch b, Vector3 at, float yaw, float len, float curl)
        {
            var fwd = new Vector3(Mathf.Cos(yaw), 0, Mathf.Sin(yaw)); var side = new Vector3(-fwd.z, 0, fwd.x);
            float w = len * 0.28f, fold = len * 0.07f;
            Vector3 P(float along, float across, float lift) => at + fwd * (along - len * 0.5f) + side * across + Vector3.up * (lift + curl * Mathf.Pow(Mathf.Abs(along / len - 0.5f) * 2f, 2f) * len * 0.12f);
            int i = b.v.Count;
            // 3 rows (stem, belly, tip) x 3 columns (left edge, midrib, right edge)
            float[] along = { 0f, len * 0.45f, len }; float[] wid = { w * 0.2f, w, w * 0.15f };
            for (int r = 0; r < 3; r++)
                for (int c = 0; c < 3; c++)
                {
                    float across = (c - 1) * wid[r];
                    b.v.Add(P(along[r], across, c == 1 ? fold : 0f)); b.n.Add(Vector3.up);
                    b.uv.Add(new Vector2(c * 0.5f, along[r] / len));
                }
            for (int r = 0; r < 2; r++)
                for (int c = 0; c < 2; c++)
                {
                    int a = i + r * 3 + c, bb = a + 1, cc = a + 3, dd = a + 4;
                    b.t.AddRange(new[] { a, cc, bb, bb, cc, dd });
                }
        }

        internal static Material ColourMat(Color c)
        {
            string key = ColorUtility.ToHtmlStringRGB(c);
            if (Mats.TryGetValue(key, out var m) && m != null) return m;
            m = RuntimeMats.NewLit("M_Overgrowth_" + key, c); m.hideFlags = HideFlags.None;   // saved with scenes the builders bake
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.08f);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            Mats[key] = m;
            return m;
        }

        static Color[] LitterColours(LevelTheme theme)
        {
            switch (theme)
            {
                case LevelTheme.Autumn: return new[] { H("B8412B"), H("D9742C"), H("E3AE3A"), H("8A5A2E") };
                case LevelTheme.Rain: return new[] { H("3E6B3A"), H("57803F"), H("6E5A38"), H("48623A") };
                default: return new[] { H("5DA24F"), H("8DBB4A"), H("C9B24A"), H("7A5B34") };
            }
        }
        public static Color LeafColour(LevelTheme theme, int index) { var c = LitterColours(theme); return c[index % 3]; }
        internal static Color H(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }

        /// <param name="free">True when a spot is clear of lanes and keep points (not of trees: litter belongs under them).</param>
        /// <param name="treeSpots">x, z, trunk clearance of trees and ruins already placed (litter gathers around them).</param>
        public static void Apply(Transform isle, Vector3 centre, int index, System.Func<float, float> edge, System.Func<Vector2, float, bool> free,
                                 List<Vector3> treeSpots, List<Vector3> ruinSpots, float lush, LevelTheme theme, bool grassy = false, GroundKind ground = GroundKind.Grass)
        {
            var terrain = isle.GetComponent<IslandTerrain>();
            if (terrain == null) return;
            var rnd = new System.Random(index * 7919 + 53);   // own random: the rest of the scenery is unchanged
            var root = new GameObject("Overgrowth"); root.transform.SetParent(isle, true); root.transform.position = Vector3.zero;
            float Y(Vector2 p) => centre.y + terrain.HeightAtWorld(new Vector3(p.x, centre.y, p.y));
            bool Dry(Vector2 p) => Y(p) - centre.y > IslandTerrain.WaterLevel + 0.08f;
            var c2 = new Vector2(centre.x, centre.z);

            // ---- leaf litter: rings of leaves under every tree plus loose patches
            var cols = LitterColours(theme);
            var litter = new Batch[cols.Length]; for (int i = 0; i < litter.Length; i++) litter[i] = new Batch();
            void Leaf(Vector2 p)
            {
                if (!Dry(p)) return;
                var bt = litter[rnd.Next(litter.Length)];
                AddLeaf(bt, new Vector3(p.x, Y(p) + 0.025f, p.y), (float)(rnd.NextDouble() * 6.283), 0.26f + (float)rnd.NextDouble() * 0.2f, 0.2f + (float)rnd.NextDouble() * 0.5f);
            }
            foreach (var ts in treeSpots)
            {
                if (ts.z < 2.8f) continue;
                int n = Mathf.RoundToInt((10 + rnd.Next(12)) * Mathf.Lerp(0.5f, 1f, lush));
                for (int k = 0; k < n; k++)
                {
                    float a = (float)(rnd.NextDouble() * 6.283), r = 0.5f + (float)(rnd.NextDouble() * rnd.NextDouble()) * 3.2f;
                    var p = new Vector2(ts.x + Mathf.Cos(a) * r, ts.y + Mathf.Sin(a) * r);
                    if (Vector2.Distance(p, c2) < edge(Mathf.Atan2(p.y - c2.y, p.x - c2.x)) * 0.94f && free(p, 0.3f)) Leaf(p);
                }
            }
            int loose = Mathf.RoundToInt(7 * Mathf.Max(0.4f, lush) * Mathf.Max(1f, (edge(0f) * edge(0f)) / 100f));
            for (int k = 0; k < loose; k++)
            {
                float a = (float)(rnd.NextDouble() * 6.283), r = edge(a) * (0.2f + (float)rnd.NextDouble() * 0.72f);
                var p = c2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (!free(p, 0.6f)) continue;
                for (int m = 0, n = 4 + rnd.Next(7); m < n; m++) Leaf(p + new Vector2((float)(rnd.NextDouble() - 0.5), (float)(rnd.NextDouble() - 0.5)) * 1.6f);
            }
            for (int i = 0; i < litter.Length; i++) Emit(root.transform, "Litter" + i, litter[i], LeafMat(cols[i]));

            // ---- moss: dark green patches on the ground, thick around ruins
            var moss = new Batch[2]; moss[0] = new Batch(); moss[1] = new Batch();
            void Moss(Vector2 p, float r)
            {
                if (!Dry(p)) return;
                var b = moss[rnd.Next(2)]; int segs = 9; float ph = (float)(rnd.NextDouble() * 6.283);
                var o = new Vector3(p.x, 0, p.y);
                for (int s = 0; s < segs; s++)
                {
                    float a0 = s * 6.283f / segs, a1 = (s + 1) * 6.283f / segs;
                    float r0 = r * (0.7f + 0.3f * Mathf.Sin(a0 * 3f + ph)), r1 = r * (0.7f + 0.3f * Mathf.Sin(a1 * 3f + ph));
                    Vector3 P(float a, float rr) { var q = new Vector2(p.x + Mathf.Cos(a) * rr, p.y + Mathf.Sin(a) * rr); return new Vector3(q.x, Y(q) + 0.045f, q.y); }
                    b.Tri(new Vector3(p.x, Y(p) + 0.05f, p.y), P(a0, r0), P(a1, r1), Vector3.up);
                }
            }
            foreach (var rs in ruinSpots) for (int k = 0; k < 5; k++)
                {
                    float a = (float)(rnd.NextDouble() * 6.283), r = 0.4f + (float)rnd.NextDouble() * 2.2f;
                    Moss(new Vector2(rs.x + Mathf.Cos(a) * r, rs.z + Mathf.Sin(a) * r), 0.6f + (float)rnd.NextDouble() * 0.9f);
                }
            int mossN = Mathf.RoundToInt(6 * Mathf.Max(0.4f, lush) * Mathf.Max(1f, (edge(0f) * edge(0f)) / 100f));
            for (int k = 0; k < mossN; k++)
            {
                float a = (float)(rnd.NextDouble() * 6.283), r = edge(a) * (0.25f + (float)rnd.NextDouble() * 0.68f);
                var p = c2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (free(p, 0.8f)) Moss(p, 0.7f + (float)rnd.NextDouble() * 1.1f);
            }
            Emit(root.transform, "MossA", moss[0], ColourMat(theme == LevelTheme.Autumn ? H("4F6B2C") : H("3C7A3A")));
            Emit(root.transform, "MossB", moss[1], ColourMat(theme == LevelTheme.Autumn ? H("6A7A30") : H("55903F")));

            // ---- grass blades: tufts of blade sprites over grassy ground (not on mud, rock or snow)
            if (grassy)
            {
                var blades = new Batch(); var purple = new Batch();
                int tufts = Mathf.RoundToInt(70 * Mathf.Max(1f, (edge(0f) * edge(0f)) / 64f));
                for (int k = 0; k < tufts; k++)
                {
                    float a = (float)(rnd.NextDouble() * 6.283), r = edge(a) * (0.1f + (float)rnd.NextDouble() * 0.84f);
                    var p = c2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    if (!Dry(p) || !free(p, 0.5f)) continue;
                    float yaw = (float)(rnd.NextDouble() * 3.1416), size = 0.35f + (float)rnd.NextDouble() * 0.35f;
                    var tb = PurpleTuft(p) ? purple : blades;
                    for (int q = 0; q < 2; q++)
                    {
                        float ya = yaw + q * 1.5708f; var d = new Vector3(Mathf.Cos(ya), 0, Mathf.Sin(ya)) * size * 0.5f;
                        var b0 = new Vector3(p.x, Y(p) - 0.02f, p.y); int i0 = tb.v.Count;
                        tb.v.Add(b0 - d); tb.v.Add(b0 + d); tb.v.Add(b0 + d + Vector3.up * size); tb.v.Add(b0 - d + Vector3.up * size);
                        tb.uv.Add(new Vector2(0, 0)); tb.uv.Add(new Vector2(1, 0)); tb.uv.Add(new Vector2(1, 1)); tb.uv.Add(new Vector2(0, 1));
                        for (int n = 0; n < 4; n++) tb.n.Add(Vector3.up);
                        tb.t.AddRange(new[] { i0, i0 + 2, i0 + 1, i0, i0 + 3, i0 + 2 });
                    }
                }
                Emit(root.transform, "GrassBlades", blades, BladeMat());
                Emit(root.transform, "GrassPurple", purple, PurpleBladeMat());
            }

            Clutter(root.transform, centre, index, edge, free, treeSpots, theme, grassy, ground, Y, Dry);

            // ---- vines: hang from the rim and drape on ruin walls
            var vines = new Batch(); var vineLeaves = new Batch(); var roots = new Batch();
            void Vine(Vector3 top, float length, Vector3 outward, float sway, Batch strand = null, float w = 0.07f, bool leaves = true)
            {
                strand = strand ?? vines;
                int segs = Mathf.Max(3, Mathf.CeilToInt(length / 0.45f)); float ph = (float)(rnd.NextDouble() * 6.283);
                Vector3 prev = top; var tang = Vector3.Cross(Vector3.up, outward).normalized;
                for (int s = 1; s <= segs; s++)
                {
                    float f = s / (float)segs;
                    var p = top + Vector3.down * length * f + tang * Mathf.Sin(f * 5f + ph) * sway + outward * 0.04f;
                    var mid = (prev + p) * 0.5f;
                    strand.Quad(prev - tang * w, prev + tang * w, p + tang * w, p - tang * w, outward);
                    if (leaves && s % 2 == 0)   // a leaf on alternating sides
                    {
                        float side = (s % 4 == 0 ? 1f : -1f);
                        var tip = mid + tang * side * 0.28f + Vector3.down * 0.06f + outward * 0.05f;
                        vineLeaves.Quad(mid + Vector3.up * 0.05f, mid + outward * 0.08f + tang * side * 0.12f - Vector3.up * 0.1f, tip, mid + tang * side * 0.12f + Vector3.up * 0.1f, outward);
                    }
                    prev = p;
                }
            }
            int rimVines = Mathf.RoundToInt((4 + rnd.Next(5)) * Mathf.Lerp(0.5f, 1.2f, lush) * Mathf.Max(1f, edge(0f) / 10f));
            for (int k = 0; k < rimVines; k++)
            {
                float a = (float)(rnd.NextDouble() * 6.283); float rr = edge(a) * 0.995f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                float drop = rnd.NextDouble() < 0.5 ? (float)rnd.NextDouble() * 2.0f : 0f;   // some start from ledges lower on the wall
                var top = new Vector3(centre.x + dir.x * rr, centre.y + IslandTerrain.Shoulder + 0.15f - drop, centre.z + dir.z * rr);
                for (int m = 0, n = 1 + rnd.Next(3); m < n; m++)
                {
                    var off = Vector3.Cross(Vector3.up, dir) * (m * 0.5f - 0.3f);
                    Vine(top + off, 1.6f + (float)rnd.NextDouble() * 2.6f, dir, 0.12f);
                }
            }
            int rootN = Mathf.RoundToInt((3 + rnd.Next(5)) * Mathf.Max(1f, edge(0f) / 10f));   // bare roots and tendrils hanging down the side
            for (int k = 0; k < rootN; k++)
            {
                float a = (float)(rnd.NextDouble() * 6.283); float rr = edge(a) * 0.99f;
                var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var top = new Vector3(centre.x + dir.x * rr, centre.y + IslandTerrain.Shoulder - (float)rnd.NextDouble() * 1.8f, centre.z + dir.z * rr);
                Vine(top, 1.2f + (float)rnd.NextDouble() * 2.4f, dir, 0.18f, roots, 0.045f + (float)rnd.NextDouble() * 0.05f, false);
            }
            foreach (var rs in ruinSpots)
                for (int k = 0; k < 6; k++)
                {
                    float a = (float)(rnd.NextDouble() * 6.283), r = 0.5f + (float)rnd.NextDouble() * 1.7f;
                    var p = new Vector2(rs.x + Mathf.Cos(a) * r, rs.z + Mathf.Sin(a) * r);
                    var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    float h = 1.2f + (float)rnd.NextDouble() * 2.2f;
                    Vine(new Vector3(p.x, Y(p) + h, p.y), h, dir, 0.08f);
                }
            // some ruins are swallowed by vines: dozens of strands down every face, leafy clumps clinging at every height
            for (int ri = 0; ri < ruinSpots.Count; ri++)
            {
                var rs = ruinSpots[ri];
                if (new System.Random(index * 131 + ri * 17 + 5).NextDouble() > 0.5) continue;
                for (int k = 0; k < 26; k++)
                {
                    float a = (float)(rnd.NextDouble() * 6.283), r = 0.5f + (float)rnd.NextDouble() * 1.9f;
                    var p = new Vector2(rs.x + Mathf.Cos(a) * r, rs.z + Mathf.Sin(a) * r);
                    var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                    float h = 1.4f + (float)rnd.NextDouble() * 2.6f;
                    Vine(new Vector3(p.x, Y(p) + h, p.y), h * (0.7f + (float)rnd.NextDouble() * 0.3f), dir, 0.1f, null, 0.09f);
                }
                for (int k = 0; k < 14; k++)
                {
                    float a = (float)(rnd.NextDouble() * 6.283), r = 0.4f + (float)rnd.NextDouble() * 1.8f;
                    var p = new Vector2(rs.x + Mathf.Cos(a) * r, rs.z + Mathf.Sin(a) * r);
                    float sz = 0.25f + (float)rnd.NextDouble() * 0.3f;
                    Blob(vineLeaves, new Vector3(p.x, Y(p) + 0.2f + (float)rnd.NextDouble() * 2.6f, p.y), sz, sz * 0.8f, (float)(rnd.NextDouble() * 6.283), rnd);
                }
            }
            Emit(root.transform, "Roots", roots, ColourMat(H("5A4128")));
            Emit(root.transform, "Vines", vines, ColourMat(theme == LevelTheme.Autumn ? H("5E5A2C") : H("2F6B3A")));
            Emit(root.transform, "VineLeaves", vineLeaves, ColourMat(theme == LevelTheme.Autumn ? H("A6903A") : H("5DB46B")));
        }


        // ---- extra ground clutter: more grass, mud / rock decals, extra moss, low shrubs, sticks and pebbles. Own random, so the layout above is unchanged.
        internal static void Blob(Batch b, Vector3 at, float r, float h, float yaw, System.Random rnd)
        {
            const int N = 6;
            var low = new Vector3[N]; var mid = new Vector3[N];
            for (int i = 0; i < N; i++)
            {
                float a = yaw + i * 6.2832f / N; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                low[i] = at + d * r * (0.85f + 0.3f * (float)rnd.NextDouble()) - Vector3.up * h * 0.1f;
                mid[i] = at + d * r * (0.7f + 0.25f * (float)rnd.NextDouble()) + Vector3.up * h * 0.55f;
            }
            var top = at + Vector3.up * h * (0.9f + 0.2f * (float)rnd.NextDouble());
            for (int i = 0; i < N; i++)
            {
                int j = (i + 1) % N;
                b.Quad(low[i], low[j], mid[j], mid[i], (low[i] - at + Vector3.up * h * 0.4f).normalized);
                b.Tri(mid[i], mid[j], top, (mid[i] + mid[j] - at * 2f + Vector3.up * h).normalized);
            }
        }

        static void Clutter(Transform root, Vector3 centre, int index, System.Func<float, float> edge, System.Func<Vector2, float, bool> free,
                            List<Vector3> treeSpots, LevelTheme theme, bool grassy, GroundKind ground, System.Func<Vector2, float> Y, System.Func<Vector2, bool> Dry)
        {
            var rnd = new System.Random(index * 6151 + 977);
            var c2 = new Vector2(centre.x, centre.z);
            float area = Mathf.Max(1f, edge(0f) * edge(0f) / 64f);
            bool snow = theme == LevelTheme.Snow, autumn = theme == LevelTheme.Autumn;
            Vector2 Spot(float lo, float hi) { float a = (float)(rnd.NextDouble() * 6.283), r = edge(a) * (lo + (float)rnd.NextDouble() * (hi - lo)); return c2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; }

            // grass: a lot more on grassy islands, a few tufts clinging on in mud and rock
            if (!snow)
            {
                int n = Mathf.RoundToInt((grassy ? 520 : ground == GroundKind.Mud ? 60 : 26) * area);
                var blades = new Batch(); var purple = new Batch();
                for (int k = 0; k < n; k++)
                {
                    var p = Spot(0.05f, 0.95f);
                    if (!Dry(p) || !free(p, 0.45f)) continue;
                    float yaw = (float)(rnd.NextDouble() * 3.1416), size = 0.35f + (float)rnd.NextDouble() * 0.6f;
                    var tb = PurpleTuft(p) ? purple : blades;
                    for (int q = 0; q < 2; q++)
                    {
                        float ya = yaw + q * 1.5708f; var d = new Vector3(Mathf.Cos(ya), 0, Mathf.Sin(ya)) * size * 0.5f;
                        var b0 = new Vector3(p.x, Y(p) - 0.02f, p.y); int i0 = tb.v.Count;
                        tb.v.Add(b0 - d); tb.v.Add(b0 + d); tb.v.Add(b0 + d + Vector3.up * size); tb.v.Add(b0 - d + Vector3.up * size);
                        tb.uv.Add(new Vector2(0, 0)); tb.uv.Add(new Vector2(1, 0)); tb.uv.Add(new Vector2(1, 1)); tb.uv.Add(new Vector2(0, 1));
                        for (int m = 0; m < 4; m++) tb.n.Add(Vector3.up);
                        tb.t.AddRange(new[] { i0, i0 + 2, i0 + 1, i0, i0 + 3, i0 + 2 });
                    }
                }
                Emit(root, "GrassMore", blades, BladeMat());
                Emit(root, "GrassMorePurple", purple, PurpleBladeMat());
            }

            // flat irregular patches (decals): moss everywhere, bare mud and stone on the bare islands, dirt scuffs on grass
            Color[] patchCols; int patchN;
            if (ground == GroundKind.Mud) { patchCols = new[] { H("46301F"), H("5C4330"), H("3A2A1C") }; patchN = 16; }
            else if (ground == GroundKind.Rock) { patchCols = new[] { H("3E4047"), H("5A4A3C"), H("34363C") }; patchN = 18; }
            else { patchCols = new[] { H("6B5238"), H("594330") }; patchN = 5; }
            var patches = new Batch[patchCols.Length]; for (int i = 0; i < patches.Length; i++) patches[i] = new Batch();
            var moss2 = new[] { new Batch(), new Batch() };
            void Patch(Batch b, Vector2 p, float rx, float rz, float lift)
            {
                if (!Dry(p)) return;
                int segs = 10; float ph = (float)(rnd.NextDouble() * 6.283), yaw = (float)(rnd.NextDouble() * 3.1416);
                Vector3 P(float a, float f) { var o = new Vector2(Mathf.Cos(a) * rx * f, Mathf.Sin(a) * rz * f); var q = p + new Vector2(o.x * Mathf.Cos(yaw) - o.y * Mathf.Sin(yaw), o.x * Mathf.Sin(yaw) + o.y * Mathf.Cos(yaw)); return new Vector3(q.x, Y(q) + lift, q.y); }
                for (int s = 0; s < segs; s++)
                {
                    float a0 = s * 6.283f / segs, a1 = (s + 1) * 6.283f / segs;
                    b.Tri(new Vector3(p.x, Y(p) + lift + 0.005f, p.y), P(a0, 0.72f + 0.28f * Mathf.Sin(a0 * 3f + ph)), P(a1, 0.72f + 0.28f * Mathf.Sin(a1 * 3f + ph)), Vector3.up);
                }
            }
            for (int k = 0, n = Mathf.RoundToInt(patchN * area); k < n; k++)
            {
                var p = Spot(0.08f, 0.9f); if (!free(p, 0.6f)) continue;
                float r = ground == GroundKind.Grass ? 0.5f + (float)rnd.NextDouble() * 0.6f : 0.8f + (float)rnd.NextDouble() * 1.5f;
                Patch(patches[rnd.Next(patches.Length)], p, r, r * (0.6f + (float)rnd.NextDouble() * 0.5f), 0.04f);
            }
            for (int k = 0, n = Mathf.RoundToInt((ground == GroundKind.Grass ? 8 : 14) * area); k < n; k++)   // moss on every kind of ground, more on bare stone
            {
                var p = Spot(0.1f, 0.95f); if (!free(p, 0.5f)) continue;
                float r = 0.4f + (float)rnd.NextDouble() * 0.8f;
                Patch(moss2[rnd.Next(2)], p, r, r * 0.8f, 0.05f);
            }
            for (int i = 0; i < patches.Length; i++) Emit(root, "Decal" + i, patches[i], ColourMat(patchCols[i]));
            Emit(root, "MossMoreA", moss2[0], ColourMat(autumn ? H("4F6B2C") : H("3C7A3A")));
            Emit(root, "MossMoreB", moss2[1], ColourMat(autumn ? H("6A7A30") : H("55903F")));

            // low shrubs (also gathered round trees), sticks and pebbles
            Color[] shrubCols = autumn ? new[] { H("A6642A"), H("8A5A2E"), H("6A7A30") } : new[] { H("3F8A47"), H("5DA24F"), H("2F6B3A") };
            var shrubs = new Batch[shrubCols.Length]; for (int i = 0; i < shrubs.Length; i++) shrubs[i] = new Batch();
            float shrubScale = ground == GroundKind.Grass ? 1f : ground == GroundKind.Mud ? 0.5f : 0.25f;
            for (int k = 0, n = Mathf.RoundToInt(40 * shrubScale * area); k < n; k++)
            {
                var p = Spot(0.15f, 0.93f); if (!Dry(p) || !free(p, 0.7f)) continue;
                float r = 0.22f + (float)rnd.NextDouble() * 0.3f;
                for (int m = 0, c = 1 + rnd.Next(3); m < c; m++)   // a little cluster of blobs
                {
                    var q = p + new Vector2((float)(rnd.NextDouble() - 0.5), (float)(rnd.NextDouble() - 0.5)) * r * 1.4f;
                    Blob(shrubs[rnd.Next(shrubs.Length)], new Vector3(q.x, Y(q), q.y), r * (0.7f + (float)rnd.NextDouble() * 0.5f), r * (0.8f + (float)rnd.NextDouble() * 0.6f), (float)(rnd.NextDouble() * 6.283), rnd);
                }
            }
            foreach (var ts in treeSpots) if (ts.z >= 2.8f && ground == GroundKind.Grass)
                    for (int k = 0, n = 1 + rnd.Next(3); k < n; k++)
                    {
                        float a = (float)(rnd.NextDouble() * 6.283), rr = 1.1f + (float)rnd.NextDouble() * 1.8f;
                        var p = new Vector2(ts.x + Mathf.Cos(a) * rr, ts.y + Mathf.Sin(a) * rr);
                        if (!Dry(p) || !free(p, 0.4f) || Vector2.Distance(p, c2) > edge(Mathf.Atan2(p.y - c2.y, p.x - c2.x)) * 0.92f) continue;
                        float r = 0.2f + (float)rnd.NextDouble() * 0.25f;
                        Blob(shrubs[rnd.Next(shrubs.Length)], new Vector3(p.x, Y(p), p.y), r, r * 0.9f, (float)(rnd.NextDouble() * 6.283), rnd);
                    }
            for (int i = 0; i < shrubs.Length; i++) Emit(root, "Shrubs" + i, shrubs[i], ColourMat(shrubCols[i]));

            var sticks = new[] { new Batch(), new Batch() };
            for (int k = 0, n = Mathf.RoundToInt(45 * area); k < n; k++)
            {
                var p = Spot(0.1f, 0.94f); if (!Dry(p) || !free(p, 0.4f)) continue;
                var b = sticks[rnd.Next(2)];
                float yaw = (float)(rnd.NextDouble() * 6.283), len = 0.35f + (float)rnd.NextDouble() * 0.8f, w = 0.025f + (float)rnd.NextDouble() * 0.025f;
                void Stick(Vector2 at, float ya, float l)
                {
                    var d = new Vector2(Mathf.Cos(ya), Mathf.Sin(ya)); var sd = new Vector2(-d.y, d.x) * w;
                    Vector2 a = at - d * l * 0.5f, e = at + d * l * 0.5f;
                    Vector3 V(Vector2 q, float lift) => new Vector3(q.x, Y(q) + lift, q.y);
                    b.Quad(V(a - sd, 0.03f), V(a + sd, 0.03f), V(e + sd, 0.05f), V(e - sd, 0.05f), Vector3.up);   // a flat strip just above the ground
                    b.Quad(V(a, 0.07f) - Vector3.right * w * 0.4f, V(a, 0.07f) + Vector3.right * w * 0.4f, V(e, 0.09f) + Vector3.right * w * 0.4f, V(e, 0.09f) - Vector3.right * w * 0.4f, Vector3.up);
                }
                Stick(p, yaw, len);
                if (rnd.NextDouble() < 0.5) Stick(p + new Vector2(Mathf.Cos(yaw), Mathf.Sin(yaw)) * len * 0.15f, yaw + 0.7f + (float)rnd.NextDouble() * 0.8f, len * 0.45f);   // a side twig
            }
            Emit(root, "SticksA", sticks[0], ColourMat(H("6B4A2B")));
            Emit(root, "SticksB", sticks[1], ColourMat(H("4E3520")));

            var pebbles = new[] { new Batch(), new Batch() };
            float pebbleScale = ground == GroundKind.Grass ? 1f : 2.5f;
            for (int k = 0, n = Mathf.RoundToInt(20 * pebbleScale * area); k < n; k++)
            {
                var p = Spot(0.08f, 0.95f); if (!Dry(p) || !free(p, 0.35f)) continue;
                float r = 0.07f + (float)rnd.NextDouble() * 0.12f;
                for (int m = 0, c = 1 + rnd.Next(3); m < c; m++)
                {
                    var q = p + new Vector2((float)(rnd.NextDouble() - 0.5), (float)(rnd.NextDouble() - 0.5)) * 0.4f;
                    Blob(pebbles[rnd.Next(2)], new Vector3(q.x, Y(q), q.y), r * (0.6f + (float)rnd.NextDouble() * 0.7f), r * 0.6f, (float)(rnd.NextDouble() * 6.283), rnd);
                }
            }
            Emit(root, "PebblesA", pebbles[0], ColourMat(ground == GroundKind.Mud ? H("7A6A58") : H("8A8C92")));
            Emit(root, "PebblesB", pebbles[1], ColourMat(ground == GroundKind.Mud ? H("5E5040") : H("6A6C74")));
        }

        internal static void Emit(Transform parent, string name, Batch b, Material m)
        {
            if (b.v.Count == 0) { return; }
            var go = new GameObject(name); go.transform.SetParent(parent, false);
            var mesh = new Mesh { name = name, indexFormat = b.v.Count > 60000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(b.v); mesh.SetNormals(b.n); if (b.uv.Count == b.v.Count) mesh.SetUVs(0, b.uv); mesh.SetTriangles(b.t, 0); mesh.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = m;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
