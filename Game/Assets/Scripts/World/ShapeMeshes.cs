using System.Collections.Generic;
using UnityEngine;

namespace PlantGame.World
{
    /// <summary>
    /// Small procedural shapes shared by the goal objects and the melon patch: lathes (columns, bases, fruit, logs), tori (rings, rims),
    /// a waving pennant, arch bands, tubes (vines, tendrils) and a ribbed melon. Y is the axis of every lathe, profiles run bottom to top
    /// with the solid on the left (radius, height).
    /// </summary>
    public static class ShapeMeshes
    {
        /// <param name="hard">separate rows per profile segment, so steps in the profile stay crisp</param>
        /// <param name="mul">radius multiplier per (angle, profile index): flutes, ridges, bark</param>
        public static Mesh Lathe(IList<Vector2> prof, int around, bool hard = false, System.Func<float, int, float> mul = null, string name = "Lathe")
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            int n = prof.Count;
            Vector3 Pt(int p, int a)
            {
                float ang = a * Mathf.PI * 2f / around, r = prof[p].x * (mul != null ? mul(ang, p) : 1f);
                return new Vector3(Mathf.Cos(ang) * r, prof[p].y, Mathf.Sin(ang) * r);
            }
            if (!hard)
            {
                for (int p = 0; p < n; p++)
                    for (int a = 0; a < around; a++) { v.Add(Pt(p, a)); uv.Add(new Vector2(a / (float)around, p / (float)(n - 1))); }
                for (int p = 0; p < n - 1; p++)
                    for (int a = 0; a < around; a++)
                    {
                        int a2 = (a + 1) % around, i0 = p * around + a, i1 = p * around + a2, i2 = (p + 1) * around + a, i3 = (p + 1) * around + a2;
                        t.Add(i0); t.Add(i2); t.Add(i1); t.Add(i1); t.Add(i2); t.Add(i3);
                    }
            }
            else
            {
                for (int p = 0; p < n - 1; p++)
                {
                    int b = v.Count;
                    for (int row = 0; row < 2; row++)
                        for (int a = 0; a < around; a++) { v.Add(Pt(p + row, a)); uv.Add(new Vector2(a / (float)around, (p + row) / (float)(n - 1))); }
                    for (int a = 0; a < around; a++)
                    {
                        int a2 = (a + 1) % around, i0 = b + a, i1 = b + a2, i2 = b + around + a, i3 = b + around + a2;
                        t.Add(i0); t.Add(i2); t.Add(i1); t.Add(i1); t.Add(i2); t.Add(i3);
                    }
                }
            }
            var m = new Mesh { name = name, indexFormat = v.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>Profile of an ellipsoid with the given radii, bottom to top.</summary>
        public static List<Vector2> Blob(float rx, float ry, int rings)
        {
            var p = new List<Vector2>();
            for (int i = 0; i <= rings; i++) { float phi = Mathf.PI * i / rings; p.Add(new Vector2(Mathf.Max(0.0005f, rx * Mathf.Sin(phi)), -ry * Mathf.Cos(phi))); }
            return p;
        }

        public static Mesh Ellipsoid(float rx, float ry, int around = 16, int rings = 10) => Lathe(Blob(rx, ry, rings), around, false, null, "Blob");

        /// <summary>Ring in the XZ plane (lies flat), centre radius R, tube radius r.</summary>
        public static Mesh Torus(float R, float r, int seg = 32, int sides = 10)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < sides; j++)
                {
                    float u = i * Mathf.PI * 2f / seg, w = j * Mathf.PI * 2f / sides;
                    v.Add(new Vector3((R + r * Mathf.Cos(w)) * Mathf.Cos(u), r * Mathf.Sin(w), (R + r * Mathf.Cos(w)) * Mathf.Sin(u)));
                }
            for (int i = 0; i < seg; i++)
                for (int j = 0; j < sides; j++)
                {
                    int i2 = (i + 1) % seg, j2 = (j + 1) % sides, a = i * sides + j, b = i2 * sides + j, c = i * sides + j2, d = i2 * sides + j2;
                    t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
                }
            var m = new Mesh { name = "Torus" };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// A waving pennant lying in the XY plane: pinned along x = 0, reaching `w` along +X and `h` up, free end swallow-tailed, ripples along X.
        /// Two sided. The pivot is the bottom of the pole edge.
        /// </summary>
        public static Mesh Banner(float w, float h, int cols = 14, int rows = 8, float wave = 0.12f)
        {
            var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
            for (int side = 0; side < 2; side++)
            {
                int o = v.Count;
                for (int j = 0; j <= rows; j++)
                {
                    float g = j / (float)rows, ext = w * (0.62f + 0.38f * Mathf.Abs(2f * g - 1f));
                    for (int i = 0; i <= cols; i++)
                    {
                        float f = i / (float)cols, x = f * ext;
                        float z = wave * Mathf.Sin(f * 7f + g * 1.6f) * f + 0.03f * Mathf.Sin(g * 5f) * f;
                        v.Add(new Vector3(x, g * h, z)); uv.Add(new Vector2(f, g));
                    }
                }
                for (int j = 0; j < rows; j++)
                    for (int i = 0; i < cols; i++)
                    {
                        int a = o + j * (cols + 1) + i, b = a + 1, c = a + cols + 1, d = c + 1;
                        if (side == 0) { t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d); }
                        else { t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c); }
                    }
            }
            var m = new Mesh { name = "Banner" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>A flat two-sided half disc in the XY plane (flat side down on y = 0), the arch's infill.</summary>
        public static Mesh HalfDisc(float radius, int segs = 24)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            v.Add(Vector3.zero);
            for (int i = 0; i <= segs; i++) { float a = Mathf.PI * i / segs; v.Add(new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0)); }
            for (int i = 0; i < segs; i++) { t.Add(0); t.Add(i + 2); t.Add(i + 1); t.Add(0); t.Add(i + 1); t.Add(i + 2); }
            var m = new Mesh { name = "HalfDisc" };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>One half of a round door: a half disc slab in the XY plane (centre at the origin), `thick` deep along Z; `left` = the x &lt;= 0 half.</summary>
        public static Mesh HalfSlab(float radius, float thick, int segs = 24, bool left = true)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c);
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) >= 0f) { t.Add(i); t.Add(i + 1); t.Add(i + 2); } else { t.Add(i); t.Add(i + 2); t.Add(i + 1); }
            }
            float hz = thick / 2f, a0 = left ? Mathf.PI / 2f : -Mathf.PI / 2f;
            Vector3 P(int k, float z) { float a = a0 + Mathf.PI * k / segs; return new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, z); }
            for (int k = 0; k < segs; k++)
            {
                Tri(new Vector3(0, 0, -hz), P(k, -hz), P(k + 1, -hz), Vector3.back);
                Tri(new Vector3(0, 0, hz), P(k, hz), P(k + 1, hz), Vector3.forward);
                var mid = (P(k, 0) + P(k + 1, 0)) * 0.5f;
                Tri(P(k, -hz), P(k + 1, -hz), P(k, hz), mid); Tri(P(k + 1, -hz), P(k + 1, hz), P(k, hz), mid);
            }
            var top = new Vector3(0, radius, 0); var bot = new Vector3(0, -radius, 0); var edge = left ? Vector3.right : Vector3.left;
            Tri(top + Vector3.back * hz, bot + Vector3.back * hz, top + Vector3.forward * hz, edge); Tri(bot + Vector3.back * hz, bot + Vector3.forward * hz, top + Vector3.forward * hz, edge);
            var m = new Mesh { name = "HalfSlab" };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>A two-sided rectangle in the XY plane, bottom centre at the origin.</summary>
        public static Mesh Panel(float w, float h)
        {
            var m = new Mesh { name = "Panel" };
            m.SetVertices(new[] { new Vector3(-w / 2, 0, 0), new Vector3(w / 2, 0, 0), new Vector3(-w / 2, h, 0), new Vector3(w / 2, h, 0) });
            m.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
            m.SetTriangles(new[] { 0, 2, 1, 1, 2, 3, 0, 1, 2, 1, 3, 2 }, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>
        /// A band swept along the upper half circle (angle a0..a1 in the XY plane, centre at the origin): a rectangular section from
        /// innerR to outerR, `depth` deep along Z. Flat faces, so the block edges stay crisp.
        /// </summary>
        public static Mesh ArcBand(float innerR, float outerR, float depth, int segs = 24, float a0 = 0f, float a1 = Mathf.PI)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                var n = Vector3.Cross(b - a, c - a);
                if (Vector3.Dot(n, outward) >= 0f) { t.Add(i); t.Add(i + 1); t.Add(i + 2); t.Add(i); t.Add(i + 2); t.Add(i + 3); }
                else { t.Add(i); t.Add(i + 2); t.Add(i + 1); t.Add(i); t.Add(i + 3); t.Add(i + 2); }
            }
            float hz = depth / 2f;
            for (int s = 0; s < segs; s++)
            {
                float u0 = Mathf.Lerp(a0, a1, s / (float)segs), u1 = Mathf.Lerp(a0, a1, (s + 1) / (float)segs), um = (u0 + u1) / 2f;
                Vector3 D(float a) => new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0);
                Vector3 oi0 = D(u0) * innerR, oi1 = D(u1) * innerR, oo0 = D(u0) * outerR, oo1 = D(u1) * outerR;
                Vector3 zf = new Vector3(0, 0, -hz), zb = new Vector3(0, 0, hz);
                Quad(oo0 + zf, oo1 + zf, oo1 + zb, oo0 + zb, D(um));                 // outer face
                Quad(oi0 + zf, oi1 + zf, oi1 + zb, oi0 + zb, -D(um));                // inner face
                Quad(oi0 + zf, oi1 + zf, oo1 + zf, oo0 + zf, Vector3.back);          // front
                Quad(oi0 + zb, oi1 + zb, oo1 + zb, oo0 + zb, Vector3.forward);       // back
            }
            var m = new Mesh { name = "ArcBand" };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>A tube along a polyline (vines, tendrils, handles); radius(0..1 along the line).</summary>
        public static Mesh Tube(IList<Vector3> pts, System.Func<float, float> radius, int sides = 6)
        {
            var v = new List<Vector3>(); var t = new List<int>();
            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                var tan = (pts[Mathf.Min(n - 1, i + 1)] - pts[Mathf.Max(0, i - 1)]).normalized;
                var side = Vector3.Cross(tan, Mathf.Abs(tan.y) > 0.95f ? Vector3.right : Vector3.up).normalized; var up = Vector3.Cross(side, tan);
                float r = radius(i / (float)(n - 1));
                for (int k = 0; k < sides; k++) { float a = k * Mathf.PI * 2f / sides; v.Add(pts[i] + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * r); }
            }
            for (int i = 0; i < n - 1; i++)
                for (int k = 0; k < sides; k++)
                {
                    int k2 = (k + 1) % sides, a = i * sides + k, b = i * sides + k2, c = (i + 1) * sides + k, d = (i + 1) * sides + k2;
                    t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
                }
            // make the winding face outwards whichever way the frame turned
            var n0 = Vector3.Cross(v[t[1]] - v[t[0]], v[t[2]] - v[t[0]]);
            var mid = (v[t[0]] + v[t[1]] + v[t[2]]) / 3f - pts[0];
            if (Vector3.Dot(n0, mid) < 0f) for (int i = 0; i < t.Count; i += 3) { int tmp = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = tmp; }
            var m = new Mesh { name = "Tube" };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>A ribbed oval melon, about 0.62 m long by 0.46 m across at scale 1 (lies on its side), with a stem nub on top and a flower scar underneath.</summary>
        public static Mesh Melon(int ribs = 10)
        {
            var prof = new List<Vector2>();
            const int rings = 18;
            for (int i = 0; i < rings; i++) { float phi = Mathf.PI * i / rings; prof.Add(new Vector2(i == 0 ? 0.0005f : 0.23f * Mathf.Sin(phi), -0.31f * Mathf.Cos(phi))); }
            prof.Add(new Vector2(0.05f, 0.305f)); prof.Add(new Vector2(0.03f, 0.315f)); prof.Add(new Vector2(0.026f, 0.37f)); prof.Add(new Vector2(0.0005f, 0.375f));   // stem nub
            var m = Lathe(prof, 40, false, (ang, p) => 1f + 0.05f * Mathf.Cos(ang * ribs) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(p / (float)(rings - 1))), "Melon");
            return m;
        }

        /// <summary>An inflated heart about 0.36 m tall (a Heart Seed pickup), centred on the origin: the classic heart outline revolved and flattened front to back.</summary>
        public static Mesh Heart()
        {
            const int tSteps = 36, vSteps = 48; var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
            for (int i = 0; i <= tSteps; i++)
            {
                float tt = Mathf.PI * i / tSteps;
                float hx = 16f * Mathf.Pow(Mathf.Sin(tt), 3f), hy = 13f * Mathf.Cos(tt) - 5f * Mathf.Cos(2f * tt) - 2f * Mathf.Cos(3f * tt) - Mathf.Cos(4f * tt);
                for (int k = 0; k < vSteps; k++)
                {
                    float a = k * Mathf.PI * 2f / vSteps;
                    v.Add(new Vector3(hx * Mathf.Cos(a), (hy + 6f), hx * Mathf.Sin(a) * 0.62f) * (0.36f / 24f));
                    uv.Add(new Vector2(k / (float)vSteps, i / (float)tSteps));
                }
            }
            for (int i = 0; i < tSteps; i++)
                for (int k = 0; k < vSteps; k++)
                {
                    int k2 = (k + 1) % vSteps, a = i * vSteps + k, b = i * vSteps + k2, c = (i + 1) * vSteps + k, d = (i + 1) * vSteps + k2;
                    t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c);
                }
            var m = new Mesh { name = "Heart" };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            // face outwards whichever way the outline ran
            var n = m.normals; var vv = m.vertices; float dot = 0f;
            for (int i = 0; i < vv.Length; i++) dot += Vector3.Dot(n[i], vv[i] - new Vector3(0f, 0.1f, 0f));
            if (dot < 0f) { for (int i = 0; i < t.Count; i += 3) { int tmp = t[i + 1]; t[i + 1] = t[i + 2]; t[i + 2] = tmp; } m.SetTriangles(t, 0); m.RecalculateNormals(); }
            m.RecalculateBounds();
            return m;
        }

        /// <summary>The Heat Seed pickup: a small ribbed melon, centred on the origin.</summary>
        public static Mesh MelonSeed()
        {
            var m = Melon(); var src = m.vertices; var v = new Vector3[src.Length];
            for (int i = 0; i < src.Length; i++) v[i] = Quaternion.Euler(90f, 0f, 0f) * (src[i] - new Vector3(0f, 0.03f, 0f)) * 0.55f;   // lying down, 0.35 m long
            m.vertices = v; m.RecalculateBounds(); m.RecalculateNormals();
            return m;
        }

        // ------------------------------------------------------------ the temple arch opening
        /// <summary>Half width of the clear opening of the arch model (AP4_A3_Door1) by height, measured over the whole model and inset by 0.04 m: straight sides, then a pointed top. (y, half width)</summary>
        static readonly float[,] OpeningTable = { { 0.02f, 1.45f }, { 2.55f, 1.45f }, { 2.85f, 1.42f }, { 3.15f, 1.36f }, { 3.45f, 1.25f }, { 3.75f, 1.05f }, { 4.05f, 0.76f }, { 4.35f, 0.27f }, { 4.43f, 0f } };

        /// <summary>Right half of the opening outline, floor to apex, as (x, y).</summary>
        public static List<Vector2> OpeningHalf(float grow = 0f)
        {
            var l = new List<Vector2>();
            for (int i = 0; i < OpeningTable.GetLength(0); i++) l.Add(new Vector2(Mathf.Max(0f, OpeningTable[i, 1] + (i == OpeningTable.GetLength(0) - 1 ? 0f : grow)), OpeningTable[i, 0] + (i == OpeningTable.GetLength(0) - 1 ? grow : 0f)));
            return l;
        }

        /// <summary>The clear opening filled in: a two-sided panel in the XY plane, floor centre at the origin. Nothing of the arch model reaches into it.</summary>
        public static Mesh OpeningPanel()
        {
            var h = OpeningHalf(); var v = new List<Vector3>(); var t = new List<int>();
            for (int i = 0; i < h.Count; i++) { v.Add(new Vector3(-h[i].x, h[i].y, 0)); v.Add(new Vector3(h[i].x, h[i].y, 0)); }
            for (int i = 0; i < h.Count - 1; i++)
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                t.Add(a); t.Add(c); t.Add(b); t.Add(b); t.Add(c); t.Add(d);
                t.Add(a); t.Add(b); t.Add(c); t.Add(b); t.Add(d); t.Add(c);
            }
            var m = new Mesh { name = "OpeningPanel" };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>A door leaf: one half of the opening (left = x &lt;= 0) extruded `thick` along Z, the centre seam at x = 0, floor at y = 0.</summary>
        public static Mesh OpeningSlab(bool left, float thick)
        {
            var h = OpeningHalf(); var poly = new List<Vector2> { new Vector2(0, h[0].y) };
            foreach (var p in h) poly.Add(new Vector2(left ? -p.x : p.x, p.y));
            if (poly[poly.Count - 1].x != 0f) poly.Add(new Vector2(0, h[h.Count - 1].y));
            var v = new List<Vector3>(); var t = new List<int>();
            void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c);
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), outward) >= 0f) { t.Add(i); t.Add(i + 1); t.Add(i + 2); } else { t.Add(i); t.Add(i + 2); t.Add(i + 1); }
            }
            float hz = thick / 2f; var cen = new Vector2(left ? -0.5f : 0.5f, 2.0f);
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i]; var b = poly[(i + 1) % poly.Count];
                Tri(new Vector3(cen.x, cen.y, -hz), new Vector3(a.x, a.y, -hz), new Vector3(b.x, b.y, -hz), Vector3.back);
                Tri(new Vector3(cen.x, cen.y, hz), new Vector3(a.x, a.y, hz), new Vector3(b.x, b.y, hz), Vector3.forward);
                var mid = (a + b) * 0.5f - cen; var outward = new Vector3(mid.x, mid.y, 0f);
                Tri(new Vector3(a.x, a.y, -hz), new Vector3(b.x, b.y, -hz), new Vector3(a.x, a.y, hz), outward);
                Tri(new Vector3(b.x, b.y, -hz), new Vector3(b.x, b.y, hz), new Vector3(a.x, a.y, hz), outward);
            }
            var m = new Mesh { name = "OpeningSlab" };
            m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        /// <summary>The opening outline as a polyline (left floor, up and over the apex, right floor), `grow` m outside the clear opening.</summary>
        public static List<Vector3> OpeningTrim(float grow, float z, bool halfOnly = false, bool left = false)
        {
            var h = OpeningHalf(grow); var pts = new List<Vector3>();
            if (halfOnly) { foreach (var p in h) pts.Add(new Vector3(left ? -p.x : p.x, p.y, z)); return pts; }
            for (int i = 0; i < h.Count; i++) pts.Add(new Vector3(-h[i].x, h[i].y, z));
            for (int i = h.Count - 2; i >= 0; i--) pts.Add(new Vector3(h[i].x, h[i].y, z));
            return pts;
        }

        public static GameObject Obj(string name, Mesh mesh, Material mat, Transform parent, Vector3 localPos, Vector3 euler, Vector3 scale)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos; g.transform.localRotation = Quaternion.Euler(euler); g.transform.localScale = scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return g;
        }
    }
}
