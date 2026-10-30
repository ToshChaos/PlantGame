using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.World
{
    /// <summary>Plants that live on and around ponds: lily pads (some flowering), reeds with cattail heads, and a scum of floating algae on murky water.
    /// Merged into a few static meshes per island, no colliders. Own random, so the island layout is unchanged.</summary>
    public static class WaterPlants
    {
        static Color H(string hex) => Overgrowth.H(hex);

        /// <param name="discs">x, y(unused), z, radius of the pond lobes in world space.</param>
        /// <summary>A round lily pad of radius `r` with a thin slit to the centre at angle `slit`: 24 segments, the rim curling up a touch, soft normals; both sides drawn.</summary>
        static void LilyPad(Overgrowth.Batch b, Vector3 c, float r, float slit)
        {
            const int N = 24; const float gap = 0.16f;   // radians either side of the slit
            int o = b.v.Count;
            b.v.Add(c + Vector3.up * 0.012f); b.n.Add(Vector3.up);
            for (int s = 0; s <= N; s++)
            {
                float a = slit + gap + s * (6.283f - 2f * gap) / N; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                b.v.Add(c + d * (r * 0.62f) + Vector3.up * 0.006f); b.n.Add(Vector3.up);
                b.v.Add(c + d * r + Vector3.up * 0.022f); b.n.Add((Vector3.up - d * 0.35f).normalized);   // the rim turns up
            }
            for (int s = 0; s < N; s++)
            {
                int i0 = o + 1 + s * 2, i1 = i0 + 2;   // inner ring at i, rim at i + 1
                b.t.AddRange(new[] { o, i1, i0, o, i0, i1 });
                b.t.AddRange(new[] { i0, i1, i0 + 1, i0 + 1, i1, i1 + 1, i0, i0 + 1, i1, i0 + 1, i1 + 1, i1 });
            }
        }

        /// <summary>A cattail head: a rounded capsule from `a` to `b` (radius `r`), 8 sides, normals out from its axis.</summary>
        static void Cattail(Overgrowth.Batch h, Vector3 a, Vector3 b, float r)
        {
            const int S = 8; float[] t = { 0f, 0.08f, 0.3f, 0.7f, 0.92f, 1f }; float[] k = { 0f, 0.7f, 1f, 1f, 0.7f, 0f };
            var ax = (b - a).normalized; var u = Vector3.Cross(ax, Mathf.Abs(ax.y) > 0.9f ? Vector3.right : Vector3.up).normalized; var w = Vector3.Cross(ax, u);
            int o = h.v.Count;
            for (int i = 0; i < t.Length; i++)
                for (int s = 0; s < S; s++)
                {
                    float ang = s * 6.283f / S; var d = u * Mathf.Cos(ang) + w * Mathf.Sin(ang);
                    h.v.Add(Vector3.Lerp(a, b, t[i]) + d * r * k[i]); h.n.Add((d + ax * (i < 2 ? -0.6f : i > 3 ? 0.6f : 0f)).normalized);
                }
            for (int i = 0; i < t.Length - 1; i++)
                for (int s = 0; s < S; s++)
                {
                    int p0 = o + i * S + s, p1 = o + i * S + (s + 1) % S, p2 = p0 + S, p3 = p1 + S;
                    h.t.AddRange(new[] { p0, p2, p1, p1, p2, p3, p0, p1, p2, p1, p3, p2 });   // both windings: the material draws one side
                }
        }

        public static void Build(Isle I, IslandTerrain terrain, List<Vector4> discs, bool murky, int seed, LevelTheme theme)
        {
            if (discs.Count == 0 || theme == LevelTheme.Snow) return;
            var rnd = new System.Random(seed * 2297 + 41);
            var root = new GameObject("WaterPlants"); root.transform.SetParent(I.go.transform, true); root.transform.position = Vector3.zero;
            float surface = I.centre.y + IslandTerrain.WaterLevel + 0.02f;
            float Ground(Vector2 p) => I.centre.y + terrain.HeightAtWorld(new Vector3(p.x, I.centre.y, p.y));
            var pads = new[] { new Overgrowth.Batch(), new Overgrowth.Batch() }; var flowers = new Overgrowth.Batch(); var stems = new Overgrowth.Batch(); var heads = new Overgrowth.Batch(); var scum = new Overgrowth.Batch();

            // a point inside a random lobe, kept away from the shore a little
            Vector2 InPond(float margin)
            {
                var d = discs[rnd.Next(discs.Count)]; float a = (float)(rnd.NextDouble() * 6.283), r = Mathf.Max(0f, d.w - margin) * Mathf.Sqrt((float)rnd.NextDouble());
                return new Vector2(d.x + Mathf.Cos(a) * r, d.z + Mathf.Sin(a) * r);
            }

            int nPads = Mathf.RoundToInt((murky ? 16 : 6) + discs.Count * (murky ? 2.5f : 1.2f));
            for (int k = 0; k < nPads; k++)
            {
                var p = InPond(0.35f); float r = 0.22f + (float)rnd.NextDouble() * 0.2f, yaw = (float)(rnd.NextDouble() * 6.283);
                var b = pads[rnd.Next(2)]; var c = new Vector3(p.x, surface, p.y); int notch = rnd.Next(10);
                LilyPad(b, c, r, yaw + notch * 0.628f);
                if (!murky && rnd.NextDouble() < 0.3)   // a flower on some pads
                {
                    for (int q = 0; q < 6; q++)
                    {
                        float a = q * 1.047f + yaw; var d = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                        flowers.Tri(c + d * 0.07f + Vector3.up * 0.02f, c - d * 0.07f * 0.0f + Vector3.up * 0.02f + new Vector3(-d.z, 0, d.x) * 0.04f, c + d * 0.13f + Vector3.up * 0.14f, Vector3.up);
                    }
                }
            }
            if (murky)   // algae scum: flat, ragged dark-green mats on the surface
                for (int k = 0, n = 6 + discs.Count * 2; k < n; k++)
                {
                    var p = InPond(0.2f); float r = 0.35f + (float)rnd.NextDouble() * 0.55f, ph = (float)(rnd.NextDouble() * 6.283); var c = new Vector3(p.x, surface - 0.005f, p.y);
                    for (int sIdx = 0, segs = 9; sIdx < segs; sIdx++)
                    {
                        float a0 = sIdx * 6.283f / segs, a1 = (sIdx + 1) * 6.283f / segs, r0 = r * (0.65f + 0.35f * Mathf.Sin(a0 * 3f + ph)), r1 = r * (0.65f + 0.35f * Mathf.Sin(a1 * 3f + ph));
                        scum.Tri(c, c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r0, c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r1, Vector3.up);
                    }
                }

            // reeds and cattails along the shore: tufts of tall thin blades, some topped with a brown head
            int nReeds = Mathf.RoundToInt(discs.Count * (murky ? 7f : 2.5f) + 4);   // clear ponds: a lighter fringe (was 4 per lobe + 6)
            for (int k = 0; k < nReeds; k++)
            {
                var d = discs[rnd.Next(discs.Count)]; float a = (float)(rnd.NextDouble() * 6.283);
                var p = new Vector2(d.x + Mathf.Cos(a) * d.w * (0.95f + (float)rnd.NextDouble() * 0.25f), d.z + Mathf.Sin(a) * d.w * (0.95f + (float)rnd.NextDouble() * 0.25f));
                if (Vector2.Distance(p, new Vector2(I.centre.x, I.centre.z)) > I.Edge(Mathf.Atan2(p.y - I.centre.z, p.x - I.centre.x) * Mathf.Rad2Deg) * 0.92f) continue;
                float gy = Mathf.Max(Ground(p), surface - 0.15f);
                for (int bl = 0, n = 3 + rnd.Next(4); bl < n; bl++)
                {
                    var bp = p + new Vector2((float)(rnd.NextDouble() - 0.5), (float)(rnd.NextDouble() - 0.5)) * 0.35f;
                    float h = 0.9f + (float)rnd.NextDouble() * 1.0f, lean = (float)(rnd.NextDouble() * 6.283), tilt = 0.12f + (float)rnd.NextDouble() * 0.2f, w = 0.025f;
                    var baseP = new Vector3(bp.x, gy - 0.05f, bp.y); var tip = baseP + new Vector3(Mathf.Cos(lean) * tilt * h, h, Mathf.Sin(lean) * tilt * h);
                    var side = new Vector3(-Mathf.Sin(lean), 0, Mathf.Cos(lean)) * w; var mid = Vector3.Lerp(baseP, tip, 0.55f) + new Vector3(Mathf.Cos(lean), 0, Mathf.Sin(lean)) * 0.03f;
                    stems.Quad(baseP - side, baseP + side, mid + side * 0.8f, mid - side * 0.8f, Vector3.up);
                    stems.Tri(mid - side * 0.8f, mid + side * 0.8f, tip, Vector3.up);
                    if (rnd.NextDouble() < 0.4)   // cattail head
                    {
                        Cattail(heads, Vector3.Lerp(mid, tip, 0.55f), Vector3.Lerp(mid, tip, 0.8f), 0.042f);
                    }
                }
            }
            Overgrowth.Emit(root.transform, "LilyPads", pads[0], Overgrowth.ColourMat(murky ? H("3E5A2A") : H("3F8A47")));
            Overgrowth.Emit(root.transform, "LilyPadsB", pads[1], Overgrowth.ColourMat(murky ? H("55703A") : H("5DA24F")));
            Overgrowth.Emit(root.transform, "LilyFlowers", flowers, Overgrowth.ColourMat(H("F4D6E6")));
            Overgrowth.Emit(root.transform, "Reeds", stems, Overgrowth.ColourMat(murky ? H("5C7A3A") : H("4E9A4E")));
            Overgrowth.Emit(root.transform, "Cattails", heads, Overgrowth.ColourMat(H("6B4A2B")));
            Overgrowth.Emit(root.transform, "Scum", scum, Overgrowth.ColourMat(H("3A5A22")));
        }
    }
}
