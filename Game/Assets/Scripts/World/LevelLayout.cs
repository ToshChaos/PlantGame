using System.Collections.Generic;
using UnityEngine;
using PlantGame.Plants;

namespace PlantGame.World
{
    public class Isle
    {
        public Vector3 centre; public float radius; public IslandShape shape; public GameObject go;
        public float Edge(float degrees) => shape.EdgeDeg(degrees);
        public Material waterMat; public Transform waterRoot;
        public bool murky; public Material murkyMat; public Transform murkyRoot;                                         // the pond is green swamp water: it carves a hollow and shows a surface but is no WaterSource
        public readonly List<Vector4> pondDiscs = new List<Vector4>();   // x, y(unused), z, radius of the pond lobes (water plants grow on them)
        public bool frozen;                                        // T50: the pond is iced over until a Heat Seed melts it
        public Vector2 slope;                                      // tilt: surface height gradient (m per m) in world x, z; 0 = level
        public int parent = -1; public float entryAngle;           // direction (deg, 0 = +X, + toward +Z) from this island toward its parent
        public readonly List<float> outHeadings = new List<float>();
        public readonly List<Vector4> keep = new List<Vector4>();  // x, z, tree clearance, ruin clearance
    }

    /// <summary>
    /// Positions of the islands and soil patches for a LevelPlan: radius, shape, heading and distance per gap, so each gap needs its plant
    /// (bridge gaps ~8 m, ladder rise 6.5 m, platform rise from its baked tip). Consumes `rnd` in a fixed order; the builder keeps using it.
    /// </summary>
    public class LevelLayout
    {
        public const float BaseRadius = 10.5f, LadderPadReach = 1.2f, LadderPadGap = 0f, LadderEdgeInset = 1.6f;   // T51: no top pad: the target rim lies LadderPadReach in front of the stalk top
        public const float BridgeAnchorInset = 1.8f, BridgeAnchorToEdge = 7.9f, Wobble = 0.55f;
        public const int LadderVariant = 3, PlatformVariant = 3;   // Plant_Ladder_03: 11 m; Plant_Platform_03: 3 pads, 3.6 m rise, ~5 m reach
        public const float GlideGap = 19f, GlideDrop = 8f;
        public const float LadderHeight = 11f, PadMaxRadius = 1.4f, PlatformGap = 1.0f;

        public readonly List<Isle> isles = new List<Isle>();
        public readonly List<float> heads = new List<float>(), soilFwds = new List<float>();
        /// <summary>Per link: bridge length the plant is stretched to (0 for other kinds) and the height of its tip above the soil.</summary>
        public readonly List<float> reaches = new List<float>(), rises = new List<float>();
        readonly List<Vector3> bridgeFrom = new List<Vector3>(), bridgeTo = new List<Vector3>();   // per link: start (soil) and end (target island centre) of a bridge, zero for other kinds
        public const float MinBridgeHeadingApart = 50f, MinBridgeSegmentApart = 7f;
        public const float MaxBridgeRise = 3.4f;   // the bridge tilts up or down by the height difference to the next island (gentle: about 20 degrees at most)
        public const float MaxTiltDegrees = 15f, MinBridgeGap = 8.6f, MaxBridgeGap = 11.6f, PlatformGapWidth = 4.5f, MinIslandGap = 8.5f;
        public readonly List<string> problems = new List<string>();

        public static Vector3 Dir(float deg) => Quaternion.Euler(0, -deg, 0) * Vector3.right;

