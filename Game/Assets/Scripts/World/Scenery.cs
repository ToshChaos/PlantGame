using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;
using PlantGame.World;

namespace PlantGame.World
{
    /// <summary>
    /// Art pass inspired by the reference screenshot: lush trees, bushes and flowers around each island rim, white carved-stone
    /// ruins (pillars, broken pillars, a glowing portal arch) and floating rocks / islets in the sky. All primitives + the shared
    /// materials; positions come from System.Random so a rebuild is identical.
    /// </summary>
    public static class Scenery
    {
        /// <summary>Set by the world generator before Decorate: autumn swaps the greens for yellows, oranges and reds.</summary>
        public static LevelTheme Theme = LevelTheme.Sunny;

        /// <summary>Kit material by name, re-coloured for the current theme (foliage, blossom and island tops).</summary>
        /// <summary>Well-spread 0..1 value from two ints (the first draw of System.Random with nearby seeds is nearly the same, so it is not used for per-island coin flips).</summary>
        public static float Hash01(int a, int salt) { unchecked { uint h = (uint)a * 2654435761u ^ (uint)salt * 40503u; h ^= h >> 15; h *= 2246822519u; h ^= h >> 13; return (h & 0xFFFFu) / 65535f; } }

        static List<Vector4> _leafTrees;   // x, y, z, height of the leaf trees of the island being decorated (T39 falling leaves)

        static readonly Dictionary<string, Material> _snowFoliage = new Dictionary<string, Material>();
        /// <summary>Snow levels: green plant parts turn frosted white (dark foliage a frosty grey-brown) instead of staying green.</summary>
        static Material SnowFoliage(string name)
        {
            if (_snowFoliage.TryGetValue(name, out var m) && m != null) return m;
            var src = Kit.Mat(name) ?? Kit.Mat("M_Foliage");
            if (src == null) return null;
            Color c = name == "M_FoliageLight" ? new Color(0.96f, 0.98f, 1f) : name == "M_Foliage" ? new Color(0.86f, 0.92f, 0.93f) : new Color(0.58f, 0.56f, 0.52f);
            m = new Material(src) { name = name + "_Snow", hideFlags = HideFlags.DontSave };
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", null);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return _snowFoliage[name] = m;
        }

        public static Material Mat(string name)
        {
            if (Theme == LevelTheme.Autumn)
                switch (name)
                {
                    case "M_Foliage": name = "M_Autumn"; break;
                    case "M_FoliageLight": name = "M_AutumnGold"; break;
                    case "M_FoliageDark": case "M_Blossom": name = "M_AutumnRed"; break;
                    case "M_Terrain": name = "M_TerrainAutumn"; break;
                }
            if (Theme == LevelTheme.Snow && (name == "M_Foliage" || name == "M_FoliageLight" || name == "M_FoliageDark" || name == "M_Blossom")) return SnowFoliage(name);
            return Kit.Mat(name) ?? Kit.Mat(name == "M_TerrainAutumn" ? "M_Terrain" : "M_Foliage");
        }

        static Material _marble, _gold, _glow, _pink, _purple, _white, _yellow, _birch, _autumn, _berryRed, _berryBlue, _bean;

        static void EnsureMaterials()
        {
            if (_marble != null) return;
            _marble = Kit.Mat("M_Marble"); _gold = Kit.Mat("M_RuneGold"); _glow = Kit.Mat("M_PortalGlow");
            _pink = Kit.Mat("M_FlowerPink"); _purple = Kit.Mat("M_FlowerPurple"); _white = Kit.Mat("M_FlowerWhite"); _yellow = Kit.Mat("M_FlowerYellow");
            _birch = Kit.Mat("M_BirchBark");  _autumn = Kit.Mat("M_Autumn"); 
            _berryRed = Kit.Mat("M_BerryRed"); _berryBlue = Kit.Mat("M_BerryBlue"); _bean = Kit.Mat("M_BeanPod");
        }

        // ------------------------------------------------------------------ islands
        public struct Lane { public Vector2 origin, dir; public float length, halfWidth; }

        /// <summary>
        /// Decorates one island of radius R: ruins and trees / bushes / flowers on the outer part, clear of the lanes (outgoing plant
        /// corridors, the spawn side) and of the `keep` points (x, z, tree clearance, ruin clearance): seeds, water, soils, spawn.
        /// </summary>
        /// <summary>Island index that gets one huge tree (set by the generator from the level plan, -1 for none).</summary>
        public static int HugeTreeIsland = -1;

        public static void Decorate(Transform isle, Vector3 centre, float R, System.Func<float, float> edge, int index, bool first, bool goal, List<Vector4> keep, List<Lane> lanes, float vegetation = 1f, int boulders = 0, bool water = false)
        {
            EnsureMaterials();
            var rnd = new System.Random(index * 977 + 31);
            var root = new GameObject("Scenery"); root.transform.SetParent(isle, true); root.transform.position = centre;
            var c2 = new Vector2(centre.x, centre.z);
            var placed = new List<Vector3>();   // x, z, clearance used for later pieces

            bool Free(Vector2 p, float clear, bool ruin)
            {
                foreach (var l in lanes)
                {
                    var rel = p - l.origin; float along = Vector2.Dot(rel, l.dir);
                    float perp = Mathf.Abs(rel.x * l.dir.y - rel.y * l.dir.x);
                    if (along > -clear && along < l.length + clear && perp < l.halfWidth + clear * 0.5f) return false;
                }
                foreach (var k in keep) if (Vector2.Distance(p, new Vector2(k.x, k.y)) < (ruin ? k.w : k.z)) return false;
                foreach (var q in placed) if (Vector2.Distance(p, new Vector2(q.x, q.y)) < q.z + clear * 0.5f) return false;
                return true;
            }
            Vector3 World(Vector2 p) => new Vector3(p.x, centre.y, p.y);

            var ruinSpots = new List<Vector3>();
            ArchSpots.Clear();
            _leafTrees = new List<Vector4>();
            // one ruin per island (an arch on the first and the goal island): try angles until one is clear
            float a0 = (float)rnd.NextDouble() * 360f;
            for (int t = 0; t < 36; t++)
            {
                float a = (a0 + t * 10f) * Mathf.Deg2Rad;
                var p = c2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * edge(a) * 0.76f;
                if (!Free(p, first || goal ? 5f : 3.5f, true)) continue;
                var pos = World(p); ruinSpots.Add(pos);
                if (first || goal) { Arch(root.transform, pos, Quaternion.LookRotation(new Vector3(c2.x - p.x, 0, c2.y - p.y).normalized)); placed.Add(new Vector3(p.x, p.y, 4.5f)); }
                else
                {
                    var side = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
                    Pillar(root.transform, pos, 3.6f + (float)rnd.NextDouble() * 1.4f, false, rnd);
                    Pillar(root.transform, pos + side * 1.6f, 1.4f, true, rnd);
                    Pillar(root.transform, pos - side * 1.5f + (centre - pos).normalized * 0.8f, 2.2f, true, rnd);
                    Slabs(root.transform, pos + (centre - pos).normalized * 2f, rnd);
                    placed.Add(new Vector3(p.x, p.y, 3f));
                }
                break;
            }

            // flowering bush with butterflies on some islands (own random so the layout stays; not in snow, fog or rain)
            if (Theme != LevelTheme.Snow && Theme != LevelTheme.Fog && Theme != LevelTheme.Rain && vegetation > 0.5f && Hash01(index, 449) < 0.5)
            {
                var br = new System.Random(index * 449 + 11);
                for (int t = 0; t < 80; t++)
                {
                    float a = (float)(br.NextDouble() * Mathf.PI * 2), r = edge(a) * (0.3f + (float)br.NextDouble() * 0.5f);
                    var p = c2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    if (!Free(p, 2.5f, false)) continue;
                    var bushModel = Model(root.transform, FirstAvailable(br, "Bush_Common_Flowers", "Bush_Common", "plant_bush"), World(p), (float)br.NextDouble() * 360f, 1.3f, false);
                    ButterflyBush.Build(root.transform, World(p), bushModel, br);
                    placed.Add(new Vector3(p.x, p.y, 1.8f));
                    break;
                }
            }
            var weights = new float[TreeKinds];   // each island leans toward a few species
            for (int k = 0; k < TreeKinds; k++) { double w = rnd.NextDouble(); weights[k] = 0.15f + (float)(w * w * w) * 3f + (k == 6 ? 0.5f : 0f); }
            weights[7] = Theme == LevelTheme.Snow || Theme == LevelTheme.Fog ? 0f : Theme == LevelTheme.Rain || Theme == LevelTheme.Autumn ? 0.2f : 0.9f + (float)rnd.NextDouble();   // palms: mostly sunny, sunset and sunrise levels
            float area = (R * R) / 64f;
            int Count(float perUnit) => Mathf.RoundToInt(perUnit * area * vegetation);
            // big trees (about 2x the usual height) only grow where the island has water
            int bigTrees = water && vegetation > 0.5f && rnd.NextDouble() < 0.65 ? 1 + (area > 1.8f && rnd.NextDouble() < 0.4 ? 1 : 0) : 0;
            var bigTreeSpots = new List<Vector3>();   // x, z, height: thick roots hang from the underside below them (SideDecor)
            if (bigTrees > 0) Scatter(bigTrees, 80, 0.45f, 0.85f, 8f, 6f, p => { float h = 12f + (float)rnd.NextDouble() * 3f; Tree(root.transform, World(p), h, rnd, PickKind(weights, rnd)); bigTreeSpots.Add(new Vector3(p.x, p.y, h)); }, 6f);   // T38: normal trees are 2x, big ones stay above them
            if (index == HugeTreeIsland) Scatter(1, 120, 0.4f, 0.8f, 10f, 8f, p => { Tree(root.transform, World(p), 20f, rnd, PickKind(weights, rnd)); bigTreeSpots.Add(new Vector3(p.x, p.y, 20f)); }, 8f);   // one tree three times a normal one
            Scatter(Count(14), 160, 0.6f, 0.92f, 4.5f, 3.2f, p => Tree(root.transform, World(p), 4.8f + (float)rnd.NextDouble() * 4.0f, rnd, PickKind(weights, rnd)), 3.2f);
            Scatter(Count(10), 120, 0.35f, 0.92f, 1.8f, 1.2f, p => Bush(root.transform, World(p), 0.7f + (float)rnd.NextDouble() * 0.7f, rnd), 1.2f);
            Scatter(Mathf.RoundToInt(Count(18) * (vegetation < 0.3f ? 0.5f : 1f)), 220, 0.25f, 0.92f, 1.3f, 0.9f, p => Flowers(root.transform, World(p), rnd), 0.7f);

            // T39: ferns and tall grass fill the gaps (own picks so the trees and bushes above keep their layout)
            if (Kit.Prop("Fern_1") != null || Kit.Prop("grass_large") != null)
                Scatter(Count(12), 200, 0.2f, 0.94f, 1.0f, 0.8f, p => { var m = Model(root.transform, FirstAvailable(rnd, "Fern_1", "grass_large", "grass"), World(p), (float)rnd.NextDouble() * 360f, 0.5f + (float)rnd.NextDouble() * 0.7f, false); }, 0.5f);

            if (boulders > 0) Scatter(boulders + Count(2), 120, 0.3f, 0.9f, 2.6f, 1.8f, p => Slabs(root.transform, World(p), rnd), 1.8f);

            {
                var trees = new List<Vector3>(); foreach (var q in placed) trees.Add(q);
                bool FreeLanes(Vector2 p, float clear)
                {
                    foreach (var l in lanes)
                    {
                        var rel = p - l.origin; float along = Vector2.Dot(rel, l.dir);
                        float perp = Mathf.Abs(rel.x * l.dir.y - rel.y * l.dir.x);
                        if (along > -clear && along < l.length + clear && perp < l.halfWidth + clear * 0.5f) return false;
                    }
                    foreach (var k in keep) if (Vector2.Distance(p, new Vector2(k.x, k.y)) < k.z * 0.6f) return false;
                    return true;
                }
                Overgrowth.Apply(isle, centre, index, edge, FreeLanes, trees, ruinSpots, vegetation, Theme, vegetation >= 1f && Theme != LevelTheme.Snow, vegetation >= 1f ? GroundKind.Grass : vegetation > 0.3f ? GroundKind.Mud : GroundKind.Rock);
                ShinyBits(isle, centre, edge, index, ruinSpots, FreeLanes, vegetation >= 1f ? GroundKind.Grass : vegetation > 0.3f ? GroundKind.Mud : GroundKind.Rock);
                var near = new List<Vector4>();   // leaf trees within 6 m of the rim may shed a leaf over the edge now and then
                foreach (var lt in _leafTrees) { var rel = new Vector2(lt.x - c2.x, lt.z - c2.y); if (edge(Mathf.Atan2(rel.y, rel.x)) - rel.magnitude < 6f) near.Add(lt); }
                if (near.Count > 0 && Hash01(index, 131) < 0.7) FallingBits.AddLeaves(isle, centre, near, Overgrowth.LeafColour(Theme, index));
                // T43: sounds that match their source: wind in the leaves of leafy islands, whispers and drones near some ruins
                if (_leafTrees.Count >= 3)
                {
                    Vector3 cen = Vector3.zero; foreach (var lt in _leafTrees) cen += new Vector3(lt.x, lt.y + lt.w * 0.7f, lt.z); cen /= _leafTrees.Count;
                    var lg = new GameObject("LeafRustle"); lg.transform.SetParent(isle, true); lg.transform.position = cen;
                    var le = lg.AddComponent<Audio.LoopEmitter>(); le.clipName = "leaf_rustle"; le.volume = 0.3f; le.maxDistance = 20f;
                }
                if (ruinSpots.Count > 0 && Hash01(index, 211) < 0.6)
                {
                    var rg = new GameObject("RuinSounds"); rg.transform.SetParent(isle, true); rg.transform.position = ruinSpots[0] + Vector3.up * 1.5f;
                    rg.AddComponent<Audio.RuinSounds>();
                }
                SideDecor(isle, centre, index, vegetation, edge(0f), bigTreeSpots);
                _leafTrees = null;
            }

            void Scatter(int want, int tries, float rMin, float rMax, float clear, float treeClear, System.Action<Vector2> make, float mark)
            {
                int n = 0;
                for (int t = 0; t < tries && n < want; t++)
                {
                    float a = (float)(rnd.NextDouble() * Mathf.PI * 2), r = edge(a) * (rMin + (float)rnd.NextDouble() * (rMax - rMin));
                    var p = c2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    if (!Free(p, clear, false)) continue;
                    make(p); placed.Add(new Vector3(p.x, p.y, mark)); n++;
                }
            }
        }


