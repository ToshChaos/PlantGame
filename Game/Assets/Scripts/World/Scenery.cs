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
            if (bigTrees > 0) Scatter(bigTrees, 80, 0.45f, 0.85f, 8f, 6f, p => Tree(root.transform, World(p), 12f + (float)rnd.NextDouble() * 3f, rnd, PickKind(weights, rnd)), 6f);   // T38: normal trees are 2x, big ones stay above them
            if (index == HugeTreeIsland) Scatter(1, 120, 0.4f, 0.8f, 10f, 8f, p => Tree(root.transform, World(p), 20f, rnd, PickKind(weights, rnd)), 8f);   // one tree three times a normal one
            Scatter(Count(14), 160, 0.6f, 0.92f, 4.5f, 3.2f, p => Tree(root.transform, World(p), 4.8f + (float)rnd.NextDouble() * 4.0f, rnd, PickKind(weights, rnd)), 3.2f);
            Scatter(Count(10), 120, 0.35f, 0.92f, 1.8f, 1.2f, p => Bush(root.transform, World(p), 0.7f + (float)rnd.NextDouble() * 0.7f, rnd), 1.2f);
            Scatter(Count(16), 200, 0.25f, 0.92f, 1.2f, 0.9f, p => Flowers(root.transform, World(p), rnd), 0.7f);

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
                SideDecor(isle, centre, index, vegetation, edge(0f));
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
        public static void SideDecor(Transform isle, Vector3 centre, int index, float lush, float rimRadius)
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
            int nr = Mathf.RoundToInt((7 + rnd.Next(7)) * size);
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
        }

        // ------------------------------------------------------------------ leaf crowns
        static readonly Dictionary<string, Material> LeafMats = new Dictionary<string, Material>();

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
            var marble = Kit.Mat("M_Marble") ?? _marble; var stone = Kit.Mat("M_Stone"); var crystal = Kit.Mat("M_Crystal");
            Material brick = Kit.Mat("M_Brick") ?? Kit.Mat("M_TerrainMud") ?? stone;
            // chips of white marble and chunks of old brick scattered round every ruin
            foreach (var rs in ruinSpots)
                for (int k = 0, n = 7 + rnd.Next(7); k < n; k++)
                {
                    float a = (float)(rnd.NextDouble() * 6.283), r = 0.6f + (float)rnd.NextDouble() * 2.6f;
                    var p = new Vector2(rs.x + Mathf.Cos(a) * r, rs.z + Mathf.Sin(a) * r);
                    if (Vector2.Distance(p, c2) > edge(Mathf.Atan2(p.y - c2.y, p.x - c2.x)) * 0.93f || !free(p, 0.3f)) continue;
                    bool isBrick = rnd.NextDouble() < 0.3; float sz = 0.08f + (float)rnd.NextDouble() * 0.2f;
                    var g = MeshPrim(isBrick ? "Brick" : "MarbleChip", DebrisMeshes.Rock(rnd.Next(8)), root.transform, new Vector3(p.x, Y(p) + sz * 0.15f, p.y),
                                     isBrick ? new Vector3(sz * 2.2f, sz * 1.1f, sz * 1.2f) : new Vector3(sz * 1.5f, sz * 0.7f, sz * 1.1f), isBrick ? brick : marble, false);
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
                var sp = gem.AddComponent<GemSparkle>(); sp.glow = rnd.NextDouble() < 0.3 ? new Color(1f, 0.8f, 0.4f) : new Color(0.49f, 0.95f, 0.9f);
            }
            // shiny debris outside the island: chips of marble and gems hovering just past the rim and under it
            for (int k = 0, n = 6 + rnd.Next(6); k < n; k++)
            {
                float a = (float)(rnd.NextDouble() * 6.283), r = edge(a) + 1.2f + (float)rnd.NextDouble() * 5f;
                var pos = new Vector3(centre.x + Mathf.Cos(a) * r, centre.y + IslandTerrain.Shoulder - 5f + (float)rnd.NextDouble() * 7f, centre.z + Mathf.Sin(a) * r);
                float sz = 0.15f + (float)rnd.NextDouble() * 0.35f; bool gem = rnd.NextDouble() < 0.5;
                var g = MeshPrim(gem ? "FloatGem" : "FloatChip", gem ? DebrisMeshes.Crystal(rnd.Next(4)) : DebrisMeshes.Rock(rnd.Next(8)), root.transform, pos,
                                 gem ? new Vector3(sz, sz * 1.5f, sz) : new Vector3(sz * 1.6f, sz * 0.8f, sz * 1.2f), gem ? crystal : marble, false);
                var b = g.AddComponent<FloatBob>(); b.amplitude = 0.25f + sz; b.period = 3f + (float)rnd.NextDouble() * 4f; b.wobble = 10f; b.spin = (rnd.NextDouble() < 0.5 ? -1f : 1f) * (10f + (float)rnd.NextDouble() * 30f);
                if (gem) g.AddComponent<GemSparkle>().baseLevel = 0.6f;
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
        // The small Kenney rocks have no usable material of their own (they came out orange and green): they are shown as golden-orange jewels instead.
        static bool IsJewelModel(string n) => n == "rock_smallA" || n == "rock_smallB" || n == "stone_smallC";
        static Material _jewel;
        static Material JewelMat()
        {
            if (_jewel != null) return _jewel;
            var m = RuntimeMats.NewLit("M_Jewel", new Color(1f, 0.62f, 0.12f, 0.62f));
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.92f);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.1f);
            RuntimeMats.SetTransparent(m);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(1f, 0.5f, 0.08f) * 0.55f);
            return _jewel = m;
        }

        /// <summary>
        /// Places an imported prop (Kit.Prop) standing on `pos`, scaled so it is `height` m tall (0 = native size), turned by `yaw`.
        /// Optional box collider on the Ground layer. Returns null when the kit has no such model, so callers fall back to primitives.
        /// </summary>
        static GameObject Model(Transform parent, string name, Vector3 pos, float yaw, float height, bool collide)
        {
            var prefab = Kit.Prop(name); if (prefab == null) return null;
            var go = Object.Instantiate(prefab, pos, Quaternion.Euler(0, yaw, 0), parent); go.name = name;
            if (IsJewelModel(name)) { if (height > 0f) height *= 0.6f; else go.transform.localScale *= 0.6f; foreach (var jr in go.GetComponentsInChildren<MeshRenderer>()) { var jm = new Material[jr.sharedMaterials.Length]; for (int q = 0; q < jm.Length; q++) jm[q] = JewelMat(); jr.sharedMaterials = jm; jr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; } }
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

        static readonly string[] TreeModel = { "tree_oak", "tree_pineTallA", "tree_thin", "tree_fat", "tree_default", "tree_cone", "tree_oak" };

        // Quaternius Stylized Nature MegaKit (CC0): textured trees; used when present and the theme is not autumn
        static readonly string[][] QuaterniusTrees =
        {
            new[] { "CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "CommonTree_5" },   // oak
            new[] { "Pine_1", "Pine_2", "Pine_3", "Pine_4", "Pine_5" },                                 // pine
            new[] { "CommonTree_2", "CommonTree_4" },                                                   // birch
            new[] { "CommonTree_1", "CommonTree_3" },                                                   // blossom
            new[] { "TwistedTree_1", "TwistedTree_2", "TwistedTree_3", "TwistedTree_4", "TwistedTree_5" }, // willow
            new[] { "Pine_2", "Pine_4" },                                                               // cypress
            null,                                                                                       // autumn: Kenney _fall models
        };

        static readonly string[] PalmModels = { "tree_palmTall", "tree_palmDetailedTall", "tree_palmBend", "tree_palm", "tree_palmDetailedShort", "tree_palmShort" };

        static string ThemedTree(int kind, System.Random rnd)
        {
            if (kind == 7) return FirstAvailable(rnd, PalmModels);
            bool autumn = Theme == LevelTheme.Autumn || kind == 6;
            if ((kind == 1 || kind == 5) && Kit.Prop(QuaterniusTrees[kind][0]) != null) autumn = false;   // conifers stay green (the Kenney fall cones were the flat orange trees)
            var q = kind == 6 ? QuaterniusTrees[0] : QuaterniusTrees[kind];   // coloured trees: the textured oaks, recoloured in Tree()
            if (kind == 6 && Kit.Prop(q[0]) != null) return q[rnd.Next(q.Length)];
            if (Theme == LevelTheme.Autumn && (kind == 0 || kind == 2 || kind == 3 || kind == 4) && Kit.Prop(QuaterniusTrees[kind][0]) != null) return QuaterniusTrees[kind][rnd.Next(QuaterniusTrees[kind].Length)];
            if (!autumn && q != null && kind != 6)
            {
                var pick = q[rnd.Next(q.Length)];
                if (Kit.Prop(pick) != null) return pick;
            }
            string n = TreeModel[kind];
            if (autumn && Kit.Prop(n + "_fall") != null) return n + "_fall";
            return n;
        }

        static string FirstAvailable(System.Random rnd, params string[] names)
        {
            var ok = new List<string>(); foreach (var n in names) if (Kit.Prop(n) != null) ok.Add(n);
            return ok.Count == 0 ? names[0] : ok[rnd.Next(ok.Count)];
        }

        static readonly Dictionary<Mesh, Mesh> SimplifiedTrees = new Dictionary<Mesh, Mesh>();

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
                    PalmLook.Apply(model);
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

        /// <summary>Vine-seed source: a grove of tall bright-green bamboo culms with leafy tufts and bamboo pieces hanging in the middle.</summary>
        public static void BambooGrove(Transform parent, Vector3 pos, Material fruit, Mesh fruitMesh, System.Random rnd)
        {
            EnsureMaterials();
            var foliage = Mat("M_Foliage"); var light = Mat("M_FoliageLight");
            var culmMat = fruit != null ? new Material(fruit) { name = "M_BambooCulm" } : light;
            if (fruit != null) { culmMat.SetColor("_BaseColor", fruit.GetColor("_BaseColor") * 0.82f); culmMat.SetColor("_EmissionColor", fruit.GetColor("_BaseColor") * 0.12f); }
            int n = 6;
            for (int i = 0; i < n; i++)
            {
                float a = i * 6.28f / n + (float)rnd.NextDouble() * 0.5f, d = i == 0 ? 0f : 0.55f + (float)rnd.NextDouble() * 0.45f;
                int tier = (i + rnd.Next(3)) % 3;   // T40: short, medium and tall culms in one grove
                float h = tier == 0 ? 2.2f + (float)rnd.NextDouble() * 0.9f : tier == 1 ? 3.5f + (float)rnd.NextDouble() * 1.1f : 5.0f + (float)rnd.NextDouble() * 1.3f;
                var cp = pos + new Vector3(Mathf.Cos(a) * d, -0.18f, Mathf.Sin(a) * d);   // culm feet sunk into the ground
                var culm = new GameObject("Culm" + i); culm.transform.SetParent(parent, true); culm.transform.position = cp;
                culm.transform.rotation = Quaternion.Euler(Mathf.Sin(a) * 5f, 0, -Mathf.Cos(a) * 5f);   // leaning a little outwards
                culm.AddComponent<MeshFilter>().sharedMesh = SeedMeshes.Culm(h, 0.12f + (float)rnd.NextDouble() * 0.03f, Mathf.Clamp(Mathf.RoundToInt(h * 1.2f), 3, 8), false);
                culm.AddComponent<MeshRenderer>().sharedMaterial = culmMat;
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
            // T67: two yellow flowers like the ones on the vine bridges (yellow-orange petals, white inner petals, glowing cyan centre), low among the culms
            for (int fl = 0; fl < 2; fl++)
            {
                float fa = 1.0f + fl * 3.3f + (float)rnd.NextDouble() * 0.5f, fd = 1.0f + (float)rnd.NextDouble() * 0.25f;
                var fb = pos + new Vector3(Mathf.Cos(fa) * fd, 0f, Mathf.Sin(fa) * fd); float fh = 1.2f + fl * 0.5f;
                Prim("FlowerStem" + fl, PrimitiveType.Cylinder, parent, fb + Vector3.up * fh * 0.5f, new Vector3(0.05f, fh * 0.5f, 0.05f), light, false);
                for (int pt = 0; pt < 8; pt++)
                {
                    float pa = pt * 0.785f;
                    var petal = Prim("Petal", PrimitiveType.Sphere, parent, fb + new Vector3(Mathf.Cos(pa) * 0.3f, fh + 0.05f, Mathf.Sin(pa) * 0.3f), new Vector3(0.55f, 0.04f, 0.16f), _yellow, false);
                    petal.transform.rotation = Quaternion.Euler(0, -pa * Mathf.Rad2Deg, 22f);
                }
                for (int pt = 0; pt < 5; pt++)
                {
                    float pa = pt * 1.257f + 0.4f;
                    var petal = Prim("InnerPetal", PrimitiveType.Sphere, parent, fb + new Vector3(Mathf.Cos(pa) * 0.14f, fh + 0.11f, Mathf.Sin(pa) * 0.14f), new Vector3(0.32f, 0.03f, 0.1f), _white, false);
                    petal.transform.rotation = Quaternion.Euler(0, -pa * Mathf.Rad2Deg, 40f);
                }
                Prim("FlowerCentre" + fl, PrimitiveType.Sphere, parent, fb + Vector3.up * (fh + 0.1f), Vector3.one * 0.13f, Kit.Mat("M_Crystal") ?? _white, false);
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

        /// <summary>
        /// A leaf shaped like an open parachute canopy: a flattened dome of `gores` panels (alternating two submeshes) that bulge a little, with a
        /// scalloped lower edge. Two-sided. The dome is centred on the origin, its rim at y = 0, its top at y = height.
        /// </summary>
        public static Mesh CanopyMesh(float radius, float height, int gores = 10)
        {
            const int cols = 6, rows = 12;   // T67: finer panels (was 4 x 5), seam ribs, a top knob and UVs
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>[] { new List<int>(), new List<int>() };
            for (int g = 0; g < gores; g++)
            {
                int o = verts.Count;
                for (int r = 0; r <= rows; r++)
                {
                    float t = r / (float)rows, lat = t * 1.3f;                 // 0 at the top, 1.3 rad (75 degrees) at the rim
                    for (int c = 0; c <= cols; c++)
                    {
                        float u = c / (float)cols, ang = (g + u) * Mathf.PI * 2f / gores;
                        float rr = radius * Mathf.Sin(lat) / Mathf.Sin(1.3f);
                        rr *= 1f + 0.07f * Mathf.Sin(u * Mathf.PI) * t;       // each panel puffs out
                        float seam = Mathf.Exp(-Mathf.Pow(Mathf.Min(u, 1f - u) * 14f, 2f));
                        rr *= 1f + 0.035f * seam;                              // raised stitched seam between panels
                        if (r == 0) rr = 0f;
                        float y = height * (Mathf.Cos(lat) - Mathf.Cos(1.3f)) / (1f - Mathf.Cos(1.3f));
                        y += 0.14f * radius * Mathf.Sin(u * Mathf.PI) * t * t;   // scalloped edge: the middle of a panel hangs higher
                        verts.Add(new Vector3(Mathf.Cos(ang) * rr, y + 0.02f * radius * seam * t, Mathf.Sin(ang) * rr));
                        uvs.Add(new Vector2((g + u) / gores * 3f, t * 2f));
                    }
                }
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                    {
                        int a = o + r * (cols + 1) + c, b = a + 1, d = a + cols + 1, e = d + 1;
                        var t = tris[g % 2];
                        t.AddRange(new[] { a, b, d, b, e, d });                  // outside
                        t.AddRange(new[] { a, d, b, b, d, e });                  // inside
                    }
            }
            // top knob: a small bulb on the apex, in the second panel material
            int k0 = verts.Count; float kr = radius * 0.07f; int kseg = 10, krow = 5;
            for (int r = 0; r <= krow; r++)
            {
                float ph = r / (float)krow * Mathf.PI;
                for (int c = 0; c <= kseg; c++)
                {
                    float a = c * Mathf.PI * 2f / kseg;
                    verts.Add(new Vector3(Mathf.Cos(a) * Mathf.Sin(ph) * kr, height + 0.01f * radius - Mathf.Cos(ph) * kr * 0.9f + kr * 0.6f, Mathf.Sin(a) * Mathf.Sin(ph) * kr));
                    uvs.Add(new Vector2(c / (float)kseg, r / (float)krow));
                }
            }
            for (int r = 0; r < krow; r++)
                for (int c = 0; c < kseg; c++)
                {
                    int a = k0 + r * (kseg + 1) + c, b = a + 1, d = a + kseg + 1, e = d + 1;
                    tris[1].AddRange(new[] { a, b, d, b, e, d, a, d, b, b, d, e });
                }
            var mesh = new Mesh { name = "Canopy" };
            mesh.SetVertices(verts); mesh.SetUVs(0, uvs); mesh.subMeshCount = 2;
            mesh.SetTriangles(tris[0], 0); mesh.SetTriangles(tris[1], 1);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
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
                string[] cols = { "red", "purple", "yellow" }; string col = cols[rnd.Next(cols.Length)], col2 = cols[rnd.Next(cols.Length)];   // T40: two colours per clump, more flowers, varied heights
                for (int i = 0; i < 4; i++)
                {
                    var o = new Vector3((float)(rnd.NextDouble() - 0.5) * 1.1f, 0f, (float)(rnd.NextDouble() - 0.5) * 1.1f);
                    Model(go.transform, "flower_" + (i % 3 == 2 ? col2 : col) + "ABC"[rnd.Next(3)], pos + o, (float)rnd.NextDouble() * 360f, 0.45f + (float)rnd.NextDouble() * 0.5f, false);
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

        /// <summary>Portal arch: two pillars, a lintel, a glowing disc in the middle and steps. Faces `rot` forward.</summary>
        static void Arch(Transform parent, Vector3 pos, Quaternion rot)
        {
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
            Part("Disc", PrimitiveType.Cylinder, new Vector3(0, 2.3f, 0.05f), new Vector3(3.4f, 0.06f, 3.4f), _glow, false, Quaternion.Euler(90, 0, 0));
            Part("Ring", PrimitiveType.Cylinder, new Vector3(0, 2.3f, -0.02f), new Vector3(3.8f, 0.05f, 3.8f), _gold, false, Quaternion.Euler(90, 0, 0));
            Part("Step1", PrimitiveType.Cube, new Vector3(0, 0.1f, 1.2f), new Vector3(4.6f, 0.2f, 1.2f), _marble, true);
            Part("Step2", PrimitiveType.Cube, new Vector3(0, 0.3f, 0.6f), new Vector3(4.2f, 0.2f, 1.0f), _marble, true);
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
        static readonly string[] DebrisRuins = { "AP4_A1_StoneWall3", "AP4_A1_StoneWall5", "AP4_A2_StoneWall4", "AP4_A2_StoneWall6", "AP4_A3_DoorStone1", "AP4_A3_DoorStone2", "AP4_A3_LongStone1", "AP4_A2_Stone1", "AP4_A2_Stone2", "AP4_A3_StoneRing1" };

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
            var crystalMat = Kit.Mat("M_Crystal");
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
                    g.AddComponent<MeshFilter>().sharedMesh = DebrisMeshes.Rock(rnd.Next(8));
                    g.AddComponent<MeshRenderer>().sharedMaterial = stone;
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
                    var chunk = Prim("Chunk", PrimitiveType.Cube, g.transform, pos + new Vector3(s * 0.9f, -s * 0.5f, s * 0.2f), new Vector3(s * 0.5f, s * 0.35f, s * 0.45f), _marble, false);
                    chunk.transform.rotation = Random.rotationUniform;
                    g.transform.rotation = Quaternion.Euler((float)rnd.NextDouble() * 40f - 20f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 40f - 20f);
                }
                else if (kind < 0.84)       // flat carved slab
                {
                    g = Prim("Slab" + i, PrimitiveType.Cube, rocks.transform, pos, new Vector3(s * 1.6f, s * 0.22f, s * (0.9f + (float)rnd.NextDouble() * 0.5f)), _marble, false);
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
                    rock.AddComponent<MeshFilter>().sharedMesh = DebrisMeshes.Rock(rnd.Next(8)); rock.AddComponent<MeshRenderer>().sharedMaterial = stone;
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
                if (i % 5 == 0) Model(isl.transform, "AP4_A3_Door1", pos + new Vector3(-0.2f, 0, 1.6f), (float)rnd.NextDouble() * 360f, 2.6f, false);
                foreach (var c in isl.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);   // islets stay out of reach
                var b = isl.AddComponent<FloatBob>(); b.amplitude = 0.8f; b.period = 8f + i; b.spin = 1.5f;
            }
        }
    }
}