        public static LevelLayout Compute(LevelPlan plan, System.Random rnd)
        {
            var lay = new LevelLayout();
            var isles = lay.isles;
            isles.Add(new Isle { centre = Vector3.zero, radius = BaseRadius * 1.1f, shape = IslandShape.Random(rnd, BaseRadius * 1.1f, Wobble) });
            for (int i = 0; i < plan.links.Count; i++)
            {
                var L = plan.links[i];
                float h0 = L.heading + ((float)rnd.NextDouble() - 0.5f) * 20f;    // +/-10 deg
                var from = isles[L.from];
                float toR = BaseRadius * (0.85f + (float)rnd.NextDouble() * 0.4f);   // 8.9 .. 13 m
                if (i == plan.MainLinks - 1) toR = BaseRadius * 1.0f;
                if (plan.bigIsland == isles.Count) toR = BaseRadius * 1.9f;
                var toShape = IslandShape.Random(rnd, toR, Wobble);
                // random draws for this gap are made once, so trying other headings below does not change what the rest of the layout draws
                bool draws = L.kind == PlantKind.Glider || (L.kind != PlantKind.Ladder && L.kind != PlantKind.Platform);   // same draws as before, so existing layouts keep their shapes
                float jit1 = draws ? (float)rnd.NextDouble() : 0.5f, jit2 = draws ? (float)rnd.NextDouble() : 0f;

                // heading candidates: the planned one first, then fanned out to both sides until the new island clears every earlier one
                Isle best = null, first = null; float bestH = h0, bestSoil = 0f, firstH = h0, firstSoil = 0f, bestReach = 0f, firstReach = 0f, bestRise = 0f, firstRise = 0f;
                var slopeTo = TiltFor(plan, isles.Count);
                for (int t = 0; t < 17 && best == null; t++)
                {
                    float h = h0 + (t == 0 ? 0f : (t % 2 == 1 ? 1f : -1f) * ((t + 1) / 2) * 12f);
                    var dir = Dir(h);
                    float soilFwd, dist, dy = 0f, reach = 0f;
                    float fromE = from.Edge(h), toE = toShape.EdgeDeg(h + 180f);   // rim distances along the heading (islands are not circles)
                    if (L.kind == PlantKind.Glider)
                    {
                        // a wide gap (jumping alone cannot do it) down to a much lower island: run off the edge, press E with a leaf
                        soilFwd = 0f; dist = fromE + toE + GlideGap + (jit1 - 0.5f) * 3f; dy = -GlideDrop - jit2 * 2f;
                    }
                    else if (L.kind == PlantKind.Ladder)
                    {
                        soilFwd = fromE - LadderEdgeInset;
                        dist = soilFwd + LadderPadReach + LadderPadGap + toE; dy = LadderHeight;
                    }
                    else if (L.kind == PlantKind.Platform)
                    {
                        PadTip(PlantKind.Platform, PlatformVariant, out float hx, out float ty);
                        dist = fromE + toE + PlatformGapWidth; dy = ty;   // 4.5 m: too wide to jump even with the pads' 3.6 m rise
                        soilFwd = dist - toE - hx - PadMaxRadius - PlatformGap;
                    }
                    else
                    {
                        // gaps too wide to jump (8.6 .. 11.6 m between the rims): the bridge plant is stretched to the length needed
                        soilFwd = fromE - BridgeAnchorInset;
                        reach = MinBridgeGap + (MaxBridgeGap - MinBridgeGap) * jit1 + BridgeAnchorInset;
                        dist = soilFwd + reach + toE;
                        dy = -3.2f + jit2 * 4.4f;   // the far end lies 3.2 m lower .. 1.2 m higher (the bridge tilts to meet the far island, up to MaxBridgeRise)
                    }
                    // a tilted island: both ends of the gap are measured from the real surface (soil on the tilted start island, landing rim on the tilted target)
                    var flatDir = new Vector2(dir.x, dir.z);
                    float soilH = Vector2.Dot(from.slope, flatDir * soilFwd), landH = Vector2.Dot(slopeTo, -flatDir * toE);
                    var cand = new Isle { centre = from.centre + dir * dist + Vector3.up * (soilH + dy - landH), radius = toR, shape = toShape, parent = L.from, entryAngle = h + 180f, slope = slopeTo };
                    if (Clear(cand, isles, L.from) && (L.kind != PlantKind.Bridge || lay.BridgeApart(plan, i, h, from.centre + dir * soilFwd, cand.centre))) { best = cand; bestH = h; bestSoil = soilFwd; bestReach = reach; bestRise = Mathf.Clamp(dy, -MaxBridgeRise, MaxBridgeRise); }
                    else if (t == 0) { first = cand; firstH = h; firstSoil = soilFwd; firstReach = reach; firstRise = Mathf.Clamp(dy, -MaxBridgeRise, MaxBridgeRise); }
                }
                if (best == null) { best = first; bestH = firstH; bestSoil = firstSoil; bestReach = firstReach; bestRise = firstRise; lay.problems.Add($"Island {isles.Count} could not be placed clear of the others."); }
                lay.heads.Add(bestH);
                from.outHeadings.Add(bestH);
                isles.Add(best);
                lay.soilFwds.Add(bestSoil); lay.reaches.Add(bestReach); lay.rises.Add(bestRise);
                lay.bridgeFrom.Add(L.kind == PlantKind.Bridge ? from.centre + Dir(bestH) * bestSoil : Vector3.zero); lay.bridgeTo.Add(L.kind == PlantKind.Bridge ? best.centre : Vector3.zero);
            }
            isles[0].entryAngle = 180f + lay.heads[0];   // start island: spawn sits behind the first gap

            return lay;
        }