        // ------------------------------------------------------------------ island sides (T39 follow-up)
        static readonly float[] WallScale = { 1f, 0.997f, 0.97f, 0.85f, 0.64f, 0.38f, 0f };

        /// <summary>Point on the island's side wall at angle `a` and local height `y` (negative below the top), slightly buried; also the outward normal.</summary>
        static Vector3 WallPoint(IslandTerrain terrain, Vector3 centre, float a, float y, float depth, out Vector3 outward, float bury = 0.12f)
        {
            float[] yy = { IslandTerrain.Shoulder, -2.4f, -depth * 0.3f, -depth * 0.5f, -depth * 0.72f, -depth * 0.9f, -depth };
            float sc = 0f;
            for (int i = 0; i < yy.Length - 1; i++)
                if (y <= yy[i] && y >= yy[i + 1]) { sc = Mathf.Lerp(WallScale[i], WallScale[i + 1], Mathf.InverseLerp(yy[i], yy[i + 1], y)); break; }
            outward = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            float r = terrain.Rim(a) * sc * (1f - 0.04f) - bury;
            return new Vector3(centre.x + outward.x * r, centre.y + y, centre.z + outward.z * r);
        }

        /// <summary>
        /// Things growing and sticking out of the side of an island: rock protrusions, ferns / grass / bushes reaching sideways, and (on some
        /// islands) clusters of flowers. Own random, no colliders. Lives under a "SideDecor" child that the terrain Lift skips.
        /// </summary>
        public static void SideDecor(Transform isle, Vector3 centre, int index, float lush, float rimRadius, List<Vector3> bigTrees = null)
        {
            var terrain = isle.GetComponent<IslandTerrain>(); if (terrain == null) return;
            EnsureMaterials();
            var rnd = new System.Random(index * 6151 + 71);
            var root = new GameObject("SideDecor"); root.transform.SetParent(isle, true); root.transform.position = centre;
            float size = Mathf.Max(1f, rimRadius / 10f);
            float DepthY() { double u = rnd.NextDouble(); return -0.5f - (float)(u * u) * (terrain.depth * 0.6f); }   // more near the top
            void Orient(GameObject g, Vector3 outward, float tilt)
            {
                var up = (outward * tilt + Vector3.up * (1f - tilt)).normalized;
                g.transform.rotation = Quaternion.FromToRotation(Vector3.up, up) * g.transform.rotation;
            }
            // rock protrusions
            string[] rocks = { "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3", "rock_smallA", "rock_smallB", "stone_smallC" };
            int nr = Mathf.RoundToInt((7 + rnd.Next(7)) * size * Mathf.Min(1f, rimRadius / 10f));   // small islets get fewer (they were crowded)
            for (int k = 0; k < nr; k++)
            {
                float a = (float)(rnd.NextDouble() * 6.283), y = DepthY(); var pos = WallPoint(terrain, centre, a, y, terrain.depth, out var o, 0.25f);
                var m = Model(root.transform, FirstAvailable(rnd, rocks), pos, (float)rnd.NextDouble() * 360f, 0.7f + (float)rnd.NextDouble() * 1.3f, false);
                if (m != null) { m.transform.position = pos; Orient(m, o, 0.9f); m.transform.Rotate(0, (float)rnd.NextDouble() * 360f, 0, Space.Self); }
            }
            // plants growing sideways out of cracks
            int np = Mathf.RoundToInt((5 + rnd.Next(6)) * size * Mathf.Lerp(0.5f, 1f, lush));
            string[] plants = { "Fern_1", "Fern_1", "Bush_Common", "Bush_Common_Flowers" };
            for (int k = 0; k < np; k++)
            {
                float a = (float)(rnd.NextDouble() * 6.283), y = -0.4f - (float)rnd.NextDouble() * Mathf.Min(4f, terrain.depth * 0.4f);
                var pos = WallPoint(terrain, centre, a, y, terrain.depth, out var o, 0.05f);
                var m = Model(root.transform, FirstAvailable(rnd, plants), pos, (float)rnd.NextDouble() * 360f, 0.5f + (float)rnd.NextDouble() * 0.7f, false);
                if (m != null) { m.transform.position = pos; Orient(m, o, 0.65f); }
            }
            // flowers on some islands
            if (rnd.NextDouble() < 0.45 * Mathf.Max(0.4f, lush) + 0.1 && Kit.Prop("flower_redA") != null)
            {
                int nc = 1 + rnd.Next(3);
                string[] cols = { "red", "purple", "yellow" };
                for (int c = 0; c < nc; c++)
                {
                    float a = (float)(rnd.NextDouble() * 6.283), y0 = -0.5f - (float)rnd.NextDouble() * 2.5f; string col = cols[rnd.Next(cols.Length)];
                    for (int k = 0; k < 3 + rnd.Next(3); k++)
                    {
                        float aa = a + (float)(rnd.NextDouble() - 0.5) * 0.18f, yy = y0 + (float)(rnd.NextDouble() - 0.5) * 0.7f;
                        var pos = WallPoint(terrain, centre, aa, yy, terrain.depth, out var o, 0.04f);
                        var m = Model(root.transform, "flower_" + col + "ABC"[rnd.Next(3)], pos, (float)rnd.NextDouble() * 360f, 0.5f + (float)rnd.NextDouble() * 0.4f, false);
                        if (m != null) { m.transform.position = pos; Orient(m, o, 0.8f); }
                    }
                }
            }
            if (bigTrees != null && bigTrees.Count > 0) TreeRoots(root.transform, terrain, centre, index, bigTrees);
        }

