using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.World
{
    /// <summary>
    /// Smooth, organic stand-ins for the blocky imported Kenney nature props: meadow flowers (lupine spikes, daisies, cups, stars) with
    /// soft-glow petals like the butterflies, curved grass tufts and rounded stones, all with smooth normals. Scenery.Model asks here
    /// first, so every caller (island dressing, island sides, islets, sky debris) gets them without touching its random stream: the
    /// variation comes from a hash of the position. Meshes are cached and shared; the materials are saved with scenes the builders bake.
    /// </summary>
    public static class OrganicProps
    {
        // ------------------------------------------------------------------ mesh building
        /// <summary>Shared-vertex geometry in a few submeshes; normals are recalculated, so every sheet, tube and blob shades smoothly.</summary>
        internal class Builder
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<int>[] sub;
            public Builder(int subs) { sub = new List<int>[subs]; for (int i = 0; i < subs; i++) sub[i] = new List<int>(); }

            /// <summary>A rows x cols sheet at(u along, w across), wound so its front faces `face`.</summary>
            public void Sheet(int s, int rows, int cols, System.Func<float, float, Vector3> at, Vector3 face)
            {
                int o = v.Count;
                for (int r = 0; r < rows; r++) for (int c = 0; c < cols; c++) v.Add(at(r / (float)(rows - 1), c / (float)(cols - 1)));
                int m = o + (rows / 2) * cols + Mathf.Min(cols / 2, cols - 2);
                bool flip = Vector3.Dot(Vector3.Cross(v[m + cols] - v[m], v[m + 1] - v[m]), face) < 0f;
                for (int r = 0; r < rows - 1; r++)
                    for (int c = 0; c < cols - 1; c++)
                    {
                        int a = o + r * cols + c, b = a + cols, cc = a + 1, d = b + 1;
                        if (flip) sub[s].AddRange(new[] { a, cc, b, cc, d, b }); else sub[s].AddRange(new[] { a, b, cc, cc, b, d });
                    }
            }

            /// <summary>A round tube through `pts` (radius by 0..1 along it) closed at the far end.</summary>
            public void Tube(int s, IList<Vector3> pts, System.Func<float, float> radius, int sides)
            {
                int o = v.Count, n = pts.Count;
                for (int i = 0; i < n; i++)
                {
                    var t = (pts[Mathf.Min(i + 1, n - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
                    var a = Vector3.Cross(t, Mathf.Abs(t.y) > 0.9f ? Vector3.right : Vector3.up).normalized; var b = Vector3.Cross(t, a);
                    float r = radius(i / (float)(n - 1));
                    for (int k = 0; k < sides; k++) { float ang = k * Mathf.PI * 2f / sides; v.Add(pts[i] + (a * Mathf.Cos(ang) + b * Mathf.Sin(ang)) * r); }
                }
                bool flip = Vector3.Dot(Vector3.Cross(v[o + sides] - v[o], v[o + 1] - v[o]), v[o] - pts[0]) < 0f;
                for (int i = 0; i < n - 1; i++)
                    for (int k = 0; k < sides; k++)
                    {
                        int a = o + i * sides + k, b = a + sides, c = o + i * sides + (k + 1) % sides, d = c + sides;
                        if (flip) sub[s].AddRange(new[] { a, c, b, c, d, b }); else sub[s].AddRange(new[] { a, b, c, c, b, d });
                    }
                int tip = v.Count; v.Add(pts[n - 1] + (pts[n - 1] - pts[n - 2]).normalized * radius(1f) * 0.6f);
                for (int k = 0; k < sides; k++)
                {
                    int a = o + (n - 1) * sides + k, c = o + (n - 1) * sides + (k + 1) % sides;
                    if (flip) sub[s].AddRange(new[] { a, c, tip }); else sub[s].AddRange(new[] { a, tip, c });
                }
            }

            /// <summary>An ellipsoid (radii along the rotated axes) with poles on its local Y.</summary>
            public void Blob(int s, Vector3 c, Vector3 radii, Quaternion rot, int around, int rings)
            {
                int o = v.Count;
                v.Add(c + rot * new Vector3(0, -radii.y, 0));
                for (int r = 1; r < rings; r++)
                {
                    float p = -Mathf.PI / 2 + Mathf.PI * r / rings, y = Mathf.Sin(p), q = Mathf.Cos(p);
                    for (int k = 0; k < around; k++) { float a = k * Mathf.PI * 2f / around; v.Add(c + rot * new Vector3(Mathf.Cos(a) * q * radii.x, y * radii.y, Mathf.Sin(a) * q * radii.z)); }
                }
                int top = v.Count; v.Add(c + rot * new Vector3(0, radii.y, 0));
                var tris = new List<int>();
                for (int k = 0; k < around; k++) { int k1 = (k + 1) % around; tris.AddRange(new[] { o, o + 1 + k1, o + 1 + k }); }
                for (int r = 0; r < rings - 2; r++)
                    for (int k = 0; k < around; k++)
                    {
                        int k1 = (k + 1) % around, a = o + 1 + r * around + k, b = o + 1 + r * around + k1, cc = a + around, d = b + around;
                        tris.AddRange(new[] { a, b, cc, b, d, cc });
                    }
                int last = o + 1 + (rings - 2) * around;
                for (int k = 0; k < around; k++) { int k1 = (k + 1) % around; tris.AddRange(new[] { last + k, last + k1, top }); }
                // wind outward
                bool flip = Vector3.Dot(Vector3.Cross(v[tris[1]] - v[tris[0]], v[tris[2]] - v[tris[0]]), v[tris[0]] - c) < 0f;
                for (int i = 0; i < tris.Count; i += 3) { if (flip) { sub[s].Add(tris[i]); sub[s].Add(tris[i + 2]); sub[s].Add(tris[i + 1]); } else { sub[s].Add(tris[i]); sub[s].Add(tris[i + 1]); sub[s].Add(tris[i + 2]); } }
            }

            public Mesh Build(string name)
            {
                var m = new Mesh { name = name, indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
                m.SetVertices(v); m.subMeshCount = sub.Length;
                for (int i = 0; i < sub.Length; i++) m.SetTriangles(sub[i], i);
                m.RecalculateNormals(); m.RecalculateBounds();
                return m;
            }
        }

        // ------------------------------------------------------------------ flowers
        const int Lupine = 0, Daisy = 1, Cup = 2, Star = 3;
        static readonly Dictionary<int, Mesh> Flowers = new Dictionary<int, Mesh>();

        /// <summary>One flower about 1 unit tall (Scenery.Model scales it): submesh 0 stem and leaves, 1 petals, 2 the heart (lupine: its paler tip buds).</summary>
        static Mesh Flower(int shape, int variant)
        {
            int key = shape * 8 + variant;
            if (Flowers.TryGetValue(key, out var cached) && cached != null) return cached;
            var rnd = new System.Random(shape * 101 + variant * 7 + 3);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var b = new Builder(3);
            float la = R(0f, 6.283f), lean = R(0.03f, 0.12f), top = shape == Lupine ? 1f : 0.86f;
            var leanDir = new Vector3(Mathf.Cos(la), 0, Mathf.Sin(la));
            Vector3 StemAt(float t) => leanDir * (lean * t * t) + Vector3.up * (top * t);
            var stem = new List<Vector3>(); for (int i = 0; i <= 6; i++) stem.Add(StemAt(i / 6f));
            b.Tube(0, stem, t => (shape == Lupine ? 0.026f : 0.02f) * (1f - 0.4f * t), 6);

            // leaves: a low rosette of narrow leaflets for the lupine, two or three arching blades up the stem for the others
            int leaves = shape == Lupine ? 6 : 2 + rnd.Next(2);
            for (int k = 0; k < leaves; k++)
            {
                float ang = k * 6.283f / leaves + R(-0.3f, 0.3f), at = shape == Lupine ? R(0.04f, 0.1f) : R(0.04f, 0.3f);
                float len = shape == Lupine ? R(0.22f, 0.3f) : R(0.24f, 0.34f), wid = shape == Lupine ? 0.03f : R(0.04f, 0.06f), rise = R(0.35f, 0.7f);
                var d = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)); var side = new Vector3(-d.z, 0, d.x); var basePt = StemAt(at / top);
                b.Sheet(0, 6, 3, (u, w) =>
                {
                    float sw = w * 2f - 1f, half = wid * Mathf.Sin(u * Mathf.PI) * (1f - 0.3f * u);
                    return basePt + d * (u * len) + Vector3.up * (len * (rise * Mathf.Sin(u * Mathf.PI * 0.6f) - 0.45f * u * u) + Mathf.Abs(sw) * half * 0.35f) + side * (sw * half);
                }, Vector3.up);
            }

            var headUp = (Vector3.up + leanDir * 0.35f).normalized; var head = Quaternion.FromToRotation(Vector3.up, headUp); var hc = StemAt(1f);
            switch (shape)
            {
                case Lupine:   // a tapering spike of small round buds spiralling round the top of the stem, paler toward the tip
                    for (int i = 0, n = 17; i < n; i++)
                    {
                        float t = i / (float)(n - 1), y = 0.48f + 0.5f * t, ang = i * 2.4f;
                        var c = StemAt(y / top) + new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)) * 0.065f * (1f - 0.65f * t);
                        float sz = 0.054f * (1f - 0.5f * t);
                        b.Blob(t > 0.72f ? 2 : 1, c, new Vector3(sz, sz * 0.85f, sz), Quaternion.Euler(0, ang * Mathf.Rad2Deg, 0), 5, 3);
                    }
                    break;
                case Daisy:
                {
                    int n = 11 + rnd.Next(3);
                    for (int i = 0; i < n; i++) Petal(b, hc, head, i * 6.283f / n + R(-0.08f, 0.08f), 0.036f, 0.16f, 0.032f, 0.2f, -0.25f, 0.4f);
                    b.Blob(2, hc + headUp * 0.008f, new Vector3(0.055f, 0.032f, 0.055f), head, 8, 4);
                    break;
                }
                case Cup:      // tulip: two rings of broad petals curving up and in
                    for (int i = 0; i < 6; i++) Petal(b, hc, head, i * 1.047f + (i % 2) * 0.52f, 0.015f, i % 2 == 0 ? 0.2f : 0.185f, 0.072f, 1.05f, 1.6f, 0.45f);
                    b.Blob(2, hc + headUp * 0.03f, new Vector3(0.022f, 0.035f, 0.022f), head, 6, 4);
                    break;
                default:       // star: five wide petals flaring out
                    for (int i = 0; i < 5; i++) Petal(b, hc, head, i * 1.2566f + R(-0.05f, 0.05f), 0.014f, 0.15f, 0.064f, 0.55f, 0.05f, 0.35f);
                    b.Blob(2, hc + headUp * 0.01f, new Vector3(0.028f, 0.02f, 0.028f), head, 7, 4);
                    break;
            }
            var mesh = b.Build("Flower" + key);
            return Flowers[key] = mesh;
        }

        /// <summary>A rounded petal leaving the heart at angle `ang`, rising from `lift0` to `lift1` (radians) along its length, edges cupped up.</summary>
        static void Petal(Builder b, Vector3 c, Quaternion head, float ang, float r0, float len, float wid, float lift0, float lift1, float cup)
        {
            var d = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)); var side = new Vector3(-d.z, 0, d.x);
            float midLift = Mathf.Lerp(lift0, lift1, 0.5f);
            b.Sheet(1, 5, 3, (u, w) =>
            {
                float x = 0f, y = 0f; const int st = 6;
                for (int i = 0; i < st; i++) { float a = Mathf.Lerp(lift0, lift1, (i + 0.5f) / st * u); x += Mathf.Cos(a) * len * u / st; y += Mathf.Sin(a) * len * u / st; }
                float la = Mathf.Lerp(lift0, lift1, u), sw = w * 2f - 1f;
                float half = wid * Mathf.Sin(Mathf.Lerp(0.18f, 0.9f, u) * Mathf.PI);
                var nrm = -d * Mathf.Sin(la) + Vector3.up * Mathf.Cos(la);
                return c + head * (d * (r0 + x) + Vector3.up * y + side * (sw * half) + nrm * (cup * sw * sw * half));
            }, head * (-d * Mathf.Sin(midLift) + Vector3.up * Mathf.Cos(midLift)));
        }

        // ------------------------------------------------------------------ grass tufts
        static readonly Dictionary<int, Mesh> Tufts = new Dictionary<int, Mesh>();

        /// <summary>A tuft of curved, tapering blades about 1 unit tall; normals point up and out from the tuft so it shades like a soft clump.</summary>
        static Mesh Tuft(int variant)
        {
            if (Tufts.TryGetValue(variant, out var cached) && cached != null) return cached;
            var rnd = new System.Random(variant * 977 + 13);
            float R(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            var b = new Builder(1);
            for (int i = 0, n = 9 + rnd.Next(4); i < n; i++)
            {
                float ang = R(0f, 6.283f), h = R(0.55f, 1f), bend = R(0.1f, 0.4f) * h, wid = R(0.03f, 0.045f);
                var d = new Vector3(Mathf.Cos(ang), 0, Mathf.Sin(ang)); var side = new Vector3(-d.z, 0, d.x) * Mathf.Cos(R(-0.6f, 0.6f)) + d * 0.2f;
                var basePt = d * R(0f, 0.08f);
                b.Sheet(0, 5, 2, (u, w) => basePt + d * (bend * u * u) + Vector3.up * (h * u * (1f - 0.15f * u * u)) + side * ((w * 2f - 1f) * wid * (1f - u * 0.95f)), d);
            }
            var m = b.Build("GrassTuft" + variant);
            var nr = new Vector3[b.v.Count];
            for (int i = 0; i < nr.Length; i++) { var p = b.v[i]; nr[i] = (new Vector3(p.x, 0, p.z).normalized * 0.6f + Vector3.up).normalized; }
            m.normals = nr;
            return Tufts[variant] = m;
        }

        // ------------------------------------------------------------------ materials
        static readonly Dictionary<string, Material> Mats = new Dictionary<string, Material>();

        /// <summary>Lit, double-sided, soft; `glow` adds a little emission in the same colour (the butterflies' look). Saved with baked scenes.</summary>
        static Material Soft(Color c, float glow, float smooth = 0.2f)
        {
            string key = ColorUtility.ToHtmlStringRGB(c) + glow.ToString("0.00");
            if (Mats.TryGetValue(key, out var m) && m != null) return m;
            m = RuntimeMats.NewLit("M_Organic_" + key, c); m.hideFlags = HideFlags.None;
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
            if (glow > 0f) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * glow); }
            return Mats[key] = m;
        }

        static Color H(string hex) => Overgrowth.H(hex);

        /// <summary>Loose stones and floating rocks: M_RockLoose (painted stone texture, MaterialBuilder); M_Stone's flagstone cells read as tiled wall.</summary>
        public static Material RockMat() => Kit.Mat("M_RockLoose") ?? Kit.Mat("M_Stone");

        static Color StemColour(LevelTheme t) => t == LevelTheme.Snow ? H("9FB7A8") : t == LevelTheme.Autumn ? H("7E8A3A") : t == LevelTheme.Rain ? H("3F7A45") : H("4E9E48");
        static Color GrassColour(LevelTheme t, int h) => t == LevelTheme.Snow ? H("C8D6D0") : t == LevelTheme.Autumn ? (h % 2 == 0 ? H("A9A04A") : H("C2903E")) : t == LevelTheme.Rain ? H("4C8443") : (h % 3 == 0 ? H("7CC25A") : H("5EAE4C"));

        // the Kenney flower colours map to small families; a clump (same 2 m cell) shares its pick
        static readonly Color[] Reds = { H("E0457B"), H("C2307E"), H("D9434E"), H("F07AA6") };
        static readonly Color[] Purples = { H("9B5DE5"), H("B784E8"), H("6F8DEB"), H("C55BC9") };
        static readonly Color[] Yellows = { H("F6D04D"), H("F7F3EA"), H("F59E3A"), H("FFE38A") };

        // ------------------------------------------------------------------ props
        static int PosHash(Vector3 p) { unchecked { return (Mathf.RoundToInt(p.x * 13.7f) * 73856093) ^ (Mathf.RoundToInt(p.z * 11.3f) * 19349663) ^ (Mathf.RoundToInt(p.y * 7.1f) * 83492791); } }
        static int CellHash(Vector3 p) { unchecked { return (Mathf.FloorToInt(p.x / 2f) * 73856093) ^ (Mathf.FloorToInt(p.z / 2f) * 19349663); } }

        /// <summary>True for the imported models this class replaces.</summary>
        public static bool Replaces(string name) => name.StartsWith("flower_") || name == "grass" || name == "grass_large" || IsRock(name);
        static bool IsRock(string n) => n.StartsWith("rock_") || n.StartsWith("stone_") || n.StartsWith("Rock_Medium");

        /// <summary>The smooth replacement for imported model `name`, standing at `pos` turned by `yaw`, at roughly the original's native size; null when not replaced.</summary>
        public static GameObject Create(string name, Vector3 pos, float yaw, Transform parent, LevelTheme theme)
        {
            if (!Replaces(name)) return null;
            int h = PosHash(pos) & 0x7fffffff;
            var go = new GameObject(name); go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
            var mf = go.AddComponent<MeshFilter>(); var mr = go.AddComponent<MeshRenderer>();
            if (name.StartsWith("flower_"))
            {
                string fam = name.Substring(7, name.Length - 8); char letter = name[name.Length - 1];
                int ch = CellHash(pos) & 0x7fffffff;
                var cols = fam == "red" ? Reds : fam == "purple" ? Purples : Yellows;
                var petal = cols[(ch >> 3) % cols.Length];
                if ((h >> 9) % 5 == 0) petal = cols[(h >> 3) % cols.Length];   // the odd stray colour in a clump
                int shape = letter == 'A' ? ((h >> 5) % 10 < 7 ? Lupine : Daisy) : letter == 'B' ? ((h >> 5) % 2 == 0 ? Daisy : Star) : ((h >> 5) % 2 == 0 ? Cup : Star);
                mf.sharedMesh = Flower(shape, (h >> 12) % 3);
                var heart = shape == Lupine ? Color.Lerp(petal, Color.white, 0.45f) : fam == "yellow" ? H("E07B2A") : H("F2C94C");
                mr.sharedMaterials = new[] { Soft(StemColour(theme), 0f, 0.1f), Soft(petal, 0.2f, 0.25f), Soft(heart, 0.25f, 0.3f) };
                go.transform.localScale = Vector3.one * 0.5f;
            }
            else if (name.StartsWith("grass"))
            {
                mf.sharedMesh = Tuft(h % 6);
                mr.sharedMaterial = Soft(GrassColour(theme, h >> 4), 0f, 0.1f);
                go.transform.localScale = name == "grass_large" ? new Vector3(0.7f, 0.6f, 0.7f) : new Vector3(0.5f, 0.35f, 0.5f);
            }
            else
            {
                // rounded stone: native sizes close to the models it replaces (Kenney small ~0.5 m, tall / large ~1 m, Quaternius medium ~1.4 m)
                bool tall = name.Contains("tall"), large = name.Contains("large"), medium = name.StartsWith("Rock_Medium");
                mf.sharedMesh = DebrisMeshes.Rock(h, medium || large ? 3 : 2);
                mr.sharedMaterial = RockMat();
                float s = medium ? 0.9f : large ? 0.7f : tall ? 0.5f : 0.35f;
                go.transform.localScale = tall ? new Vector3(s * 0.8f, s * 1.7f, s * 0.8f) : new Vector3(s * (1f + (h >> 4) % 4 * 0.1f), s * 0.72f, s);
                if (medium && (h >> 7) % 2 == 0)   // a soft moss cushion on top of about half the bigger stones
                {
                    var moss = new GameObject("Moss"); moss.transform.SetParent(go.transform, false);
                    moss.transform.localPosition = new Vector3(0.05f, 0.42f, -0.03f); moss.transform.localScale = new Vector3(0.78f, 0.36f, 0.72f);
                    moss.AddComponent<MeshFilter>().sharedMesh = DebrisMeshes.Rock(h >> 3, 2);
                    moss.AddComponent<MeshRenderer>().sharedMaterial = Soft(theme == LevelTheme.Snow ? H("EEF3F5") : theme == LevelTheme.Autumn ? H("7A8A36") : H("5DA24F"), 0f, 0.1f);
                }
            }
            return go;
        }
    }
}