        /// <summary>Two vine bridges must not lie close together: from one island they leave in clearly different directions, and bridges elsewhere keep their distance.</summary>
        bool BridgeApart(LevelPlan plan, int link, float heading, Vector3 a, Vector3 b)
        {
            for (int j = 0; j < link && j < bridgeFrom.Count; j++)
            {
                if (plan.links[j].kind != PlantKind.Bridge) continue;
                if (plan.links[j].from == plan.links[link].from) { if (Mathf.Abs(Mathf.DeltaAngle(heading, heads[j])) < MinBridgeHeadingApart) return false; continue; }
                if (j + 1 == plan.links[link].from) continue;   // the previous bridge ends on this very island
                if (SegmentDistance(a, b, bridgeFrom[j], bridgeTo[j]) < MinBridgeSegmentApart) return false;
            }
            return true;
        }

        static float SegmentDistance(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
        {
            Vector2 a = new Vector2(p1.x, p1.z), b = new Vector2(q1.x, q1.z), c = new Vector2(p2.x, p2.z), d = new Vector2(q2.x, q2.z);
            float best = float.MaxValue;
            for (int i = 0; i <= 8; i++) { var p = Vector2.Lerp(a, b, i / 8f); for (int j = 0; j <= 8; j++) best = Mathf.Min(best, Vector2.Distance(p, Vector2.Lerp(c, d, j / 8f))); }
            return best;
        }

        /// <summary>A second place to plant for a gap: the soil position and the direction its plant grows.</summary>
        public struct AltSoil { public Vector3 pos, dir; public float reach, rise; }

        /// <summary>
        /// Alternative soil patches for link k, on the same island, a little along the rim from the preferred one. The bigger the island the
        /// more there are (0..3). Each aims at the rim point the preferred plant lands on and must keep about the same reach, so the same
        /// plant still gets across; `keep` (scenery clearances already placed on that island) is respected. Own random: the layout is unchanged.
        /// </summary>
        public List<AltSoil> Alternatives(LevelPlan plan, int k, List<Vector4> keep)
        {
            var res = new List<AltSoil>();
            var L = plan.links[k];
            if (L.kind == PlantKind.Glider) return res;
            var from = isles[L.from]; var to = isles[k + 1];
            float h = heads[k]; var dir0 = Dir(h);
            float inset = from.Edge(h) - soilFwds[k];
            var p0 = from.centre + dir0 * soilFwds[k];
            float e0 = RimDistance(p0, dir0, to);
            if (e0 < 0f) return res;
            var target = p0 + dir0 * e0;
            int want = Mathf.Clamp(Mathf.FloorToInt((from.radius - 8.9f) / 1.4f), 0, 3);
            float lo = L.kind == PlantKind.Bridge ? 1.5f : L.kind == PlantKind.Ladder ? 0.5f : 0.8f;   // shorter reach is fine (the plant lands further in), longer is not
            float hi = L.kind == PlantKind.Bridge ? 0.2f : L.kind == PlantKind.Ladder ? 0.4f : 0.5f;
            var rnd = new System.Random(unchecked(plan.layoutSeed * 131 + k * 7919 + 17));
            var taken = new List<Vector3> { p0 };
            for (int t = 0; t < 60 && res.Count < want; t++)
            {
                float a = h + (rnd.Next(2) == 0 ? -1f : 1f) * (14f + (float)rnd.NextDouble() * 46f);
                var q = from.centre + Dir(a) * (from.Edge(a) - inset);
                bool ok = Mathf.Abs(Mathf.DeltaAngle(a, from.entryAngle)) > 30f;
                foreach (var oh in from.outHeadings) if (Mathf.Abs(Mathf.DeltaAngle(a, oh)) < 12f) ok = false;
                foreach (var o in taken) if (Flat(o - q) < 3.2f) ok = false;
                foreach (var kp in keep) if (Vector2.Distance(new Vector2(q.x, q.z), new Vector2(kp.x, kp.y)) < kp.z * 0.8f) ok = false;
                if (!ok) continue;
                var d = target - q; d.y = 0f; float e = d.magnitude;
                if (e < e0 - lo || e > e0 + hi) continue;
                var dir = d / e;
                float r = RimDistance(q, dir, to);
                if (r < 0f || Mathf.Abs(r - e) > 1f) continue;
                if (Mathf.Abs(Vector2.Dot(from.slope, new Vector2(q.x - p0.x, q.z - p0.z))) > (L.kind == PlantKind.Bridge ? 1f : 0.4f)) continue;   // tilted island: a spot much lower or higher than the preferred one would change the reach
                float dh = Vector2.Dot(from.slope, new Vector2(q.x - p0.x, q.z - p0.z));
                res.Add(new AltSoil { pos = q, dir = dir, reach = reaches[k] > 0f ? reaches[k] + (e - e0) : 0f, rise = Mathf.Clamp(rises[k] - dh, -MaxBridgeRise, MaxBridgeRise) }); taken.Add(q);
            }
            return res;
        }

        static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        /// <summary>Distance from `origin` along the flat direction `dir` to the rim of `isle` (-1 when the ray never reaches it).</summary>
        static float RimDistance(Vector3 origin, Vector3 dir, Isle isle)
        {
            for (float s = 0f; s < 40f; s += 0.1f)
            {
                var rel = origin + dir * s - isle.centre;
                if (new Vector2(rel.x, rel.z).magnitude < isle.shape.Edge(Mathf.Atan2(rel.z, rel.x))) return s;
            }
            return -1f;
        }

        const float FarApart = 24f;   // islands this far apart in height cannot be jumped between whatever the gap

        /// <summary>
        /// True when `c` keeps at least MinIslandGap (8.5 m, more than a running jump) between its outline and every earlier island except its parent,
        /// so you can never jump from one island to another by accident; the gap to the parent is what the plant bridges.
        /// </summary>
        static bool Clear(Isle c, List<Isle> isles, int parent)
        {
            for (int j = 0; j < isles.Count; j++)
            {
                if (j == parent) continue;
                if (Mathf.Abs(c.centre.y - isles[j].centre.y) >= FarApart) continue;
                if (Inside(c, isles[j], MinIslandGap) || Inside(isles[j], c, MinIslandGap)) return false;
            }
            return true;
        }

        /// <summary>Tilt of island `index` (0 = level): some islands lean up to MaxTiltDegrees. Never the start or goal island, nor one with a pond (water stays flat).</summary>
        static Vector2 TiltFor(LevelPlan plan, int index)
        {
            if (index == 0 || index == plan.GoalIsland(plan.links.Count + 1) || plan.waterIslands.Contains(index) || plan.roundTrip) return Vector2.zero;
            var r = new System.Random(unchecked(plan.layoutSeed * 7 + index * 131 + 5));
            if (r.NextDouble() > 0.4) return Vector2.zero;
            float deg = 4f + (float)r.NextDouble() * (MaxTiltDegrees - 4f), a = (float)r.NextDouble() * Mathf.PI * 2f;
            float tan = Mathf.Tan(deg * Mathf.Deg2Rad);
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * tan;
        }

        /// <summary>True when the rim of `a` comes within `margin` of the top of `b`.</summary>
        static bool Inside(Isle a, Isle b, float margin)
        {
            for (int i = 0; i < 72; i++)
            {
                float t = i * Mathf.PI * 2f / 72f;
                var p = a.centre + new Vector3(Mathf.Cos(t), 0, Mathf.Sin(t)) * a.shape.Edge(t) - b.centre;
                if (new Vector2(p.x, p.z).magnitude < b.shape.Edge(Mathf.Atan2(p.z, p.x)) + margin) return true;
            }
            return false;
        }

        /// <summary>Horizontal distance anchor -> Tip and Tip height of a baked plant prefab (read from the prefab asset, nothing is instantiated).</summary>
        public static void PadTip(PlantKind kind, int variant, out float horizontal, out float height)
        {
            horizontal = 5f; height = 3.6f;   // Plant_Platform_03 if the kit is missing
            var prefabs = Kit.Plants(kind);
            if (prefabs == null || variant - 1 >= prefabs.Length || prefabs[variant - 1] == null) return;
            Transform tip = null, anchor = null;
            foreach (var t in prefabs[variant - 1].GetComponentsInChildren<Transform>(true)) { if (t.name == "Tip") tip = t; else if (t.name == "Anchor") anchor = t; }
            if (tip == null || anchor == null) return;
            var d = tip.position - anchor.position;
            horizontal = new Vector2(d.x, d.z).magnitude; height = d.y;
        }
    }
}