        /// <summary>
        /// Thick bark roots breaking out of the rocky underside right below each big tree (x, z, height) and hanging down into the air,
        /// with a few thinner side rootlets. Merged into one mesh, no collider, own random.
        /// </summary>
        static void TreeRoots(Transform parent, IslandTerrain terrain, Vector3 centre, int index, List<Vector3> trees)
        {
            var rnd = new System.Random(index * 7121 + 5);
            var parts = new List<CombineInstance>();
            float Rand(float a, float b) => a + (float)rnd.NextDouble() * (b - a);
            void Root(Vector3 start, Vector3 outward, float len, float thick, float drift)
            {
                var side = Vector3.Cross(Vector3.up, outward).normalized; float ph = Rand(0f, 6.28f), sw = Rand(-1f, 1f);
                var pts = new List<Vector3>();
                for (int i = 0; i <= 11; i++)
                {
                    float s = i / 11f;
                    pts.Add(start - centre + outward * (drift * len * (1f - (1f - s) * (1f - s))) + side * (len * 0.1f * Mathf.Sin(s * 4.5f + ph) * s + sw * s * s * len * 0.15f) + Vector3.down * (len * s));
                }
                parts.Add(new CombineInstance { mesh = ShapeMeshes.Tube(pts, q => thick * Mathf.Lerp(1.25f, 0.1f, Mathf.Sqrt(q)), 7), transform = Matrix4x4.identity });
                for (int b = 0, nb = rnd.Next(3); b < nb; b++)   // side rootlets
                {
                    int k = 3 + rnd.Next(4); var from = pts[k] + centre; float bl = len * Rand(0.25f, 0.45f);
                    var dir = (outward * Rand(-0.3f, 0.8f) + side * Rand(-1f, 1f)).normalized; var sub = new List<Vector3>();
                    for (int i = 0; i <= 6; i++) { float s = i / 6f; sub.Add(from - centre + dir * (bl * 0.6f * s) + Vector3.down * (bl * s * s)); }
                    float bt = thick * Mathf.Lerp(1.25f, 0.1f, Mathf.Sqrt(k / 11f)) * 0.6f;
                    parts.Add(new CombineInstance { mesh = ShapeMeshes.Tube(sub, q => bt * Mathf.Lerp(1f, 0.15f, q), 5), transform = Matrix4x4.identity });
                }
            }
            foreach (var t in trees)
            {
                float big = t.z / 12f, rt = new Vector2(t.x - centre.x, t.y - centre.z).magnitude, a0 = Mathf.Atan2(t.y - centre.z, t.x - centre.x);
                int n = Mathf.RoundToInt(Rand(3f, 5.5f) * Mathf.Sqrt(big));
                for (int k = 0; k < n; k++)
                {
                    float a = a0 + Rand(-1.3f, 1.3f) / Mathf.Max(rt, 2f), r = rt + Rand(-1f, 1f);
                    // the underside point straight below: walk down the side wall until it has curled in to the tree's distance from the centre
                    float y = IslandTerrain.Shoulder; Vector3 o = Vector3.zero, wp = Vector3.zero;
                    for (; y > -terrain.depth * 0.95f; y -= 0.15f)
                    {
                        wp = WallPoint(terrain, centre, a, y, terrain.depth, out o, 0.35f);
                        if (new Vector2(wp.x - centre.x, wp.z - centre.z).magnitude <= r) break;
                    }
                    Root(wp + Vector3.up * 0.3f, o, Rand(3f, 6.5f) * Mathf.Sqrt(big), Rand(0.16f, 0.28f) * Mathf.Sqrt(big), Rand(0.08f, 0.3f));
                }
            }
            if (parts.Count == 0) return;
            var mesh = new Mesh { name = "TreeRoots", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(parts.ToArray(), true, true);
            foreach (var c in parts) Object.DestroyImmediate(c.mesh);
            var g = new GameObject("TreeRoots"); g.transform.SetParent(parent, false); g.transform.position = centre;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = Kit.Mat("M_Bark");
        }

        // ------------------------------------------------------------------ leaf crowns
        static readonly Dictionary<string, Material> LeafMats = new Dictionary<string, Material>();

        /// <summary>Leaf tint for a bush standing at `pos` (position hash, so no random draws): greens, autumn warm colours, frosted in the snow.</summary>
        static Color BushTint(Vector3 pos)
        {
            int h = Mathf.Abs(Mathf.RoundToInt(pos.x * 7.3f) * 31 + Mathf.RoundToInt(pos.z * 5.9f) * 17) % 3;
            if (Theme == LevelTheme.Snow) return new Color(0.88f, 0.94f, 0.95f);
            if (Theme == LevelTheme.Autumn) return h == 0 ? new Color(0.92f, 0.45f, 0.16f) : h == 1 ? new Color(0.85f, 0.25f, 0.18f) : new Color(0.95f, 0.78f, 0.28f);
            return h == 0 ? new Color(0.55f, 0.88f, 0.42f) : h == 1 ? new Color(0.42f, 0.78f, 0.36f) : new Color(0.66f, 0.9f, 0.45f);
        }

        /// <summary>The Quaternius leaf-cluster material (Q_Leaves_NormalTree, alpha-clipped) tinted for `tint`; null when the kit does not carry it.</summary>
        static readonly Dictionary<string, Material> TintMats = new Dictionary<string, Material>();

        /// <summary>The greyscale leaf material (M_LeafTint, menu 16) tinted any colour with the leaf texture's detail kept; falls back to the green one.</summary>
        public static Material TintMat(Color tint)
        {
            var baseMat = Kit.Mat("M_LeafTint"); if (baseMat == null) return LeafMat(tint);
            string key = ColorUtility.ToHtmlStringRGB(tint);
            if (TintMats.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(baseMat) { name = "M_LeafTint_" + key }; m.SetColor("_BaseColor", tint);
            TintMats[key] = m; return m;
        }

        /// <summary>Leaf colour of a coloured tree: mostly green, red, yellow and orange, purple only rarely (autumn levels: no green or purple).</summary>
        static Color TreeTint(System.Random rnd)
        {
            bool autumn = Theme == LevelTheme.Autumn;
            float r = (float)rnd.NextDouble() * (autumn ? 0.68f : 1f);
            Color Jit(Color c) => new Color(Mathf.Clamp01(c.r + ((float)rnd.NextDouble() - 0.5f) * 0.12f), Mathf.Clamp01(c.g + ((float)rnd.NextDouble() - 0.5f) * 0.12f), Mathf.Clamp01(c.b + ((float)rnd.NextDouble() - 0.5f) * 0.08f), 1f);
            if (!autumn)
            {
                if (r < 0.28f) return Jit(new Color(0.42f, 0.82f, 0.36f));
                if (r > 0.96f) return Jit(new Color(0.62f, 0.35f, 0.85f));
                r = (r - 0.28f) / 0.68f * 0.68f;   // 0 .. 0.68 over red / yellow / orange
            }
            return r < 0.24f ? Jit(new Color(0.85f, 0.2f, 0.18f)) : r < 0.46f ? Jit(new Color(0.98f, 0.85f, 0.25f)) : Jit(new Color(0.98f, 0.52f, 0.14f));
        }

        /// <summary>Swaps the leaf material of an imported tree for the tinted one.</summary>
        static void Recolour(GameObject model, Color tint)
        {
            var mat = TintMat(tint); if (mat == null) return;
            foreach (var mr in model.GetComponentsInChildren<MeshRenderer>())
            {
                var mats = mr.sharedMaterials; bool changed = false;
                for (int i = 0; i < mats.Length; i++) if (mats[i] != null && mats[i].name.Contains("Leaves")) { mats[i] = mat; changed = true; }
                if (changed) mr.sharedMaterials = mats;
            }
        }

        public static Material LeafMat(Color tint)
        {
            var baseMat = Kit.Mat("Q_Leaves_NormalTree"); if (baseMat == null) return null;
            string key = ColorUtility.ToHtmlStringRGB(tint);
            if (LeafMats.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(baseMat) { name = "Q_Leaves_" + key }; m.SetColor("_BaseColor", tint);
            LeafMats[key] = m; return m;
        }

        /// <summary>
        /// A tree crown made of many crossed leaf-cluster cards (the imported trees' leaf texture) instead of a smooth sphere: one combined
        /// mesh, `radius` m across, centred on `centre` (world). Returns null when the leaf material is missing so callers keep the puffs.
        /// </summary>
        public static GameObject LeafCrown(Transform parent, Vector3 centre, float radius, float squash, Color tint, System.Random rnd, int cards = 38)
        {
            var mat = LeafMat(tint); if (mat == null) return null;
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>(); var cols = new List<Color>();
            for (int i = 0; i < cards; i++)
            {
                var dir = new Vector3((float)(rnd.NextDouble() * 2 - 1), (float)(rnd.NextDouble() * 2 - 1), (float)(rnd.NextDouble() * 2 - 1)); if (dir.sqrMagnitude < 0.05f) dir = Vector3.up; dir.Normalize();
                var pos = dir * radius * (0.35f + 0.6f * (float)rnd.NextDouble()); pos.y *= squash;
                float size = radius * (0.9f + 0.5f * (float)rnd.NextDouble());
                var rot = Quaternion.LookRotation(dir == Vector3.up ? Vector3.forward : dir, Vector3.up) * Quaternion.Euler((float)rnd.NextDouble() * 60f - 30f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 360f);
                int o = verts.Count;
                foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
                { verts.Add(pos + rot * new Vector3(c.x, c.y, 0) * size * 0.5f); uvs.Add(new Vector2((c.x + 1) * 0.5f, (c.y + 1) * 0.5f)); }
                tris.AddRange(new[] { o, o + 2, o + 1, o + 1, o + 2, o + 3 });
            }
            var mesh = new Mesh { name = "LeafCrown", indexFormat = UnityEngine.Rendering.IndexFormat.UInt16 };
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            // soft, rounded shading: normals point away from the crown centre (cards are two-sided)
            var n = new Vector3[verts.Count]; for (int i = 0; i < n.Length; i++) n[i] = (verts[i] + Vector3.up * radius * 0.1f).normalized; mesh.SetNormals(n);
            var go = new GameObject("LeafCrown"); go.transform.SetParent(parent, true); go.transform.position = centre;
            go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        // ------------------------------------------------------------------ pieces
        static GameObject Prim(string name, PrimitiveType t, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collide)
        {
            var g = GameObject.CreatePrimitive(t); g.name = name; g.transform.SetParent(parent, true);
            g.transform.position = pos; g.transform.localScale = scale; g.GetComponent<Renderer>().sharedMaterial = m;
            if (!collide) Object.DestroyImmediate(g.GetComponent<Collider>());
            else { int gl = LayerMask.NameToLayer(Layers.GroundName); if (gl >= 0) g.layer = gl; }
            return g;
        }

        /// <summary>A faceted rock / gem mesh from DebrisMeshes placed like Prim (replaces the plain spheres on the ground).</summary>
        static GameObject MeshPrim(string name, Mesh mesh, Transform parent, Vector3 pos, Vector3 scale, Material m, bool collide)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, true); g.transform.position = pos; g.transform.localScale = scale * 0.62f;   // the debris meshes are ~1.6 m across, a unit sphere is 1 m
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = m;
            if (collide) { var mc = g.AddComponent<MeshCollider>(); mc.sharedMesh = mesh; mc.convex = true; int gl = LayerMask.NameToLayer(Layers.GroundName); if (gl >= 0) g.layer = gl; }
            return g;
        }

        /// <summary>Marble chips and brick chunks round the ruins, a few glinting gems, and shiny debris hovering just outside the rim. Own random: the layout is unchanged.</summary>
        static void ShinyBits(Transform isle, Vector3 centre, System.Func<float, float> edge, int index, List<Vector3> ruinSpots, System.Func<Vector2, float, bool> free, GroundKind ground)
        {
            var rnd = new System.Random(index * 4813 + 211);
            var root = new GameObject("ShinyBits"); root.transform.SetParent(isle, true); root.transform.position = Vector3.zero;
            var terrain = isle.GetComponent<IslandTerrain>();
            var c2 = new Vector2(centre.x, centre.z);
            float Y(Vector2 p) => terrain != null ? centre.y + terrain.HeightAtWorld(new Vector3(p.x, centre.y, p.y)) : centre.y;
            var marble = Kit.Mat("M_Marble") ?? _marble; var stone = Kit.Mat("M_Stone"); var crystal = CrystalMat();
            Material brick = Kit.Mat("M_Brick") ?? Kit.Mat("M_TerrainMud") ?? stone;
            // chips of white marble and chunks of old brick scattered round every ruin
            foreach (var rs in ruinSpots)
                for (int k = 0, n = 7 + rnd.Next(7); k < n; k++)
                {
                    float a = (float)(rnd.NextDouble() * 6.283), r = 0.6f + (float)rnd.NextDouble() * 2.6f;
                    var p = new Vector2(rs.x + Mathf.Cos(a) * r, rs.z + Mathf.Sin(a) * r);
                    if (Vector2.Distance(p, c2) > edge(Mathf.Atan2(p.y - c2.y, p.x - c2.x)) * 0.93f || !free(p, 0.3f)) continue;
                    bool isBrick = rnd.NextDouble() < 0.3; float sz = 0.08f + (float)rnd.NextDouble() * 0.2f;
                    var g = MeshPrim(isBrick ? "Brick" : "MarbleChip", DebrisMeshes.Tile(rnd.Next(8)), root.transform, new Vector3(p.x, Y(p) + sz * 0.15f, p.y),
                                     isBrick ? new Vector3(sz * 2.2f, sz * 1.1f, sz * 1.2f) : new Vector3(sz * 1.5f, sz * 0.45f, sz * 1.1f), isBrick ? brick : marble, false);
                    g.transform.rotation = Quaternion.Euler((float)rnd.NextDouble() * 12f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 12f);
                    g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            // a few glinting gems on the ground (more on bare islands)
            int gems = 2 + rnd.Next(3) + (ground == GroundKind.Grass ? 0 : 2);
            for (int k = 0; k < gems; k++)
            {
                float a = (float)(rnd.NextDouble() * 6.283), r = edge(a) * (0.2f + (float)rnd.NextDouble() * 0.7f);
                var p = c2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; if (!free(p, 0.8f)) continue;
                float sz = 0.12f + (float)rnd.NextDouble() * 0.16f;
                var gem = MeshPrim("Gem", DebrisMeshes.Crystal(rnd.Next(4)), root.transform, new Vector3(p.x, Y(p) + sz * 0.9f, p.y), new Vector3(sz, sz * 1.4f, sz), crystal, false);
                gem.transform.rotation = Quaternion.Euler((float)(rnd.NextDouble() - 0.5) * 30f, (float)rnd.NextDouble() * 360f, (float)(rnd.NextDouble() - 0.5) * 30f);
                var sp = gem.AddComponent<GemSparkle>(); sp.glow = Theme == LevelTheme.Snow || rnd.NextDouble() < 0.3 ? new Color(1f, 0.8f, 0.4f) : new Color(0.49f, 0.95f, 0.9f);
            }
            // fallen logs on the rocky islands (a few on mud ones): bark lathe logs lying on the ground, some with a branch stub
            if (ground != GroundKind.Grass)
            {
                var bark = Kit.Mat("M_Bark") ?? stone;
                for (int k = 0, n = (ground == GroundKind.Rock ? 4 : 2) + rnd.Next(2); k < n; k++)
                {
                    float a = (float)(rnd.NextDouble() * 6.283), r = edge(a) * (0.25f + (float)rnd.NextDouble() * 0.6f);
                    var p = c2 + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r; if (!free(p, 1.4f)) continue;
                    float len = 1.8f + (float)rnd.NextDouble() * 2.2f, rad = 0.17f + (float)rnd.NextDouble() * 0.12f;
                    var prof = new List<Vector2> { new Vector2(0.001f, -len / 2), new Vector2(rad * 0.8f, -len / 2), new Vector2(rad, -len / 2 + 0.05f), new Vector2(rad * 1.06f, -len * 0.25f), new Vector2(rad, len * 0.25f), new Vector2(rad * 0.95f, len / 2 - 0.05f), new Vector2(rad * 0.78f, len / 2), new Vector2(0.001f, len / 2) };
                    var lg = MeshPrim("FallenLog", ShapeMeshes.Lathe(prof, 12, false, (ang, q) => 1f + 0.08f * Mathf.Cos(ang * 7f), "FallenLog"), root.transform, new Vector3(p.x, Y(p) + rad * 0.8f, p.y), Vector3.one / 0.62f, bark, false);   // MeshPrim scales by 0.62
                    lg.transform.rotation = Quaternion.Euler(90f + ((float)rnd.NextDouble() - 0.5f) * 8f, (float)rnd.NextDouble() * 360f, 0f);
                    if (rnd.NextDouble() < 0.5)
                    {
                        var stub = MeshPrim("Branch", ShapeMeshes.Lathe(new List<Vector2> { new Vector2(0.001f, 0f), new Vector2(rad * 0.45f, 0f), new Vector2(rad * 0.3f, 0.45f), new Vector2(0.001f, 0.5f) }, 8, false, null, "Branch"), lg.transform, lg.transform.position, Vector3.one / 0.62f, bark, false);
                        stub.transform.rotation = lg.transform.rotation * Quaternion.Euler(0, 0, 0) * Quaternion.AngleAxis(50f, Vector3.right) * Quaternion.AngleAxis((float)rnd.NextDouble() * 360f, Vector3.up);
                        stub.transform.position = lg.transform.position + lg.transform.up * (((float)rnd.NextDouble() - 0.5f) * len * 0.5f);
                    }
                    lg.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            // shiny debris outside the island: chips of marble and gems hovering just past the rim and under it
            for (int k = 0, n = 6 + rnd.Next(6); k < n; k++)
            {
                float a = (float)(rnd.NextDouble() * 6.283), r = edge(a) + 1.2f + (float)rnd.NextDouble() * 5f;
                var pos = new Vector3(centre.x + Mathf.Cos(a) * r, centre.y + IslandTerrain.Shoulder - 5f + (float)rnd.NextDouble() * 7f, centre.z + Mathf.Sin(a) * r);
                float sz = 0.15f + (float)rnd.NextDouble() * 0.35f; bool gem = rnd.NextDouble() < 0.5;
                var g = MeshPrim(gem ? "FloatGem" : "FloatChip", gem ? DebrisMeshes.Crystal(rnd.Next(4)) : DebrisMeshes.Tile(rnd.Next(8)), root.transform, pos,
                                 gem ? new Vector3(sz, sz * 1.5f, sz) : new Vector3(sz * 1.6f, sz * 0.8f, sz * 1.2f), gem ? crystal : marble, false);
                var b = g.AddComponent<FloatBob>(); b.amplitude = 0.25f + sz; b.period = 3f + (float)rnd.NextDouble() * 4f; b.wobble = 10f; b.spin = (rnd.NextDouble() < 0.5 ? -1f : 1f) * (10f + (float)rnd.NextDouble() * 30f);
                if (gem) g.AddComponent<GemSparkle>().baseLevel = 0.3f;
                g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        // tree species: 0 oak, 1 pine, 2 birch, 3 blossom, 4 willow, 5 cypress, 6 autumn
        public const int TreeKinds = 8;   // 6 = coloured oak (green, red, yellow, orange, rarely purple), 7 = palm (Kenney CC0)

        static int PickKind(float[] weights, System.Random rnd)
        {
            float sum = 0; foreach (var w in weights) sum += w;
            float r = (float)rnd.NextDouble() * sum;
            for (int i = 0; i < weights.Length; i++) { r -= weights[i]; if (r <= 0) return i; }
            return 0;
        }

        // ------------------------------------------------------------------ imported models (Kenney CC0 kits)
        static Material _yellowCrystal;
        /// <summary>The glowing crystal material; snow levels use a golden-yellow copy (the cyan crystals read as green against the snow).</summary>
        static Material CrystalMat()
        {
            var m = Kit.Mat("M_CrystalShard") ?? Kit.Mat("M_Crystal");
            if (Theme != LevelTheme.Snow || m == null) return m;
            if (_yellowCrystal != null) return _yellowCrystal;
            _yellowCrystal = new Material(m) { name = "M_Crystal_Snow", hideFlags = HideFlags.DontSave };
            var yellow = new Color(1f, 0.82f, 0.25f);
            if (_yellowCrystal.HasProperty("_BaseColor")) _yellowCrystal.SetColor("_BaseColor", yellow);
            if (_yellowCrystal.HasProperty("_Color")) _yellowCrystal.SetColor("_Color", yellow);
            if (_yellowCrystal.HasProperty("_EmissionColor")) _yellowCrystal.SetColor("_EmissionColor", yellow * 0.8f);
            return _yellowCrystal;
        }

        static Material _flowerBlue;
        static readonly Dictionary<(Material, string), Material> _derived = new Dictionary<(Material, string), Material>();
        /// <summary>A copy of `src` named `name`, set up once by `tint` and then shared (seed-source groves reuse their fruit's tinted variants).</summary>
        static Material Derived(Material src, string name, System.Action<Material> tint)
        {
            if (_derived.TryGetValue((src, name), out var m) && m != null) return m;
            m = new Material(src) { name = name }; tint(m);
            return _derived[(src, name)] = m;
        }

        /// <summary>
        /// Places an imported prop (Kit.Prop) standing on `pos`, scaled so it is `height` m tall (0 = native size), turned by `yaw`.
        /// Optional box collider on the Ground layer. Returns null when the kit has no such model, so callers fall back to primitives.
        /// </summary>
        static GameObject Model(Transform parent, string name, Vector3 pos, float yaw, float height, bool collide)
        {
            var go = OrganicProps.Create(name, pos, yaw, parent, Theme);   // the blocky Kenney flowers, grass and rocks: smooth stand-ins
            if (go == null)
            {
                bool flatBush = name.StartsWith("plant_bush") && Kit.Prop("Bush_Common") != null;   // Kenney's flat-leaf bushes: the leafy Quaternius bush, tinted
                var prefab = Kit.Prop(flatBush ? "Bush_Common" : name); if (prefab == null) return null;
                go = Object.Instantiate(prefab, pos, Quaternion.Euler(0, yaw, 0), parent); go.name = name;
                if (flatBush) { Recolour(go, BushTint(pos)); if (height <= 0f) go.transform.localScale *= 0.8f; }
            }
            var rs = go.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) { Object.Destroy(go); return null; }
            Bounds B() { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b; }
            var bb = B();
            if (height > 0 && bb.size.y > 0.001f) { go.transform.localScale *= height / bb.size.y; bb = B(); }
            go.transform.position += Vector3.up * (pos.y - bb.min.y);
            bb = B();
            if (collide)
            {
                var c = new GameObject("Collider"); c.transform.SetParent(go.transform, true); c.transform.position = bb.center;
                c.AddComponent<BoxCollider>().size = bb.size * 0.9f;
                int gl = LayerMask.NameToLayer(Layers.GroundName); if (gl >= 0) c.layer = gl;
            }
            return go;
        }

        // Quaternius Stylized Nature MegaKit (CC0): the textured trees, picked by kind; autumn and "coloured" (kind 6) oaks and willows are recoloured in Tree()
        static readonly string[][] QuaterniusTrees =
        {
            new[] { "CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "CommonTree_5" },   // oak
            new[] { "Pine_1", "Pine_2", "Pine_3", "Pine_4", "Pine_5" },                                 // pine
            new[] { "CommonTree_2", "CommonTree_4" },                                                   // birch
            new[] { "CommonTree_1", "CommonTree_3" },                                                   // blossom
            new[] { "TwistedTree_1", "TwistedTree_2", "TwistedTree_3", "TwistedTree_4", "TwistedTree_5" }, // willow
            new[] { "Pine_2", "Pine_4" },                                                               // cypress
        };

        static readonly string[] PalmModels = { "tree_palmTall", "tree_palmDetailedTall", "tree_palmBend", "tree_palm", "tree_palmDetailedShort", "tree_palmShort" };

        static string ThemedTree(int kind, System.Random rnd)
        {
            if (kind == 7) return FirstAvailable(rnd, PalmModels);
            var q = QuaterniusTrees[kind == 6 ? 0 : kind];   // kind 6 (coloured): the textured oaks
            return q[rnd.Next(q.Length)];
        }

        static string FirstAvailable(System.Random rnd, params string[] names)
        {
            var ok = new List<string>(); foreach (var n in names) if (Kit.Prop(n) != null) ok.Add(n);
            return ok.Count == 0 ? names[0] : ok[rnd.Next(ok.Count)];
        }

        static readonly Dictionary<Mesh, Mesh> SimplifiedTrees = new Dictionary<Mesh, Mesh>();

        /// <summary>The imported mesh a simplified tree copy was made from (the atmosphere builder bakes the original, an asset, instead of the copy); `m` itself otherwise.</summary>
        public static Mesh SourceOf(Mesh m) { foreach (var kv in SimplifiedTrees) if (kv.Value == m) return kv.Key; return m; }

        /// <summary>The Quaternius trees are 3-10k triangles, mostly bark. Swap in a copy whose leaf-card submesh uses generated mesh LOD 1 (fewer cards) while the bark keeps
        /// LOD 0; deeper LODs collapse the alpha-clipped leaf cards entirely, leaving bare trees.</summary>
        static void SimplifyTree(GameObject model)
        {
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
            {
                var src = mf.sharedMesh; var mr = mf.GetComponent<MeshRenderer>();
                if (src == null || mr == null || src.lodCount < 3 || !src.isReadable) continue;
                if (!SimplifiedTrees.TryGetValue(src, out var m) || m == null)
                {
                    m = new Mesh { name = src.name + "_Simplified", indexFormat = src.indexFormat };
                    m.vertices = src.vertices; m.normals = src.normals; m.tangents = src.tangents; m.uv = src.uv; m.colors = src.colors;
                    m.subMeshCount = src.subMeshCount;
                    var mats = mr.sharedMaterials;
                    for (int i = 0; i < src.subMeshCount; i++)
                    {
                        bool leaf = i < mats.Length && mats[i] != null && mats[i].name.Contains("Leaves");
                        m.SetIndices(src.GetIndices(i, leaf ? 1 : 0, false), MeshTopology.Triangles, i);
                    }
                    m.RecalculateBounds(); SimplifiedTrees[src] = m;
                }
                mf.sharedMesh = m;
            }
        }

        static void Tree(Transform parent, Vector3 pos, float height, System.Random rnd, int kind = -1)
        {
            if (kind < 0) kind = new[] { 0, 3, 1, 2 }[rnd.Next(4)];
            float sv = Mathf.Repeat(Mathf.Sin(pos.x * 12.9898f + pos.z * 78.233f + kind * 3.7f) * 43758.5453f, 1f);   // position hash: varies size without consuming the layout's random stream
            height *= (kind == 7 ? 0.65f : 0.72f) + sv * sv * (kind == 7 ? 0.95f : 0.7f) + sv * 0.12f;   // 0.7x to about 1.5x, mostly mid-sized with the odd giant or sapling
            pos += Vector3.down * (0.25f + 0.04f * height);   // sink the foot into the ground (the island's Lift sets the terrain height afterwards)
            float mh = kind == 7 ? height * 1.3f : kind == 1 ? height * 1.5f : kind == 2 ? height * 1.35f : kind == 5 ? height * 1.6f : height * 1.1f;
            if (_leafTrees != null && kind != 1 && kind != 5 && kind != 7) _leafTrees.Add(new Vector4(pos.x, pos.y, pos.z, kind == 2 ? height * 1.35f : height * 1.1f));
            var model = Model(parent, ThemedTree(kind, rnd), pos, (float)rnd.NextDouble() * 360f, mh, false);
            if (model != null)
            {
                SimplifyTree(model);
                if (kind == 7)
                {
                    if (!OrganicProps.Replaces(model.name)) PalmLook.Apply(model);   // the procedural palm brings its own UVs
                    var coco = Kit.Item("coconut");   // at most one fallen coconut at the foot of about two palms in three; it never regrows
                    if (coco != null && coco.worldPrefab != null && Mathf.Repeat(sv * 7.31f, 1f) < 0.65f)
                    {
                        float ca = sv * 40f; var cp = pos + new Vector3(Mathf.Cos(ca), 0.9f, Mathf.Sin(ca)) * (height * 0.12f + 0.5f);
                        var cgo = Object.Instantiate(coco.worldPrefab, cp, Quaternion.identity, parent); cgo.name = "Coconut";
                    }
                }
                bool coloured = kind == 6 || (Theme == LevelTheme.Autumn && kind != 1 && kind != 5);
                if (coloured && model.name.StartsWith("CommonTree") || coloured && model.name.StartsWith("TwistedTree")) Recolour(model, TreeTint(rnd));
                var tcol = new GameObject("Trunk"); tcol.transform.SetParent(model.transform.parent, true); tcol.transform.position = pos;
                var cap = tcol.AddComponent<CapsuleCollider>(); cap.radius = 0.28f * Mathf.Max(1f, height / 4f); cap.height = Mathf.Min(mh, 2.5f); cap.center = new Vector3(0, cap.height * 0.5f, 0);
                int g0 = LayerMask.NameToLayer(Layers.GroundName); if (g0 >= 0) tcol.layer = g0;
                return;
            }
            var foliage = Mat("M_Foliage"); var light = Mat("M_FoliageLight"); var bark = Kit.Mat("M_Bark");
            var go = new GameObject("Tree_" + kind); go.transform.SetParent(parent, true); go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
            float h = kind == 1 ? height * 1.5f : kind == 2 ? height * 1.35f : kind == 5 ? height * 1.6f : height;
            float baseR = kind == 2 ? 0.2f : kind == 1 || kind == 5 ? 0.24f : 0.3f, topR = kind == 2 ? 0.1f : 0.14f;
            var trunk = new GameObject("Trunk"); trunk.transform.SetParent(go.transform, false);
            trunk.AddComponent<MeshFilter>().sharedMesh = TreeParts.Trunk(kind == 5 ? h * 0.35f : h, baseR, topR, rnd.Next());
            trunk.AddComponent<MeshRenderer>().sharedMaterial = kind == 2 ? _birch : bark;
            float th = kind == 5 ? h * 0.35f : h;
            var cc = trunk.AddComponent<CapsuleCollider>(); cc.radius = baseR * 0.9f; cc.height = th; cc.center = new Vector3(0, th * 0.5f, 0);
            int gl = LayerMask.NameToLayer(Layers.GroundName); if (gl >= 0) trunk.layer = gl;
            Vector3 P(float dx, float y, float dz) => go.transform.position + new Vector3(dx, y, dz);
            void Puff(string n, Vector3 p, float w, float hgt, Material m) => Prim(n, PrimitiveType.Sphere, go.transform, p, new Vector3(w, hgt, w), m, false);
            float cr = height * 0.42f;
            switch (kind)
            {
                case 1:   // pine: stacked tapering tiers
                    for (int t = 0; t < 5; t++) Puff("Tier" + t, P(0, h * (0.32f + t * 0.16f), 0), cr * (2.0f - t * 0.32f), cr * 1.1f, Mat("M_FoliageDark"));
                    break;
                case 2:   // birch: slim, light crown high up
                    for (int c = 0; c < 5; c++)
                    {
                        float a = c * 1.3f + (float)rnd.NextDouble(), rr = c == 0 ? 0 : cr * 0.45f;
                        Puff("Crown" + c, P(Mathf.Cos(a) * rr, h * (c == 0 ? 0.98f : 0.78f + (float)rnd.NextDouble() * 0.2f), Mathf.Sin(a) * rr), cr * (c == 0 ? 1.3f : 0.95f), cr * 1.0f, light);
                    }
                    break;
                case 4:   // willow: flat crown with hanging strands
                    Puff("Crown", P(0, h * 1.0f, 0), cr * 2.3f, cr * 0.9f, light);
                    for (int st = 0; st < 12; st++)
                    {
                        float a = st * Mathf.PI * 2 / 12f + (float)rnd.NextDouble() * 0.3f, rr = cr * (0.85f + (float)rnd.NextDouble() * 0.3f);
                        float len = 1.2f + (float)rnd.NextDouble() * 0.9f;
                        Prim("Strand" + st, PrimitiveType.Cylinder, go.transform, P(Mathf.Cos(a) * rr, h * 1.0f - len * 0.5f, Mathf.Sin(a) * rr), new Vector3(0.09f, len * 0.5f, 0.09f), foliage, false);
                    }
                    break;
                case 5:   // cypress: one tall dark ellipsoid
                    Puff("Crown", P(0, th + h * 0.38f, 0), cr * 1.25f, h * 0.95f, Mat("M_FoliageDark"));
                    break;
                default:  // oak / blossom / autumn: leaf-card crowns (imported leaf texture), clustered puffs when the leaf material is missing
                    if (LeafCrown(go.transform, P(0, h * 1.0f, 0), cr * 1.9f, 0.8f, kind == 3 ? new Color(1f, 0.85f, 0.9f) : kind == 6 ? new Color(1f, 0.75f, 0.35f) : Color.white, rnd, 60) != null) break;
                    Material a1 = kind == 3 ? Mat("M_Blossom") : kind == 6 ? _autumn : foliage, a2 = kind == 3 ? _white : kind == 6 ? _yellow : light;
                    for (int c = 0; c < 6; c++)
                    {
                        float a = c * 1.05f + (float)rnd.NextDouble() * 0.5f, rr = c == 0 ? 0f : cr * 0.55f;
                        float sz = cr * (c == 0 ? 1.7f : 1.1f + (float)rnd.NextDouble() * 0.4f);
                        Puff("Crown" + c, P(Mathf.Cos(a) * rr, h * (c == 0 ? 1.0f : 0.85f + (float)rnd.NextDouble() * 0.25f), Mathf.Sin(a) * rr), sz, sz * 0.8f, c % 2 == 0 ? a1 : (kind == 0 ? a2 : a1));
                    }
                    break;
            }
        }

        static void Bush(Transform parent, Vector3 pos, float size, System.Random rnd)
        {
            var foliage = Mat("M_Foliage"); var light = Mat("M_FoliageLight"); var stone = Kit.Mat("M_Stone");
            var go = new GameObject("Bush"); go.transform.SetParent(parent, true); go.transform.position = pos;
            int type = rnd.Next(10);   // 0-5 round, 6-7 berry, 8-9 rock cluster
            {
                string[] bushes = Theme == LevelTheme.Autumn ? new[] { "plant_bush", "plant_bushLarge", "plant_bushDetailed", "plant_bushSmall" } : new[] { "Bush_Common", "Bush_Common_Flowers", "Fern_1", "plant_bush", "plant_bushLarge" };
                string[] rocks = { "rock_smallA", "rock_smallB", "stone_smallC", "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3", "Mushroom_Common" };
                string pick = type >= 8 ? FirstAvailable(rnd, rocks) : FirstAvailable(rnd, bushes);
                var m = Model(parent, pick, pos, (float)rnd.NextDouble() * 360f, size * (type >= 8 ? 0.8f : 1.0f), type >= 8);   // rocks block the player, shrubs do not
                if (m != null) { Object.Destroy(go); return; }
            }
            if (type >= 8)
            {
                go.name = "Rocks";
                for (int i = 0; i < 3; i++)
                {
                    float a = i * 2.2f + (float)rnd.NextDouble();
                    var r = MeshPrim("Rock" + i, DebrisMeshes.Rock(rnd.Next(8)), go.transform, pos + new Vector3(Mathf.Cos(a) * size * 0.4f, size * 0.18f, Mathf.Sin(a) * size * 0.4f), new Vector3(size * (0.5f + i * 0.12f), size * 0.4f, size * 0.55f), stone, true);
                    r.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 180f, 0);
                }
                return;
            }
            for (int i = 0; i < 3; i++)
            {
                float a = i * 2.1f + (float)rnd.NextDouble();
                Prim("Puff" + i, PrimitiveType.Sphere, go.transform, pos + new Vector3(Mathf.Cos(a) * size * 0.35f, size * 0.3f, Mathf.Sin(a) * size * 0.35f),
                    new Vector3(size, size * 0.75f, size), i == 1 ? light : foliage, true);
            }
            if (type >= 6)
            {
                go.name = "BerryBush";
                var m = type == 6 ? _berryRed : _berryBlue;
                for (int i = 0; i < 7; i++)
                {
                    float a = (float)(rnd.NextDouble() * Mathf.PI * 2), r = size * (0.45f + (float)rnd.NextDouble() * 0.25f);
                    Prim("Berry" + i, PrimitiveType.Sphere, go.transform, pos + new Vector3(Mathf.Cos(a) * r, size * (0.25f + (float)rnd.NextDouble() * 0.45f), Mathf.Sin(a) * r), Vector3.one * 0.14f, m, false);
                }
            }
        }

        // ------------------------------------------------------------------ seed patches (distinct look per seed)
        /// <summary>Ground disc under a seed source: snow-white on snowy levels, otherwise the given foliage material.</summary>
        static Material PatchBase(Material foliage) => Theme == LevelTheme.Snow ? Ground.Top(GroundKind.Grass) : foliage;

        /// <summary>T33: a copy of the seed's shape (its pickup mesh and material) used as fruit on the seed sources.</summary>
        static GameObject Fruit(Transform parent, string name, Vector3 pos, float scale, Mesh mesh, Material mat, Quaternion rot)
        {
            var g = new GameObject(name); g.transform.SetParent(parent, true);
            g.transform.position = pos; g.transform.rotation = rot; g.transform.localScale = Vector3.one * scale;
            g.AddComponent<MeshFilter>().sharedMesh = mesh; g.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return g;
        }

        /// <summary>Heat-seed source: a melon patch. Creeping vines with big lobed leaves, curly tendrils and small yellow flowers, and ribbed warm melons lying in the middle.</summary>
        public static void MelonPatch(Transform parent, Vector3 pos, Material fruit, System.Random rnd)
        {
            EnsureMaterials();
            var foliage = Kit.Mat("M_Foliage"); var light = Kit.Mat("M_FoliageLight");   // the Heat Seed patch stays green on every theme: it sits on warm ground
            Prim("Base", PrimitiveType.Cylinder, parent, pos + Vector3.down * 0.03f, new Vector3(3.0f, 0.05f, 3.0f), foliage, false);
            GameObject At(string n, Mesh m, Material mat, Vector3 off, Vector3 euler, Vector3 sc)
            {
                var g = ShapeMeshes.Obj(n, m, mat, parent, Vector3.zero, euler, sc); g.transform.position = pos + off; return g;
            }
            const int vines = 6;
            var petalMesh = ShapeMeshes.Ellipsoid(0.1f, 0.02f, 8, 5); var centreMesh = ShapeMeshes.Ellipsoid(0.04f, 0.035f, 8, 6);   // shared by every vine-tip flower
            for (int v = 0; v < vines; v++)
            {
                float a0 = v * 6.283f / vines + (float)rnd.NextDouble() * 0.4f, len = 1.7f + (float)rnd.NextDouble() * 0.5f, wig = 0.3f + (float)rnd.NextDouble() * 0.3f, ph = (float)rnd.NextDouble() * 6f;
                var pts = new List<Vector3>();
                for (int i = 0; i <= 10; i++)
                {
                    float q = i / 10f, d = 0.25f + len * q, a = a0 + wig * Mathf.Sin(q * 5f + ph);
                    pts.Add(new Vector3(Mathf.Cos(a) * d, 0.06f + 0.03f * Mathf.Sin(q * 9f + ph), Mathf.Sin(a) * d));
                }
                At("Vine" + v, ShapeMeshes.Tube(pts, q => 0.032f - 0.016f * q, 6), foliage, Vector3.zero, Vector3.zero, Vector3.one);
                for (int i = 2; i <= 8; i += 2)   // big leaves on alternate sides
                {
                    int side = (i / 2 + v) % 2 == 0 ? 1 : -1;
                    var tan = (pts[i + 1] - pts[i - 1]).normalized; var outv = Vector3.Cross(Vector3.up, tan) * side;
                    float ll = 0.75f - 0.05f * i * 0.3f;
                    var leafGo = ShapeMeshes.Obj("Leaf", LeafMesh.Build(ll, ll * 0.85f, 0.05f, 0.06f, 10), (i + v) % 2 == 0 ? light : foliage, parent, Vector3.zero, Vector3.zero, Vector3.one);
                    var dir = (outv + Vector3.up * 0.18f).normalized;
                    leafGo.transform.position = pos + pts[i] + dir * ll * 0.5f; leafGo.transform.rotation = Quaternion.FromToRotation(Vector3.right, dir);
                    if (i == 4)   // a curly tendril next to the leaf
                    {
                        var tp = new List<Vector3>(); var sd = pts[i] + outv * 0.05f;
                        for (int k = 0; k <= 12; k++) { float q = k / 12f, ang = q * 9f; tp.Add(sd + outv * (0.25f * q) + Vector3.up * (0.04f + 0.1f * q * Mathf.Sin(ang)) + tan * (0.1f * q * Mathf.Cos(ang))); }
                        At("Tendril", ShapeMeshes.Tube(tp, q => 0.012f * (1f - 0.5f * q), 5), light, Vector3.zero, Vector3.zero, Vector3.one);
                    }
                }
                // a small yellow flower at the vine tip
                var tip = pts[pts.Count - 1] + Vector3.up * 0.05f;
                for (int pt = 0; pt < 5; pt++)
                {
                    float pa = pt * 1.257f;
                    At("Petal", petalMesh, _yellow, tip + new Vector3(Mathf.Cos(pa) * 0.1f, 0f, Mathf.Sin(pa) * 0.1f), new Vector3(0, -pa * Mathf.Rad2Deg, 20f), new Vector3(1.2f, 1f, 0.7f));
                }
                At("FlowerCentre", centreMesh, _gold, tip + Vector3.up * 0.02f, Vector3.zero, Vector3.one);
            }
            // the melons: ribbed ovals in the seed's warm orange, two paler green-gold ones, resting on little leaf pads
            var pale = Derived(fruit, "M_MelonPale", m => { if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.Lerp(fruit.GetColor("_BaseColor"), new Color(0.55f, 0.75f, 0.25f), 0.65f)); });
            var melon = ShapeMeshes.Melon();
            for (int m = 0; m < 5; m++)
            {
                float a = m * 1.257f + (float)rnd.NextDouble() * 0.4f, d = m == 0 ? 0.1f : 0.7f + (float)rnd.NextDouble() * 0.55f, s = 1.05f + (float)rnd.NextDouble() * 0.5f;
                var g = At("Melon" + m, melon, m % 3 == 2 ? pale : fruit, new Vector3(Mathf.Cos(a) * d, 0.23f * s + 0.03f, Mathf.Sin(a) * d), new Vector3(90f + (float)(rnd.NextDouble() - 0.5) * 10f, (float)rnd.NextDouble() * 360f, 0f), Vector3.one * s);
                var padGo = ShapeMeshes.Obj("Pad", LeafMesh.Build(0.9f, 0.7f, 0.02f, 0.03f, 8), light, parent, Vector3.zero, Vector3.zero, Vector3.one);
                padGo.transform.position = pos + new Vector3(Mathf.Cos(a) * d, 0.015f, Mathf.Sin(a) * d); padGo.transform.rotation = Quaternion.Euler(0, a * 57f + 90f * m, 0);
            }
        }

        /// <summary>Vine-seed source: a grove of tall bright-green bamboo culms with leafy tufts and bamboo pieces hanging in the middle.</summary>
        public static void BambooGrove(Transform parent, Vector3 pos, Material fruit, Mesh fruitMesh, System.Random rnd)
        {
            EnsureMaterials();
            var foliage = Mat("M_Foliage"); var light = Mat("M_FoliageLight");
            var culmMat = fruit != null ? Derived(fruit, "M_BambooCulm", m => { m.SetColor("_BaseColor", fruit.GetColor("_BaseColor") * 0.82f); m.SetColor("_EmissionColor", fruit.GetColor("_BaseColor") * 0.12f); }) : light;
            int n = 6; var culms = new List<(Transform t, float h)>();
            for (int i = 0; i < n; i++)
            {
                float a = i * 6.28f / n + (float)rnd.NextDouble() * 0.5f, d = i == 0 ? 0f : 0.55f + (float)rnd.NextDouble() * 0.45f;
                int tier = (i + rnd.Next(3)) % 3;   // T40: short, medium and tall culms in one grove
                float h = tier == 0 ? 2.2f + (float)rnd.NextDouble() * 0.9f : tier == 1 ? 3.5f + (float)rnd.NextDouble() * 1.1f : 5.0f + (float)rnd.NextDouble() * 1.3f;
                var cp = pos + new Vector3(Mathf.Cos(a) * d, -0.18f, Mathf.Sin(a) * d);   // culm feet sunk into the ground
                var culm = new GameObject("Culm" + i); culm.transform.SetParent(parent, true); culm.transform.position = cp;
                culm.transform.rotation = Quaternion.Euler(Mathf.Sin(a) * 5f, 0, -Mathf.Cos(a) * 5f);   // leaning a little outwards
                culm.AddComponent<MeshFilter>().sharedMesh = SeedMeshes.Culm(h, 0.12f + (float)rnd.NextDouble() * 0.03f, Mathf.Clamp(Mathf.RoundToInt(h * 1.2f), 3, 8), false);
                culm.AddComponent<MeshRenderer>().sharedMaterial = culmMat; culms.Add((culm.transform, h));
                for (int l = 0; l < 4; l++)   // narrow leaves drooping from the top joints
                {
                    float la = l * 1.7f + i, y = h * (0.62f + 0.09f * l);
                    var lp = culm.transform.TransformPoint(new Vector3(Mathf.Cos(la) * 0.28f, y, Mathf.Sin(la) * 0.28f));
                    Prim("Leaf", PrimitiveType.Sphere, parent, lp, new Vector3(0.75f, 0.04f, 0.13f), l % 2 == 0 ? foliage : light, false)
                        .transform.rotation = Quaternion.Euler(0, -la * Mathf.Rad2Deg, -22f);
                }
            }
            if (fruitMesh != null)
                for (int f = 0; f < 6; f++)
                {
                    float a = (float)rnd.NextDouble() * 6.28f, d = 0.7f + (float)rnd.NextDouble() * 0.5f, y = 2.4f + (float)rnd.NextDouble() * 1.4f;
                    Fruit(parent, "Fruit" + f, pos + new Vector3(Mathf.Cos(a) * d, y, Mathf.Sin(a) * d), 0.55f, fruitMesh, fruit,
                        Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 70f + (float)rnd.NextDouble() * 20f));   // short pieces hanging almost upright
                }
            // T67: two flowers grow on the culms like the ones on the vine bridges (one white with mist-blue inner petals, one yellow), facing outwards
            var blue = _flowerBlue != null ? _flowerBlue : _flowerBlue = RuntimeMats.NewLit("M_FlowerBlue", new Color(0.75f, 0.85f, 0.95f));
            var tall = new List<(Transform t, float h)>(culms); tall.Sort((x, y) => y.h.CompareTo(x.h));
            for (int fl = 0; fl < 2 && fl < tall.Count; fl++)
            {
                var (ct, ch) = tall[fl + 1 < tall.Count ? fl + 1 : fl];   // skip the very tallest so the flower sits at eye level
                var on = ct.TransformPoint(new Vector3(0, ch * (0.5f + 0.1f * fl), 0));
                var outward = new Vector3(on.x - pos.x, 0, on.z - pos.z); outward = outward.sqrMagnitude < 0.01f ? new Vector3(1, 0, 0) : outward.normalized;
                var axis = (outward + Vector3.up * 0.35f).normalized;
                var q = Quaternion.FromToRotation(Vector3.up, axis);
                var fc = on + axis * 0.18f;
                Material outer = fl == 0 ? _white : _yellow, inner = fl == 0 ? blue : _white;
                Prim("FlowerStalk" + fl, PrimitiveType.Cylinder, parent, on + axis * 0.09f, new Vector3(0.04f, 0.09f, 0.04f), light, false).transform.rotation = q;
                for (int pt = 0; pt < 8; pt++)
                {
                    float pa = pt * 0.785f;
                    var petal = Prim("Petal", PrimitiveType.Sphere, parent, fc + q * new Vector3(Mathf.Cos(pa) * 0.3f, 0.05f, Mathf.Sin(pa) * 0.3f), new Vector3(0.55f, 0.04f, 0.16f), outer, false);
                    petal.transform.rotation = q * Quaternion.Euler(0, -pa * Mathf.Rad2Deg, 22f);
                }
                for (int pt = 0; pt < 5; pt++)
                {
                    float pa = pt * 1.257f + 0.4f;
                    var petal = Prim("InnerPetal", PrimitiveType.Sphere, parent, fc + q * new Vector3(Mathf.Cos(pa) * 0.14f, 0.11f, Mathf.Sin(pa) * 0.14f), new Vector3(0.32f, 0.03f, 0.1f), inner, false);
                    petal.transform.rotation = q * Quaternion.Euler(0, -pa * Mathf.Rad2Deg, 40f);
                }
                Prim("FlowerCentre" + fl, PrimitiveType.Sphere, parent, fc + axis * 0.1f, Vector3.one * 0.13f, Kit.Mat("M_Crystal") ?? _white, false);
            }
            Prim("Base", PrimitiveType.Cylinder, parent, pos + Vector3.down * 0.03f, new Vector3(2.6f, 0.05f, 2.6f), PatchBase(foliage), false);
        }

        /// <summary>Dew-seed source: a few big teal succulent rosettes in a nest of stones.</summary>
        public static void SucculentPatch(Transform parent, Vector3 pos, Material fruit, Mesh fruitMesh, System.Random rnd)
        {
            EnsureMaterials();
            var foliage = Mat("M_Foliage");
            Prim("Base", PrimitiveType.Cylinder, parent, pos + Vector3.up * 0.05f, new Vector3(2.4f, 0.05f, 2.4f), PatchBase(foliage), false);
            float[] sc = { 3.2f, 2.2f, 1.7f, 1.3f };
            for (int i = 0; i < sc.Length; i++)
            {
                float a = i * 2.2f + (float)rnd.NextDouble() * 0.6f, d = i == 0 ? 0f : 0.95f + (float)rnd.NextDouble() * 0.2f;
                var p = pos + new Vector3(Mathf.Cos(a) * d, 0.12f + 0.14f * sc[i], Mathf.Sin(a) * d);
                if (fruitMesh != null) Fruit(parent, "Rosette" + i, p, sc[i], fruitMesh, fruit, Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0));
                else Prim("Rosette" + i, PrimitiveType.Sphere, parent, p, new Vector3(sc[i] * 0.5f, sc[i] * 0.25f, sc[i] * 0.5f), fruit, false);
                // T67: dew pooled in the middle of each rosette: a small water disc with a bright dewdrop on it
                var water = Kit.Mat("M_Water");
                if (water != null)
                {
                    Prim("Pool" + i, PrimitiveType.Cylinder, parent, p + Vector3.down * 0.005f * sc[i], new Vector3(0.46f * sc[i], 0.012f, 0.46f * sc[i]), water, false);
                    Prim("Dew" + i, PrimitiveType.Sphere, parent, p + new Vector3(0.06f * sc[i], 0.012f * sc[i], 0.03f * sc[i]), Vector3.one * 0.05f * sc[i], Kit.Mat("M_Crystal") ?? water, false);
                }
            }
            var stone = Kit.Mat("M_Bark");
            for (int i = 0; i < 5; i++)
            {
                float a = i * 1.3f + (float)rnd.NextDouble(), d = 1.5f + (float)rnd.NextDouble() * 0.3f;
                MeshPrim("Stone" + i, DebrisMeshes.Rock(rnd.Next(8)), parent, pos + new Vector3(Mathf.Cos(a) * d, 0.1f, Mathf.Sin(a) * d), new Vector3(0.38f, 0.22f, 0.34f), stone, false);
            }
        }

        /// <summary>Beanstalk patch: three tall bean poles with climbing vines, leaves and hanging bean pods.</summary>
        public static void BeanPatch(Transform parent, Vector3 pos, Material fruit, Mesh fruitMesh, System.Random rnd)
        {
            EnsureMaterials();
            var bark = Kit.Mat("M_Bark"); var foliage = Mat("M_Foliage"); var light = Mat("M_FoliageLight");
            for (int i = 0; i < 4; i++)
            {
                float a = i * 1.57f + (float)rnd.NextDouble() * 0.5f; var o = new Vector3(Mathf.Cos(a) * 0.9f, -0.18f, Mathf.Sin(a) * 0.9f);   // pole feet sunk into the ground
                float h = 2.4f + (float)rnd.NextDouble() * 1.0f;
                Prim("Pole" + i, PrimitiveType.Cylinder, parent, pos + o + Vector3.up * h * 0.5f, new Vector3(0.09f, h * 0.5f, 0.09f), bark, false);
                for (int l = 0; l < 6; l++)   // leaves spiralling up the pole
                {
                    float la = l * 1.9f + i; float y = 0.3f + l * (h - 0.4f) / 6f;
                    Prim("Leaf", PrimitiveType.Sphere, parent, pos + o + new Vector3(Mathf.Cos(la) * 0.22f, y, Mathf.Sin(la) * 0.22f), new Vector3(0.38f, 0.1f, 0.24f), l % 2 == 0 ? foliage : light, false)
                        .transform.rotation = Quaternion.Euler(0, la * Mathf.Rad2Deg, 25f);
                }
                for (int b = 0; b < 3; b++)
                {
                    var pp = pos + o + new Vector3(0.2f, 0.9f + b * 0.55f, 0.05f * b);
                    if (fruitMesh != null) Fruit(parent, "Bean", pp, 0.95f, fruitMesh, fruit, Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 65f + (float)rnd.NextDouble() * 20f));   // beans dangling from the vine
                    else Prim("Pod", PrimitiveType.Capsule, parent, pp, new Vector3(0.07f, 0.16f, 0.07f), fruit != null ? fruit : _bean, false);
                }
            }
            Prim("Base", PrimitiveType.Cylinder, parent, pos + Vector3.down * 0.03f, new Vector3(2.4f, 0.05f, 2.4f), PatchBase(foliage), false);
        }

        /// <summary>Parachute leaf plant: a short pale stalk carrying three huge leaves (the parachute leaves) on curved stems, plus a soft glow so it stands out on the island.</summary>
        public static void LeafPlant(Transform parent, Vector3 pos, Material fruit, System.Random rnd)
        {
            EnsureMaterials();
            var stem = Mat("M_FoliageLight"); var light = Mat("M_FoliageLight"); var dark = Mat("M_Foliage"); var glow = Kit.Mat("M_PortalGlow");
            // T67: a knobbly stalk (stacked tapering segments with node rings), a swollen root and a few leaflets low down
            for (int k = 0; k < 6; k++)
            {
                float w = 0.2f - k * 0.016f;
                Prim("StalkSeg" + k, PrimitiveType.Cylinder, parent, pos + Vector3.up * (0.2f + k * 0.34f), new Vector3(w, 0.19f, w), stem, false);
                if (k > 0) Prim("Node" + k, PrimitiveType.Sphere, parent, pos + Vector3.up * (0.03f + k * 0.34f), new Vector3(w * 1.5f, 0.07f, w * 1.5f), dark, false);
            }
            Prim("Root", PrimitiveType.Sphere, parent, pos + Vector3.up * 0.1f, new Vector3(0.7f, 0.3f, 0.7f), dark, false);
            for (int k = 0; k < 5; k++)
            {
                float la = k * 1.2566f + 0.3f; var lo = new Vector3(Mathf.Cos(la), 0, Mathf.Sin(la));
                var lf = Prim("Leaflet" + k, PrimitiveType.Sphere, parent, pos + lo * 0.5f + Vector3.up * (0.25f + 0.12f * k), new Vector3(0.7f, 0.05f, 0.24f), light, false);
                lf.transform.rotation = Quaternion.AngleAxis(-la * Mathf.Rad2Deg, Vector3.up) * Quaternion.Euler(0, 0, 18f);
            }
            float a0 = (float)rnd.NextDouble() * 6.28f;
            for (int i = 0; i < 3; i++)
            {
                float a = a0 + i * 2.094f + ((float)rnd.NextDouble() - 0.5f) * 0.4f, R = 1.5f + (float)rnd.NextDouble() * 0.4f;
                var outward = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var armEnd = pos + Vector3.up * (1.9f + i * 0.12f) + outward * 0.9f;
                var arm = Prim("Arm" + i, PrimitiveType.Cylinder, parent, (pos + Vector3.up * 1.7f + armEnd) * 0.5f, new Vector3(0.07f, 0.55f, 0.07f), stem, false);
                arm.transform.rotation = Quaternion.FromToRotation(Vector3.up, armEnd - (pos + Vector3.up * 1.7f));
                Prim("Bud" + i, PrimitiveType.Sphere, parent, armEnd, Vector3.one * 0.16f, dark, false);   // T67: knuckle where the arm meets the canopy
                // T67: the plant grows big leaves (not canopies): a blade at the end of the arm, its stem on the near side, tip up and outward
                float L = R * 1.9f, W = R * 1.2f;
                var dirUp = (outward * Mathf.Cos(0.35f) + Vector3.up * Mathf.Sin(0.35f)).normalized;
                var leaf = new GameObject("Leaf" + i); leaf.transform.SetParent(parent, true);
                leaf.transform.position = armEnd + dirUp * (L * 0.5f);
                leaf.transform.rotation = Quaternion.FromToRotation(Vector3.right, dirUp);
                leaf.AddComponent<MeshFilter>().sharedMesh = LeafMesh.Build(L, W, 0.25f, 0.3f, 28);
                leaf.AddComponent<MeshRenderer>().sharedMaterial = Kit.Mat("M_GlideLeaf") ?? light;
                var rib = Prim("Rib" + i, PrimitiveType.Cylinder, parent, armEnd + dirUp * (L * 0.45f), new Vector3(0.05f, L * 0.45f, 0.05f), stem, false);
                rib.transform.rotation = Quaternion.FromToRotation(Vector3.up, dirUp);
            }
            Prim("Halo", PrimitiveType.Cylinder, parent, pos + Vector3.up * 0.04f, new Vector3(2.6f, 0.02f, 2.6f), glow, false);
            var lg = new GameObject("LeafGlow"); lg.transform.SetParent(parent, true); lg.transform.position = pos + Vector3.up * 2.5f;
            var li = lg.AddComponent<Light>(); li.type = LightType.Point; li.color = new Color(0.6f, 1f, 0.7f); li.range = 7f; li.intensity = 1.2f;
        }

        /// <summary>Shelf-seed source: a mossy log with a cluster of white puffball mushrooms of different sizes.</summary>
        public static void ShelfPatch(Transform parent, Vector3 pos, Material fruit, Mesh fruitMesh, System.Random rnd)
        {
            EnsureMaterials();
            var bark = Kit.Mat("M_Bark"); var foliage = Mat("M_Foliage");
            var log = Prim("Log", PrimitiveType.Cylinder, parent, pos + new Vector3(0, 0.38f, 0), new Vector3(0.7f, 1.4f, 0.7f), bark, false);
            log.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 180f, 90f);
            Prim("Moss", PrimitiveType.Sphere, parent, pos + new Vector3(0, 0.62f, 0), new Vector3(1.6f, 0.3f, 0.7f), PatchBase(foliage), false).transform.rotation = log.transform.rotation * Quaternion.Euler(0, 0, -90f) * Quaternion.Euler(0, 0, 90f);
            Prim("Base", PrimitiveType.Cylinder, parent, pos + Vector3.up * 0.05f, new Vector3(3.2f, 0.05f, 3.2f), PatchBase(foliage), false);
            float[] sc = { 2.6f, 2.0f, 1.6f, 1.4f, 1.2f, 1.0f, 0.9f };
            for (int i = 0; i < sc.Length; i++)
            {
                float a = i * 0.9f + (float)rnd.NextDouble() * 0.4f, d = 1.5f + 0.2f * i;
                var p = pos + new Vector3(Mathf.Cos(a) * d, 0.27f * sc[i] + 0.04f, Mathf.Sin(a) * d);
                if (fruitMesh != null) Fruit(parent, "Puffball" + i, p, sc[i], fruitMesh, fruit != null ? fruit : _white, Quaternion.Euler((float)(rnd.NextDouble() - 0.5) * 14f, (float)rnd.NextDouble() * 360f, (float)(rnd.NextDouble() - 0.5) * 14f));
                else Prim("Puffball" + i, PrimitiveType.Sphere, parent, p, Vector3.one * 0.45f * sc[i], fruit != null ? fruit : _white, false);
            }
            // T67: red toadstools with white spots among the puffballs, like the grown mushroom patch
            var red = Kit.Mat("M_BerryRed") ?? _pink;
            float[] rs = { 1.6f, 1.2f, 0.9f };
            for (int i = 0; i < rs.Length; i++)
            {
                float a = 4.1f + i * 1.7f + (float)rnd.NextDouble() * 0.5f, d = 1.3f + 0.25f * i + (float)rnd.NextDouble() * 0.3f, s = rs[i];
                var b = pos + new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
                float capY = 0.62f * s, capR = 0.42f * s, capH = 0.3f * s;     // cap: a domed ellipsoid sitting on the stalk
                Prim("RedStalk" + i, PrimitiveType.Cylinder, parent, b + Vector3.up * 0.33f * s, new Vector3(0.2f * s, 0.33f * s, 0.2f * s), _white, false);
                Prim("RedCap" + i, PrimitiveType.Sphere, parent, b + Vector3.up * capY, new Vector3(capR * 2f, capH * 2f, capR * 2f), red, false);
                for (int k = 0; k < 6; k++)
                {
                    float ka = k * 1.047f + i, kr = k == 5 ? 0f : capR * 0.55f;
                    float ky = capY + capH * Mathf.Sqrt(Mathf.Max(0f, 1f - (kr / capR) * (kr / capR)));
                    var sp = Prim("Spot", PrimitiveType.Sphere, parent, b + new Vector3(Mathf.Cos(ka) * kr, ky - 0.01f * s, Mathf.Sin(ka) * kr), new Vector3(0.16f, 0.06f, 0.16f) * s, _white, false);
                }
            }
        }

        static void Flowers(Transform parent, Vector3 pos, System.Random rnd)
        {
            Material[] mats = { _pink, _purple, _white, _yellow, _purple };
            var m = mats[rnd.Next(mats.Length)];
            var go = new GameObject("Flowers"); go.transform.SetParent(parent, true); go.transform.position = pos;
            if (Kit.Prop("flower_redA") != null)
            {
                // richer clumps: three colours, eight to eleven blooms of varied height and slight lean, a ring of grass and small buds round the base
                string[] cols = { "red", "purple", "yellow" }; string col = cols[rnd.Next(cols.Length)], col2 = cols[rnd.Next(cols.Length)], col3 = cols[rnd.Next(cols.Length)];
                int n = 8 + rnd.Next(4);
                for (int i = 0; i < n; i++)
                {
                    float ang = (float)(rnd.NextDouble() * 6.283), rad = Mathf.Sqrt((float)rnd.NextDouble()) * 0.75f;
                    var o = new Vector3(Mathf.Cos(ang) * rad, 0f, Mathf.Sin(ang) * rad);
                    string c = i % 3 == 2 ? col2 : i % 3 == 1 && i > 3 ? col3 : col;
                    float h = (0.3f + (float)rnd.NextDouble() * 0.55f) * (1.1f - rad * 0.5f);   // taller in the middle of the clump
                    var f = Model(go.transform, "flower_" + c + "ABC"[rnd.Next(3)], pos + o, (float)rnd.NextDouble() * 360f, h, false);
                    if (f != null) f.transform.rotation = Quaternion.Euler(((float)rnd.NextDouble() - 0.5f) * 14f, f.transform.eulerAngles.y, ((float)rnd.NextDouble() - 0.5f) * 14f);
                }
                if (Kit.Prop("grass") != null || Kit.Prop("grass_large") != null)
                    for (int i = 0; i < 3; i++)
                    {
                        float ang = i * 2.1f + (float)rnd.NextDouble(); var o = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * 0.6f;
                        Model(go.transform, FirstAvailable(rnd, "grass", "grass_large"), pos + o, (float)rnd.NextDouble() * 360f, 0.3f + (float)rnd.NextDouble() * 0.2f, false);
                    }
                return;
            }
            var stem = Mat("M_FoliageLight");
            for (int i = 0; i < 5; i++)
            {
                var o = new Vector3((float)(rnd.NextDouble() - 0.5) * 0.9f, 0f, (float)(rnd.NextDouble() - 0.5) * 0.9f);
                float h = 0.35f + (float)rnd.NextDouble() * 0.35f;
                Prim("Stem", PrimitiveType.Cylinder, go.transform, pos + o + Vector3.up * h * 0.5f, new Vector3(0.03f, h * 0.5f, 0.03f), stem, false);
                Prim("Bloom", PrimitiveType.Sphere, go.transform, pos + o + Vector3.up * (h + 0.04f), new Vector3(0.16f, 0.12f, 0.16f), m, false);
            }
        }

        /// <summary>White carved pillar: base block, fluted shaft, cap, a gold rune ring. Broken ones are short with a chipped top.</summary>
        static void Pillar(Transform parent, Vector3 pos, float h, bool broken, System.Random rnd)
        {
            var go = new GameObject(broken ? "Pillar_Broken" : "Pillar"); go.transform.SetParent(parent, true); go.transform.position = pos;
            if (Kit.Prop("AP4_A1_StoneWall1") != null)   // FreeStylized ruins: tall wall for pillars, low broken walls otherwise
            {
                string pn = broken ? FirstAvailable(rnd, "AP4_A1_StoneWall5", "AP4_A2_StoneWall4", "AP4_A2_StoneWall6", "AP4_A1_StoneWall3")
                                   : FirstAvailable(rnd, "AP4_A1_StoneWall1", "AP4_A2_StoneWall2");
                if (Model(go.transform, pn, pos, (float)rnd.NextDouble() * 360f, 0f, true) != null) return;
            }
            if (Kit.Prop("pillar-stone") != null)
            {
                var pm = Model(go.transform, "pillar-stone", pos, (float)rnd.NextDouble() * 360f, h, true);
                if (pm != null) return;
            }
            Prim("Base", PrimitiveType.Cube, go.transform, pos + Vector3.up * 0.15f, new Vector3(1.1f, 0.3f, 1.1f), _marble, true);
            var shaft = Prim("Shaft", PrimitiveType.Cube, go.transform, pos + Vector3.up * (0.3f + h * 0.5f), new Vector3(0.75f, h, 0.75f), _marble, true);
            if (broken) shaft.transform.rotation = go.transform.rotation * Quaternion.Euler(0, 0, (float)(rnd.NextDouble() - 0.5) * 8f);
            if (!broken)
            {
                Prim("Cap", PrimitiveType.Cube, go.transform, pos + Vector3.up * (0.3f + h + 0.12f), new Vector3(1.05f, 0.25f, 1.05f), _marble, false);
                var ring = Prim("Rune", PrimitiveType.Cylinder, go.transform, pos + Vector3.up * (0.3f + h * 0.62f), new Vector3(0.82f, 0.05f, 0.82f), _gold, false);
                Prim("Rune2", PrimitiveType.Cylinder, go.transform, pos + Vector3.up * (0.3f + h * 0.8f), new Vector3(0.8f, 0.03f, 0.8f), _gold, false);
            }
        }

        static void Slabs(Transform parent, Vector3 pos, System.Random rnd)
        {
            if (Kit.Prop("AP4_A3_LongStone1") != null)
            {
                string[] bits = { "AP4_A3_LongStone1", "AP4_A3_LongStone2", "AP4_A2_Stone1", "AP4_A2_Stone2", "AP4_A2_Stone3", "AP4_A2_Stone5", "AP4_A3_StoneRing1", "AP4_A2_Stone6" };
                for (int i = 0; i < 5; i++)
                {
                    var o = new Vector3((float)(rnd.NextDouble() - 0.5) * 3.6f, 0f, (float)(rnd.NextDouble() - 0.5) * 3.6f);
                    Model(parent, bits[rnd.Next(bits.Length)], pos + o, (float)rnd.NextDouble() * 360f, 0f, true);
                }
                return;
            }
            if (Kit.Prop("wall-broken") != null)
            {
                string[] bits = { "wall-block-half", "wall-broken", "stairs-stone", "wall-half", "rock-small" };
                for (int i = 0; i < 3; i++)
                {
                    var o = new Vector3((float)(rnd.NextDouble() - 0.5) * 3.2f, 0f, (float)(rnd.NextDouble() - 0.5) * 3.2f);
                    Model(parent, bits[rnd.Next(bits.Length)], pos + o, (float)rnd.NextDouble() * 360f, 0.7f + (float)rnd.NextDouble() * 0.5f, true);
                }
                return;
            }
            for (int i = 0; i < 6; i++)
            {
                var o = new Vector3((float)(rnd.NextDouble() - 0.5) * 3.2f, 0.07f, (float)(rnd.NextDouble() - 0.5) * 3.2f);
                var s = Prim("Slab", PrimitiveType.Cube, parent, pos + o, new Vector3(0.7f + (float)rnd.NextDouble() * 0.5f, 0.14f, 0.5f + (float)rnd.NextDouble() * 0.3f), _marble, false);
                s.transform.rotation = Quaternion.Euler((float)(rnd.NextDouble() - 0.5) * 12f, (float)rnd.NextDouble() * 180f, (float)(rnd.NextDouble() - 0.5) * 12f);
                if (i % 3 == 0) Prim("Glyph", PrimitiveType.Cylinder, parent, pos + o + Vector3.up * 0.08f, new Vector3(0.28f, 0.01f, 0.28f), _gold, false);
            }
        }

        static Material _portalDisc;
        /// <summary>Translucent copy of the portal glow, so the view through the arch stays visible.</summary>
        static Texture2D _crackTex;
        /// <summary>Glass with a crack pattern: pale translucent pane, bright thin cracks radiating from an impact point plus a few ring cracks. Alpha carries the pattern.</summary>
        static Texture2D CrackTexture()
        {
            if (_crackTex != null) return _crackTex;
            const int W = 256, H = 512;
            var px = new Color32[W * H];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(235, 250, 255, 120);   // the pane: 47 % opaque
            var rnd = new System.Random(77);
            void Dot(int x, int y, int r, byte a)
            {
                for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++)
                {
                    int xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= W || yy >= H) continue;
                    var c = px[yy * W + xx]; if (c.a < a) px[yy * W + xx] = new Color32(255, 255, 255, a);
                }
            }
            void Line(Vector2 p0, Vector2 p1, int r, byte a)
            {
                int n = Mathf.CeilToInt((p1 - p0).magnitude);
                for (int i = 0; i <= n; i++) { var q = Vector2.Lerp(p0, p1, i / (float)n); Dot(Mathf.RoundToInt(q.x), Mathf.RoundToInt(q.y), r, a); }
            }
            var hit = new Vector2(W * 0.4f, H * 0.42f);
            for (int k = 0; k < 13; k++)   // radial cracks that kink and fork
            {
                float ang = k * 6.283f / 13f + (float)rnd.NextDouble() * 0.3f, len = 90f + (float)rnd.NextDouble() * 260f;
                var p = hit;
                for (int seg = 0; seg < 6 && len > 0f; seg++)
                {
                    float step = Mathf.Min(len, 25f + (float)rnd.NextDouble() * 45f); len -= step;
                    ang += ((float)rnd.NextDouble() - 0.5f) * 0.5f;
                    var q = p + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * step;
                    Line(p, q, seg < 2 ? 1 : 0, 235); p = q;
                    if (seg == 2 && rnd.NextDouble() < 0.6) { float fa = ang + (rnd.NextDouble() < 0.5 ? 0.7f : -0.7f); Line(p, p + new Vector2(Mathf.Cos(fa), Mathf.Sin(fa)) * (30f + (float)rnd.NextDouble() * 50f), 0, 215); }
                }
            }
            for (int ring = 1; ring <= 3; ring++)   // ring cracks joining the radials, broken into short arcs
            {
                float rad = ring * 38f + (float)rnd.NextDouble() * 8f;
                for (int a0 = 0; a0 < 360; a0 += 12)
                {
                    if (rnd.NextDouble() < 0.4) continue;
                    var p0 = hit + new Vector2(Mathf.Cos(a0 * Mathf.Deg2Rad), Mathf.Sin(a0 * Mathf.Deg2Rad)) * rad;
                    var p1 = hit + new Vector2(Mathf.Cos((a0 + 12) * Mathf.Deg2Rad), Mathf.Sin((a0 + 12) * Mathf.Deg2Rad)) * (rad + (float)rnd.NextDouble() * 4f);
                    Line(p0, p1, 0, 200);
                }
            }
            for (int i = 0; i < 6; i++) Dot(Mathf.RoundToInt(hit.x + ((float)rnd.NextDouble() - 0.5f) * 14f), Mathf.RoundToInt(hit.y + ((float)rnd.NextDouble() - 0.5f) * 14f), 2, 255);   // the impact point
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, true) { name = "T_CrackedGlass", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            tex.SetPixels32(px); tex.Apply();
            return _crackTex = tex;
        }

