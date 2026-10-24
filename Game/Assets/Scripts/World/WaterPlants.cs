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
                var b = pads[rnd.Next(2)]; var c = new Vector3(p.x, surface, p.y); int segs = 10, notch = rnd.Next(segs);
                for (int sIdx = 0; sIdx < segs; sIdx++)
                {
                    if (sIdx == notch) continue;   // the wedge cut out of a lily pad
                    float a0 = yaw + sIdx * 6.283f / segs, a1 = yaw + (sIdx + 1) * 6.283f / segs;
                    b.Tri(c + Vector3.up * 0.02f, c + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * r, c + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * r, Vector3.up);
                }
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
            int nReeds = Mathf.RoundToInt(discs.Count * (murky ? 7 : 4) + 6);
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
                        var up = (tip - mid).normalized; var hb = Vector3.Lerp(mid, tip, 0.55f); var he = Vector3.Lerp(mid, tip, 0.8f); float hw = 0.045f;
                        var s2 = side.normalized * hw; var s3 = Vector3.Cross(up, s2.normalized) * hw;
                        heads.Quad(hb - s2, hb + s2, he + s2, he - s2, s3.normalized);
                        heads.Quad(hb - s3, hb + s3, he + s3, he - s3, s2.normalized);
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