        static Material PortalDiscMat()
        {
            if (_portalDisc != null) return _portalDisc;
            var m = new Material(_glow) { name = "M_PortalDisc" };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.78f, 0.97f, 0.95f, 1f));   // alpha comes from the cracked glass texture
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", CrackTexture());
            RuntimeMats.SetTransparent(m);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.9f);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", m.GetColor("_EmissionColor") * 0.3f);
            return _portalDisc = m;
        }

        /// <summary>Portal arch: two pillars, a lintel, a glowing disc in the middle and steps. Faces `rot` forward.</summary>
        /// <param name="gate">the goal gate: no permanent glowing disc (the gate builds its own portal that opens on use)</param>
        /// <summary>Position and rotation of the temple arch on the island being dressed; the vine dressing keeps clear of the opening.</summary>
        internal static readonly List<(Vector3 pos, Quaternion rot)> ArchSpots = new List<(Vector3, Quaternion)>();

        /// <summary>How far the arch and the goal gate sink into the terrain (the gate builder lowers its doors and portal by the same amount).</summary>
        public const float ArchSink = 0.35f;

        public static GameObject Arch(Transform parent, Vector3 pos, Quaternion rot, bool gate = false)
        {
            EnsureMaterials();
            if (!gate) ArchSpots.Add((pos, rot));
            pos.y -= ArchSink;
            var go = new GameObject("Arch"); go.transform.SetParent(parent, true); go.transform.SetPositionAndRotation(pos, rot);
            Vector3 P(float x, float y, float z) => pos + rot * new Vector3(x, y, z);
            void Part(string n, PrimitiveType t, Vector3 lp, Vector3 sc, Material m, bool col, Quaternion? r = null)
            {
                var g = Prim(n, t, go.transform, P(lp.x, lp.y, lp.z), sc, m, col);
                g.transform.rotation = rot * (r ?? Quaternion.identity);
            }
            Part("PillarL", PrimitiveType.Cube, new Vector3(-2.1f, 2.1f, 0), new Vector3(0.9f, 4.2f, 0.9f), _marble, true);
            Part("PillarR", PrimitiveType.Cube, new Vector3(2.1f, 2.1f, 0), new Vector3(0.9f, 4.2f, 0.9f), _marble, true);
            Part("Lintel", PrimitiveType.Cube, new Vector3(0, 4.45f, 0), new Vector3(5.6f, 0.8f, 1.1f), _marble, true);
            Part("Crest", PrimitiveType.Cube, new Vector3(0, 5.1f, 0), new Vector3(2.4f, 0.55f, 0.9f), _marble, false);
            if (!gate)   // the glow screen fills the arch's clear opening exactly, so no stone or vine of the model pokes through it
            {
                var scr = ShapeMeshes.Obj("Disc", ShapeMeshes.OpeningPanel(), PortalDiscMat(), go.transform, Vector3.zero, Vector3.zero, Vector3.one);
                scr.transform.SetPositionAndRotation(P(0, 0, 0.0f), rot);
                // the transparent window is solid: an invisible wall fills the opening between the pillars and under the lintel
                var wall = new GameObject("WindowCollider"); wall.transform.SetParent(go.transform, true);
                wall.transform.SetPositionAndRotation(P(0, 2.03f, 0), rot);
                var wc = wall.AddComponent<BoxCollider>(); wc.size = new Vector3(3.4f, 4.1f, 0.5f);
                int wl = LayerMask.NameToLayer(Layers.GroundName); if (wl >= 0) wall.layer = wl;
            }
            foreach (float tz in new[] { 0.2f, -0.4f })   // gold trim inlaid along the opening on both faces
            {
                var trim = ShapeMeshes.Obj("OpeningTrim", ShapeMeshes.Tube(ShapeMeshes.OpeningTrim(0.03f, 0f), q => 0.035f, 6), _gold, go.transform, Vector3.zero, Vector3.zero, Vector3.one);
                trim.transform.SetPositionAndRotation(P(0, 0, tz), rot);
            }
            // crumbling steps: a few chunks per step, each with its own offset, yaw and tilt, some missing. The gate keeps them small and worn,
            // the transparent arch is much more broken (fewer chunks, wider gaps, bigger tilts)
            var srnd = new System.Random(Mathf.RoundToInt(pos.x * 31f + pos.z * 17f) ^ 0x5eed);
            void Steps(string n, float top, float width, float depth, float z, int chunks, float skip, float tilt)
            {
                float w = width / chunks;
                for (int i = 0; i < chunks; i++)
                {
                    if (srnd.NextDouble() < skip) continue;
                    float cw = w * (0.55f + (float)srnd.NextDouble() * 0.4f), cd = depth * (0.6f + (float)srnd.NextDouble() * 0.4f);
                    float x = -width * 0.5f + w * (i + 0.5f) + ((float)srnd.NextDouble() - 0.5f) * w * 0.3f;
                    float drop = (float)srnd.NextDouble() * 0.08f * (1f + tilt / 8f);
                    var tilted = Quaternion.Euler(((float)srnd.NextDouble() - 0.5f) * tilt, ((float)srnd.NextDouble() - 0.5f) * tilt * 2f, ((float)srnd.NextDouble() - 0.5f) * tilt);
                    Part(n + i, PrimitiveType.Cube, new Vector3(x, top - 0.1f - drop + ArchSink, z + ((float)srnd.NextDouble() - 0.5f) * 0.25f), new Vector3(cw, 0.2f, cd), _marble, true, tilted);
                }
            }
            if (gate)
            {
                Steps("Step1_", 0.14f, 3.6f, 0.9f, 1.0f, 4, 0.12f, 6f);
                Steps("Step2_", 0.3f, 3.0f, 0.8f, 0.5f, 3, 0.25f, 8f);
            }
            else
            {
                Steps("Step1_", 0.1f, 4.2f, 0.9f, 1.15f, 5, 0.4f, 16f);
                Steps("Step2_", 0.24f, 3.4f, 0.8f, 0.55f, 3, 0.5f, 20f);
            }
            var door = Model(go.transform, "AP4_A3_Door1", pos, rot.eulerAngles.y + 90f, 0f, false);
            if (door != null)   // stone arch model replaces the cube frame; the cubes stay as invisible colliders
            {
                foreach (var n in new[] { "PillarL", "PillarR", "Lintel" }) { var t = go.transform.Find(n); if (t != null) t.GetComponent<Renderer>().enabled = false; }
                var crest = go.transform.Find("Crest"); if (crest != null) Object.Destroy(crest.gameObject);
            }
            if (door == null)
            {
                Part("RuneL", PrimitiveType.Cylinder, new Vector3(-2.1f, 1.5f, 0), new Vector3(0.95f, 0.04f, 0.95f), _gold, false);
                Part("RuneR", PrimitiveType.Cylinder, new Vector3(2.1f, 1.5f, 0), new Vector3(0.95f, 0.04f, 0.95f), _gold, false);
            }
            return go;
        }

        /// <summary>
        /// Pushes the baked sky debris (FloatingRocks, Islets under the Atmosphere) away from the islands of the level just generated and
        /// from the corridors between linked islands, so nothing floats next to the walkable space. Pieces that cannot be moved clear are hidden.
        /// Runs from the world generator (Awake), before FloatBob records its base position.
        /// </summary>
        public static void KeepDebrisClear(IList<Vector3> centres, IList<float> radii, IList<Vector2Int> links)
        {
            const float corridor = 8f;
            foreach (var rootName in new[] { "FloatingRocks", "Islets" })
            {
                var root = GameObject.Find(rootName); if (root == null) continue;
                float margin = rootName == "Islets" ? 28f : 18f;
                foreach (Transform t in root.transform)
                {
                    var rs = t.GetComponentsInChildren<Renderer>(); float ext = 1f;
                    if (rs.Length > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); ext = Mathf.Max(b.extents.x, b.extents.z); }
                    var p = t.position; bool ok = false;
                    for (int it = 0; it < 8 && !ok; it++)
                    {
                        float need = margin + ext, best = float.MaxValue; var away = Vector3.zero;
                        void Consider(Vector3 nearest, float clear)
                        {
                            var d = new Vector3(p.x - nearest.x, 0, p.z - nearest.z); float dist = d.magnitude;
                            if (p.y - nearest.y < -35f || p.y - nearest.y > 30f) return;   // far above / below the island: fine
                            float gap = dist - clear;
                            if (gap < best) { best = gap; away = dist > 0.01f ? d / dist : Vector3.right; }
                        }
                        for (int i = 0; i < centres.Count; i++) Consider(centres[i], radii[i]);
                        foreach (var l in links)
                        {
                            Vector3 a = centres[l.x], c = centres[l.y]; var ab = new Vector2(c.x - a.x, c.z - a.z);
                            float u = ab.sqrMagnitude < 0.01f ? 0f : Mathf.Clamp01(Vector2.Dot(new Vector2(p.x - a.x, p.z - a.z), ab) / ab.sqrMagnitude);
                            Consider(Vector3.Lerp(a, c, u), corridor);
                        }
                        if (best >= need) { ok = true; break; }
                        p += away * (need - best + 1f);
                    }
                    t.position = p;
                    if (!ok) t.gameObject.SetActive(false);
                }
            }
        }

        // ------------------------------------------------------------------ sky debris from the imported kits
        static readonly string[] DebrisRocks = { "Rock_Medium_1", "Rock_Medium_2", "Rock_Medium_3", "rock_smallA", "rock_smallB", "rock_tallA", "stone_largeB", "stone_smallC" };
        static readonly string[] DebrisRuins = { "AP4_A1_StoneWall3", "AP4_A1_StoneWall5", "AP4_A2_StoneWall4", "AP4_A2_StoneWall6", "AP4_A3_DoorStone1", "AP4_A3_DoorStone2", "AP4_A3_LongStone1", "AP4_A2_Stone1", "AP4_A2_Stone2" };

        /// <summary>A floating imported rock / ruin chunk scaled by `s`, centred on `pos` and tumbled; null if the kit lacks the models.</summary>
        static GameObject ImportedDebris(Transform parent, System.Random rnd, Vector3 pos, float s, string[] names, float scale)
        {
            var g = Model(parent, FirstAvailable(rnd, names), pos, (float)rnd.NextDouble() * 360f, 0f, false);
            if (g == null) return null;
            g.transform.localScale *= s * scale;
            g.transform.position = pos;   // Model stands the piece on pos; recentre it so it floats around pos
            var rs = g.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            g.transform.position += pos - b.center;
            g.transform.rotation = Quaternion.Euler((float)rnd.NextDouble() * 50f - 25f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 50f - 25f);
            return g;
        }

        // ------------------------------------------------------------------ sky
        /// <summary>Floating rocks and small tree-topped islets scattered around the play space (children of `parent`).</summary>
        public static void SkyDebris(Transform parent)
        {
            EnsureMaterials();
            var stone = Kit.Mat("M_Stone"); var terrain = Kit.Mat("M_Terrain");
            var rnd = new System.Random(2026);
            var rocks = new GameObject("FloatingRocks"); rocks.transform.SetParent(parent, false);
            var crystalMat = CrystalMat();
            for (int i = 0; i < 70; i++)
            {
                float a = (float)(rnd.NextDouble() * Mathf.PI * 2), r = 22f + (float)rnd.NextDouble() * 150f;
                var pos = new Vector3(Mathf.Cos(a) * r, -15f + (float)rnd.NextDouble() * 80f, Mathf.Sin(a) * r);
                float s = 0.6f + Mathf.Pow((float)rnd.NextDouble(), 2f) * 4f;
                double kind = rnd.NextDouble();
                GameObject g = null;
                if (kind < 0.42 && rnd.NextDouble() < 0.55) g = ImportedDebris(rocks.transform, rnd, pos, s, DebrisRocks, 0.7f);
                else if (kind >= 0.58 && kind < 0.72 && rnd.NextDouble() < 0.75) g = ImportedDebris(rocks.transform, rnd, pos, s, DebrisRuins, 0.55f);
                if (g != null) { }
                else if (kind < 0.42)       // jagged rock, squashed or stretched
                {
                    g = new GameObject("Rock" + i); g.transform.SetParent(rocks.transform, true); g.transform.position = pos;
                    g.AddComponent<MeshFilter>().sharedMesh = DebrisMeshes.Rock(rnd.Next(8), 3);
                    g.AddComponent<MeshRenderer>().sharedMaterial = OrganicProps.RockMat() ?? stone;
                    g.transform.localScale = new Vector3(s * (0.8f + (float)rnd.NextDouble() * 0.9f), s * (0.5f + (float)rnd.NextDouble() * 0.9f), s * (0.8f + (float)rnd.NextDouble() * 0.9f));
                }
                else if (kind < 0.58)       // glowing crystal cluster: one big shard and two small ones leaning out
                {
                    g = new GameObject("Crystals" + i); g.transform.SetParent(rocks.transform, true); g.transform.position = pos;
                    for (int c = 0; c < 3; c++)
                    {
                        var shard = new GameObject("Shard" + c); shard.transform.SetParent(g.transform, false);
                        shard.AddComponent<MeshFilter>().sharedMesh = DebrisMeshes.Crystal(rnd.Next(4));
                        shard.AddComponent<MeshRenderer>().sharedMaterial = crystalMat;
                        float k = c == 0 ? 1f : 0.55f;
                        shard.transform.localPosition = c == 0 ? Vector3.zero : new Vector3(c == 1 ? 0.7f : -0.6f, -0.2f, c == 1 ? 0.3f : -0.5f) * s;
                        shard.transform.localScale = Vector3.one * s * k * 0.9f;
                        shard.transform.localRotation = Quaternion.Euler(c == 0 ? 0f : (c == 1 ? 24f : -28f), c * 120f, c == 0 ? 0f : (c == 1 ? -20f : 22f));
                    }
                }
                else if (kind < 0.72)       // broken pillar drum with a chunk beside it
                {
                    g = new GameObject("Pillar" + i); g.transform.SetParent(rocks.transform, true); g.transform.position = pos;
                    Prim("Drum", PrimitiveType.Cylinder, g.transform, pos, new Vector3(s * 0.55f, s * (0.7f + (float)rnd.NextDouble()), s * 0.55f), _marble, false);
                    var chunk = MeshPrim("Chunk", DebrisMeshes.Tile(i), g.transform, pos + new Vector3(s * 0.9f, -s * 0.5f, s * 0.2f), new Vector3(s * 0.5f, s * 0.35f, s * 0.45f), _marble, false);
                    chunk.transform.rotation = Random.rotationUniform;
                    g.transform.rotation = Quaternion.Euler((float)rnd.NextDouble() * 40f - 20f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 40f - 20f);
                }
                else if (kind < 0.84)       // flat carved slab
                {
                    g = MeshPrim("Slab" + i, DebrisMeshes.Tile(i), rocks.transform, pos, new Vector3(s * 1.6f, s * 0.22f, s * (0.9f + (float)rnd.NextDouble() * 0.5f)), _marble, false);
                    g.transform.rotation = Quaternion.Euler((float)rnd.NextDouble() * 30f - 15f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 30f - 15f);
                }
                else if (kind < 0.93)       // floating log
                {
                    g = Prim("Log" + i, PrimitiveType.Capsule, rocks.transform, pos, new Vector3(s * 0.35f, s * 1.1f, s * 0.35f), Kit.Mat("M_Bark"), false);
                    g.transform.rotation = Quaternion.Euler(80f + (float)rnd.NextDouble() * 20f, (float)rnd.NextDouble() * 360f, 0f);
                }
                else                        // tiny rock with a bush on top
                {
                    g = new GameObject("RockBush" + i); g.transform.SetParent(rocks.transform, true); g.transform.position = pos;
                    var rock = new GameObject("Rock"); rock.transform.SetParent(g.transform, false);
                    rock.AddComponent<MeshFilter>().sharedMesh = DebrisMeshes.Rock(rnd.Next(8), 3); rock.AddComponent<MeshRenderer>().sharedMaterial = OrganicProps.RockMat() ?? stone;
                    rock.transform.localScale = new Vector3(s * 1.3f, s * 0.7f, s * 1.1f);
                    MeshPrim("MossCap", DebrisMeshes.Rock(rnd.Next(8)), g.transform, pos + Vector3.up * s * 0.6f, new Vector3(s * 0.9f, s * 0.35f, s * 0.8f), Mat("M_Foliage"), false);
                }
                var b = g.AddComponent<FloatBob>(); b.amplitude = 0.7f + s * 0.25f; b.period = 4f + (float)rnd.NextDouble() * 4f; b.wobble = 6f;
                b.spin = ((float)rnd.NextDouble() < 0.5f ? -1f : 1f) * (5f + (float)rnd.NextDouble() * 14f);
                foreach (var mr in g.GetComponentsInChildren<MeshRenderer>()) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var islets = new GameObject("Islets"); islets.transform.SetParent(parent, false);
            for (int i = 0; i < 10; i++)
            {
                float a = (float)(rnd.NextDouble() * Mathf.PI * 2), r = 70f + (float)rnd.NextDouble() * 120f;
                var pos = new Vector3(Mathf.Cos(a) * r, 5f + (float)rnd.NextDouble() * 60f, Mathf.Sin(a) * r);
                var isl = RoundIsland.Create("Islet" + i, pos, 4f + (float)rnd.NextDouble() * 3f, 5f, terrain, stone, 300 + i, 4f);
                foreach (var c in isl.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                isl.transform.SetParent(islets.transform, true);
                Tree(isl.transform, pos + new Vector3(0.5f, 0, 0.3f), 2.2f + (float)rnd.NextDouble() * 1.5f, rnd);
                float ir = 1.6f + (float)rnd.NextDouble() * 1.2f;   // small unreachable islet: bushes, rocks and now and then a ruin on top
                for (int d = 0; d < 3; d++)
                {
                    float da = (float)(rnd.NextDouble() * Mathf.PI * 2);
                    var dp = pos + new Vector3(Mathf.Cos(da), 0, Mathf.Sin(da)) * ir;
                    if (d == 0) Model(isl.transform, FirstAvailable(rnd, "Bush_Common_Flowers", "Bush_Common", "Fern_1", "plant_bush"), dp, da * 57f, 0.9f, false);
                    else Model(isl.transform, FirstAvailable(rnd, DebrisRocks), dp, da * 57f, 0.5f + (float)rnd.NextDouble() * 0.5f, false);
                }
                if (i % 3 == 0) Model(isl.transform, "AP4_A2_StoneWall4", pos + new Vector3(-1.4f, 0, -1.2f), (float)rnd.NextDouble() * 360f, 0f, false);
                if (i % 5 == 0)   // a large grey rock with a green moss cap where the arch used to stand
                {
                    float rs2 = 1.8f + (float)rnd.NextDouble() * 0.6f; var rp = pos + new Vector3(-0.2f, rs2 * 0.3f, 1.6f);
                    MeshPrim("BigRock", DebrisMeshes.Rock(rnd.Next(8), 3), isl.transform, rp, new Vector3(rs2 * 1.3f, rs2 * 0.9f, rs2 * 1.1f), OrganicProps.RockMat() ?? stone, false);
                    MeshPrim("MossCap", DebrisMeshes.Rock(rnd.Next(8)), isl.transform, rp + Vector3.up * rs2 * 0.5f, new Vector3(rs2 * 0.95f, rs2 * 0.4f, rs2 * 0.85f), Mat("M_Foliage"), false);
                }
                foreach (var c in isl.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);   // islets stay out of reach
                var b = isl.AddComponent<FloatBob>(); b.amplitude = 0.8f; b.period = 8f + i; b.spin = 1.5f;
                if (i % 2 == 1) Object.DestroyImmediate(isl);   // half as many islets (made and dropped, so the random stream and the rest stay put)
            }
        }
    }
}
