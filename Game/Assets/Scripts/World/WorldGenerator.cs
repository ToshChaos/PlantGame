using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;
using PlantGame.Inventory;
using PlantGame.Plants;

namespace PlantGame.World
{
    /// <summary>
    /// Runtime world builder: turns a RunConfig into islands, soils, seed sources, water, goal and the player. Deterministic: the same
    /// RunConfig (seed, island count, kinds, water mode) always gives the same world (System.Random only). Level 1 is the hand-authored
    /// puzzle (LevelPlanner.Level1), every other config goes through LevelPlanner.Generate and is re-rolled until WorldValidator accepts it.
    /// Runs in Awake (before GameBootstrap) in Main; Sandbox scenes set useBootstrapConfig = false and fill `sandboxConfig`.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class WorldGenerator : MonoBehaviour
    {
        const float SeedFromSpawn = 10f;   // the start island's seeds lie at least this far from where you spawn: you have to walk to them

        public bool useBootstrapConfig = true;
        [Tooltip("Title showcase: a lake on the middle island and a single waterfall in the whole world.")] public bool showcaseWater;
        [Tooltip("Off for the title-screen showcase: no player, no respawn point.")] public bool spawnPlayer = true;
        public RunConfig sandboxConfig;
        public int Seed { get; private set; }
        public LevelPlan Plan { get; private set; }
        public LevelLayout Layout { get; private set; }
        public LevelTheme Theme { get; private set; }

        void Awake() { Generate(useBootstrapConfig ? GameBootstrap.Current : sandboxConfig); }

        public void Generate(int seed) { var c = GameBootstrap.DefaultConfig(GameMode.Exploration); c.seed = seed; c.levelId = null; Generate(c); }

        public static LevelPlan MakePlan(RunConfig c, out LevelLayout layout, out System.Random rnd)
        {
            if (c.levelId == GameBootstrap.HandLevelId)
            {
                var p = LevelPlanner.Plan(c); rnd = new System.Random(p.layoutSeed); layout = LevelLayout.Compute(p, rnd); return p;
            }
            LevelPlan plan = null;
            for (int attempt = 0; attempt < 100; attempt++)
            {
                plan = LevelPlanner.Generate(c, attempt);
                rnd = new System.Random(plan.layoutSeed); layout = LevelLayout.Compute(plan, rnd);
                if (WorldValidator.Validate(plan, layout).Count == 0) return plan;
            }
            Debug.LogWarning("[PlantGame] No valid layout for seed " + c.seed + ", using the last attempt.");
            rnd = new System.Random(plan.layoutSeed); layout = LevelLayout.Compute(plan, rnd);
            return plan;
        }

        public void Generate(RunConfig config)
        {
            for (int i = transform.childCount - 1; i >= 0; i--) Destroy(transform.GetChild(i).gameObject);
            var kit = Kit.Asset;
            if (kit == null) { Debug.LogError("[PlantGame] Resources/WorldKit is missing. Run the menu PlantGame > 14 Build World Kit."); return; }
            Seed = config.seed;
            var plan = Plan = MakePlan(config, out var lay, out var rnd);
            Layout = lay;
            bool collectorOnly = plan.collectorOnly;
            var isles = lay.isles; var links = plan.links; var heads = lay.heads.ToArray();
            int goalIsle = plan.GoalIsland(isles.Count);
            var grounds = Ground.Assign(plan.layoutSeed, isles.Count);
            bool summitSnow = config.spiral && config.wind && !showcaseWater;   // T46: the top of the windy spiral is snow-covered
            Theme = WorldTheme.Resolve(config); Scenery.Theme = Theme; Scenery.HugeTreeIsland = plan.hugeTreeIsland;
            var terrain = Scenery.Mat("M_Terrain"); var stone = Kit.Mat("M_Stone");
            foreach (var pr in lay.problems) Debug.LogWarning("[PlantGame] " + pr);
            IslandSpawn.All.Clear();
            int waterfalls = 0, frozenPonds = 0;
            // one or two completely unnecessary planting places on levels with more than 3 islands (own random: the layout stays)
            var decoyIsles = new HashSet<int>(); var decoyRnd = new System.Random(unchecked(plan.layoutSeed * 977 + 13));
            if (isles.Count > 3 && !showcaseWater) for (int d = 0, n = 1 + decoyRnd.Next(2); d < n; d++) decoyIsles.Add(1 + decoyRnd.Next(isles.Count - 1));

            for (int i = 0; i < isles.Count; i++)
            {
                var I = isles[i];
                I.go = RoundIsland.Create("Island_" + i, I.centre, I.shape, 6.5f + (I.radius - 8f) * 0.8f, Ground.Top(grounds[i], summitSnow && i == goalIsle), stone, i + 1, 3f);
                I.go.transform.SetParent(transform, true);
                var br = new System.Random(unchecked(plan.layoutSeed * 13 + i * 2711 + 5)); var bob = I.go.AddComponent<FloatBob>(); bob.amplitude = 0.16f + (float)br.NextDouble() * 0.2f; bob.period = 18f + (float)br.NextDouble() * 24f; bob.phaseOffset = (float)br.NextDouble() * 6.283f; bob.spin = 0f; bob.physics = true;   // every island drifts up and down very slowly
                var entry = LevelLayout.Dir(I.entryAngle);
                var forward = -entry;
                var spawn = new GameObject("Spawn"); spawn.transform.SetParent(I.go.transform, false);
                spawn.transform.position = I.centre + entry * (I.Edge(I.entryAngle) * (i == 0 ? 0.68f : 0.5f)) + Vector3.up * 0.05f;
                spawn.transform.rotation = Quaternion.LookRotation(forward);
                spawn.AddComponent<IslandSpawn>().islandRadius = I.shape.MaxEdge + 0.5f;
                I.keep.Add(new Vector4(spawn.transform.position.x, spawn.transform.position.z, 4.5f, 7f));
                if (i == 0) SpawnPlayer(spawn.transform);
                // soils for the outgoing links sit on the rim along each heading
                for (int k = 0; k < links.Count; k++)
                {
                    if (links[k].from != i || links[k].kind == PlantKind.Glider) continue;
                    var dir = LevelLayout.Dir(heads[k]); var L = links[k];
                    MakeSoil(I, L, k, I.centre + dir * lay.soilFwds[k], dir, "Soil_" + L.kind + "_" + k, terrain, lay.reaches[k], lay.rises[k]);
                }
                // water: only on some islands (the first always has it): a pond or a winding stream, away from the lanes
                if (!collectorOnly && (plan.waterIslands.Contains(i) || (showcaseWater && i == 1))) WaterFeature(I, rnd, heads, showcaseWater && i == 1, Theme == LevelTheme.Fog);
                else if (i > 0 && !showcaseWater && Theme != LevelTheme.Snow && Scenery.Hash01(plan.layoutSeed + i, 303) < 0.3) WaterFeature(I, new System.Random(plan.layoutSeed * 31 + i * 7 + 3), heads, false, false, true);   // swamp pond: green, not drinkable (own random)
                if (I.waterRoot != null && Theme == LevelTheme.Snow && !showcaseWater)   // snowy levels freeze every pond and waterfall (start island included); the two Heat Seeds on the start island melt them
                {
                    I.frozen = true; frozenPonds++;
                    var ws0 = I.waterRoot.GetComponentInChildren<WaterSource>(); if (ws0 != null) ws0.frozen = true;
                }
                // water pours over the rim only where a pond or stream already lies near the edge (its own random so the rest of the layout does not change)
                var fr = new System.Random(unchecked(plan.layoutSeed * 31 + i * 7919));
                bool wantFall = showcaseWater ? i == 1 : (fr.NextDouble() < 0.45 || Theme == LevelTheme.Fog);
                if (wantFall && I.waterRoot != null)
                {
                    var tr = I.go.GetComponent<IslandTerrain>();
                    float maxGap = showcaseWater ? 9f : Theme == LevelTheme.Fog ? 8f : 5.5f, bestGap = float.MaxValue, bestAng = 0f, bestDist = 0f;
                    foreach (var cv in tr.carves)
                    {
                        if (cv.w > IslandTerrain.BasinDepth + 0.001f) continue;   // water beds only (soil flats have bed 0)
                        var rel = new Vector3(cv.x - I.centre.x, 0, cv.y - I.centre.z); float dist = rel.magnitude;
                        float ang = Mathf.Atan2(-rel.z, rel.x) * Mathf.Rad2Deg;
                        bool clear = Mathf.Abs(Mathf.DeltaAngle(ang, I.entryAngle)) > 45f;
                        foreach (var h in I.outHeadings) if (Mathf.Abs(Mathf.DeltaAngle(ang, h)) < 45f) clear = false;
                        float gap = I.Edge(ang) - (dist + cv.z);
                        if (clear && gap < bestGap) { bestGap = gap; bestAng = ang; bestDist = dist; }
                    }
                    if (bestGap <= maxGap)
                    {
                        var dir = LevelLayout.Dir(bestAng);
                        waterfalls++;
                        Waterfall.Build(I.go.transform, I.centre + dir * (I.Edge(bestAng) - 0.2f), dir, 2.6f, I.frozen);
                        // a stream of the same water runs from the pond / stream to the lip: a winding bed of overlapping discs
                        var side = new Vector3(-dir.z, 0, dir.x); float ph = (float)(fr.NextDouble() * 6.28);
                        int n = Mathf.Clamp(Mathf.CeilToInt((I.Edge(bestAng) + 0.4f - bestDist) / 0.8f) + 1, 3, 16);
                        for (int fl = 0; fl < n; fl++)
                        {
                            var fp = I.centre + dir * (I.Edge(bestAng) + 0.4f - fl * 0.8f) + side * Mathf.Sin(fl * 0.8f + ph) * 0.55f * Mathf.Min(1f, fl * 0.5f);
                            tr.carves.Add(new Vector4(fp.x, fp.z, 1.05f + 0.25f * Mathf.Sin(fl * 1.7f + ph), IslandTerrain.BasinDepth * 0.6f));
                        }
                        I.keep.Add(new Vector4(I.centre.x + dir.x * (I.Edge(bestAng) - 2f), I.centre.z + dir.z * (I.Edge(bestAng) - 2f), 3.5f, 4f));
                    }
                }
                var spawnPos = spawn.transform.position;
                if (collectorOnly && i == 0) CollectorSpot(I, rnd, heads, spawnPos, true);   // one charge in a single-use dew patch: enough for the first plant
                if (collectorOnly && i == 1) CollectorSpot(I, rnd, heads, spawnPos, false);   // the dew seed and its soil: the water that refills
                foreach (var s in plan.sources)
                {
                    if (s.island != i) continue;
                    var item = Kit.Item(s.item);
                    if (item == null || item.worldPrefab == null) { Debug.LogWarning("[PlantGame] Missing seed item " + s.item); continue; }
                    bool tree = s.item == "vine_seed";
                    var pos = i == 0 ? Spot(I, rnd, 0.3f, 0.75f, 5.5f, heads, spawnPos, SeedFromSpawn) : Spot(I, rnd, 0.3f, 0.6f, 5.5f, heads);
                    SeedSource(I.go.transform, pos, item, tree, s.index, s.drop, s.maxAlive, I, spawnPlayer);
                }
                if (i == (plan.roundTrip ? 0 : goalIsle))
                {
                    // round trip: the goal waits on the start island, away from the spawn and the lanes
                    var gpos = plan.roundTrip ? Spot(I, rnd, 0.3f, 0.7f, 5f, heads, spawnPos, 9f) : I.centre;
                    var goal = GoalBuilder.Build(I.go.transform, gpos, WorldTheme.ResolveGoal(config), Theme);
                    if (Theme == LevelTheme.Fog) { var bl = new GameObject("FogBeacon"); bl.transform.SetParent(goal.transform, false); bl.transform.localPosition = Vector3.up * 3f; var lt = bl.AddComponent<Light>(); lt.type = LightType.Point; lt.color = new Color(1f, 0.85f, 0.5f); lt.range = 26f; lt.intensity = 4f; }   // T50: a warm glow you can find in the mist
                    if (plan.roundTrip) goal.requiresItemId = "heart_seed";
                    I.keep.Add(new Vector4(gpos.x, gpos.z, 4.5f, 8f));
                }
                if (plan.roundTrip && i == goalIsle)
                {
                    var heart = Kit.Item("heart_seed");
                    if (heart != null && heart.worldPrefab != null)
                    {
                        var src = SeedSource(I.go.transform, Spot(I, rnd, 0.2f, 0.5f, 5f, heads), heart, false, 90, 1, 1, I);
                        src.regrowSeconds = 0f;   // exactly one heart seed
                    }
                }
                // alternative planting spots for the gaps that leave this island: the preferred soil above stays the intended one, bigger islands offer more
                for (int k = 0; k < links.Count; k++)
                {
                    if (links[k].from != i || links[k].kind == PlantKind.Glider) continue;
                    int alt = 0;
                    foreach (var a in lay.Alternatives(plan, k, I.keep))
                    {
                        MakeSoil(I, links[k], k, a.pos, a.dir, "Soil_" + links[k].kind + "_" + k + "_alt" + (++alt), terrain, a.reach, a.rise);
                    }
                }
                if (decoyIsles.Contains(i))   // a patch no route needs: a seed planted here only costs the seed
                {
                    var kinds = new List<LinkPlan>(); foreach (var lp in links) if (lp.kind != PlantKind.Glider) kinds.Add(lp);
                    if (kinds.Count > 0)
                    {
                        var lk = kinds[decoyRnd.Next(kinds.Count)];
                        var spot = Spot(I, decoyRnd, 0.5f, 0.78f, 3.5f, heads);
                        var outward = spot - I.centre; outward.y = 0f; outward = outward.sqrMagnitude > 0.01f ? outward.normalized : Vector3.forward;
                        MakeSoil(I, lk, -1, spot, outward, "Soil_" + lk.kind + "_decoy", terrain, lk.kind == PlantKind.Bridge ? 9.5f : 0f, 0f);
                    }
                }
            }

            var weather = GetComponent<WeatherController>() ?? gameObject.AddComponent<WeatherController>();
            bool windy = WorldTheme.ResolveWind(config) && !showcaseWater;
            weather.Apply(Theme, windy);
            var oldWind = GetComponent<WindGusts>(); if (oldWind != null) Destroy(oldWind);
            if (windy) gameObject.AddComponent<WindGusts>();   // T50

            // scenery goes in last so it keeps clear of everything else
            for (int i = 0; i < isles.Count; i++)
            {
                var I = isles[i];
                var lanes = new List<Scenery.Lane>();
                foreach (var h in I.outHeadings) { var d = LevelLayout.Dir(h); lanes.Add(new Scenery.Lane { origin = new Vector2(I.centre.x, I.centre.z), dir = new Vector2(d.x, d.z), length = I.Edge(h), halfWidth = 3.4f }); }
                var e = LevelLayout.Dir(I.entryAngle);
                lanes.Add(new Scenery.Lane { origin = new Vector2(I.centre.x, I.centre.z), dir = new Vector2(e.x, e.z), length = I.Edge(I.entryAngle) * 0.95f, halfWidth = 3.6f });
                Scenery.Decorate(I.go.transform, I.centre, I.radius, a => I.shape.Edge(a), i, i == 0, i == goalIsle, I.keep, lanes, summitSnow && i == goalIsle ? 0.45f : Ground.Vegetation(grounds[i]), Ground.Boulders(grounds[i]), !collectorOnly && (plan.waterIslands.Contains(i) || (showcaseWater && i == 1)));
                // T39: some bare islands crumble at the rim, shedding small rocks (own random)
                if (grounds[i] != GroundKind.Grass)
                {
                    var cr = new System.Random(unchecked(plan.layoutSeed * 17 + i * 104729));
                    if (Scenery.Hash01(plan.layoutSeed + i, 17) < (grounds[i] == GroundKind.Rock ? 0.8f : 0.5f))
                        for (int tries = 0, placedEdges = 0; tries < 20 && placedEdges < 2; tries++)
                        {
                            float ang = (float)(cr.NextDouble() * 360.0); bool ok = Mathf.Abs(Mathf.DeltaAngle(ang, I.entryAngle)) > 45f;
                            foreach (var h in I.outHeadings) if (Mathf.Abs(Mathf.DeltaAngle(ang, h)) < 45f) ok = false;
                            if (!ok) continue;
                            var rd = LevelLayout.Dir(ang);
                            FallingBits.AddRocks(I.go.transform, I.centre + rd * (I.Edge(ang) - 0.3f) + Vector3.up * IslandTerrain.Shoulder, rd, grounds[i] == GroundKind.Rock ? new Color(0.5f, 0.5f, 0.54f) : new Color(0.55f, 0.4f, 0.28f));
                            placedEdges++;
                        }
                }
            }
            if (frozenPonds > 0)   // T50: Heat Seeds on the start island, one per frozen pond (the source regrows them)
            {
                var hs = Kit.Item("heat_seed");
                if (hs != null && hs.worldPrefab != null)
                {
                    var hp = Spot(isles[0], new System.Random(plan.layoutSeed + 77), 0.3f, 0.75f, 5.5f, heads);
                    SeedSource(isles[0].go.transform, hp, hs, false, 9, 2, 2, isles[0]);
                    var warm = GameObject.CreatePrimitive(PrimitiveType.Cylinder); warm.name = "UnfrozenGround";   // grass around the Heat Seeds: never snowed over
                    Object.DestroyImmediate(warm.GetComponent<Collider>()); warm.transform.SetParent(isles[0].go.transform, true);
                    warm.transform.position = hp + Vector3.up * 0.06f; warm.transform.localScale = new Vector3(4.6f, 0.02f, 4.6f);
                    warm.GetComponent<Renderer>().sharedMaterial = Scenery.Mat("M_Terrain");
                    isles[0].keep.Add(new Vector4(hp.x, hp.z, 3f, 3f));
                }
            }
            if (config.mode == GameMode.Challenge && !showcaseWater && isles.Count >= 3)   // T52: ceil(islands / 3) time fruits worth 30 s per island in total on islands past the start, own random so the rest of the world is unchanged
            {
                var fr2 = new System.Random(unchecked(plan.layoutSeed * 17 + 991));
                int fruits = Mathf.CeilToInt(isles.Count / 3f);   // one per three islands, rounded up; together they add 30 s per island
                float each = 30f * isles.Count / fruits;
                var cand = new List<int>(); for (int i = 1; i < isles.Count; i++) if (i != goalIsle) cand.Add(i);
                for (int k = 0; k < fruits && cand.Count > 0; k++)
                {
                    int pick = fr2.Next(cand.Count); int ii = cand[pick]; cand.RemoveAt(pick);
                    var fp = Spot(isles[ii], fr2, 0.3f, 0.7f, 3f, heads);
                    TimeFruit.Build(isles[ii].go.transform, fp, each);
                }
            }
            if (showcaseWater && isles.Count >= 3) AddShowcaseIslet(isles, terrain, stone);
            foreach (var I in isles) FinishTerrain(I);
            // tilted islands lean as a whole (soils, trees and spawn included); the layout already measured the gaps from the tilted surface
            foreach (var I in isles)
                if (I.slope.sqrMagnitude > 0f)
                    I.go.transform.rotation = Quaternion.FromToRotation(Vector3.up, new Vector3(-I.slope.x, 1f, -I.slope.y).normalized);
            var cs = new List<Vector3>(); var rs = new List<float>(); var ls = new List<Vector2Int>();
            for (int i = 0; i < isles.Count; i++) { cs.Add(isles[i].centre); rs.Add(isles[i].shape.MaxEdge); if (isles[i].parent >= 0) ls.Add(new Vector2Int(isles[i].parent, i)); }
            Scenery.KeepDebrisClear(cs, rs, ls);
        }

        /// <summary>Title screen only: a small island that is not connected to the chain, off to the right of the central island.</summary>
        void AddShowcaseIslet(List<Isle> isles, Material top, Material stone)
        {
            var c1 = isles[1].centre; var chain = isles[2].centre - isles[0].centre; chain.y = 0;
            if (chain.sqrMagnitude < 0.01f) chain = Vector3.right;
            var right = Vector3.Cross(Vector3.up, chain.normalized);
            float r = 5f, dist = isles[1].shape.MaxEdge + r + 9.5f;
            Vector3 Pos(float sign) => c1 + right * sign * dist + Vector3.up * 1.5f;
            bool Clear(Vector3 p) { foreach (var o in isles) { var d = p - o.centre; d.y = 0; if (d.magnitude < o.shape.MaxEdge + r + 8.5f) return false; } return true; }
            var pos = Pos(1f); if (!Clear(pos)) pos = Pos(-1f);
            var X = new Isle { centre = pos, radius = r, shape = IslandShape.Random(new System.Random(Seed * 3 + 11), r, 0.3f), parent = -1, entryAngle = 0f };
            X.go = RoundIsland.Create("Island_Extra", pos, X.shape, 5.5f, top, stone, 91, 3f);
            X.go.transform.SetParent(transform, true);
            var bob = X.go.AddComponent<FloatBob>(); bob.amplitude = 0.22f; bob.period = 33f; bob.phaseOffset = 4.1f; bob.spin = 0f; bob.physics = true;
            Scenery.Decorate(X.go.transform, X.centre, X.radius, a => X.shape.Edge(a), 77, false, false, X.keep, new List<Scenery.Lane>(), 0.9f, 1, false);
            isles.Add(X);
        }

        const float BridgeSoilSink = 0.09f;   // the bridge's squircle bed sinks 6 cm deeper than the other beds so it sticks out less

        /// <summary>A soil patch for the gap `k` at `pos`, growing towards `dir`; keeps scenery clear of it.</summary>
        static void MakeSoil(Isle I, LinkPlan L, int k, Vector3 pos, Vector3 dir, string name, Material terrain, float reach, float rise)
        {
            var soil = Box(name, pos - Vector3.up * (L.kind == PlantKind.Ladder ? 0.1f : L.kind == PlantKind.Bridge ? BridgeSoilSink : 0.03f), new Vector3(1.2f, 0.3f, 1.2f), terrain);   // sunk into the ground: only the top ~0.12 m shows (~0.05 m for the beanstalk's round patch, so it sits lower on uneven ground)
            soil.transform.rotation = Quaternion.LookRotation(dir);
            SoilPatchMesh.Apply(soil, L.kind);
            soil.transform.SetParent(I.go.transform, true);
            var target = soil.AddComponent<SandboxTarget>();
            target.kind = L.kind; target.acceptedItemId = L.seedItem; target.growDirection = dir;
            if (L.kind == PlantKind.Bridge) target.spawnOffset += Vector3.up * (BridgeSoilSink - 0.03f);   // the bed sits lower, the vine stays where it was
            target.bridgeReach = L.kind == PlantKind.Bridge ? reach : 0f; target.bridgeRise = rise;
            target.plantPrefabs = Kit.Plants(L.kind);
            target.variantIndex = L.kind == PlantKind.Ladder ? LevelLayout.LadderVariant - 1 : L.kind == PlantKind.Platform ? LevelLayout.PlatformVariant - 1 : -1;
            int interactable = LayerMask.NameToLayer(Layers.InteractableName);
            if (interactable >= 0) soil.layer = interactable;
            if (Scenery.Theme == LevelTheme.Snow) target.Freeze();   // frozen soil needs a Heat Seed before any seed goes in
            I.keep.Add(new Vector4(soil.transform.position.x, soil.transform.position.z, 4f, 6f));
            I.go.GetComponent<IslandTerrain>().carves.Add(new Vector4(pos.x, pos.z, 1.7f, 0f));   // the ground under the soil is flat
        }

        void SpawnPlayer(Transform spawn)
        {
            if (!spawnPlayer) return;
            var respawn = new GameObject("Respawn") { tag = Layers.RespawnTag };
            respawn.transform.SetParent(transform, true);
            respawn.transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            if (Kit.Asset.player != null && FindAnyObjectByType<Player.PlayerController>() == null)
                Instantiate(Kit.Asset.player, respawn.transform.position, respawn.transform.rotation);
        }

        /// <summary>A random free spot on an island between rMin..rMax of its radius, clear of lanes and earlier spots.</summary>
        static Vector3 Spot(Isle I, System.Random rnd, float rMin, float rMax, float clear, float[] heads, Vector3? farFrom = null, float farDist = 0f)
        {
            Vector3 best = I.centre; float bestScore = -1f;
            for (int t = 0; t < 60; t++)
            {
                float a = (float)(rnd.NextDouble() * Mathf.PI * 2), r = I.shape.Edge(a) * (rMin + (float)rnd.NextDouble() * (rMax - rMin));
                var p = I.centre + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * r;
                float score = float.MaxValue;
                foreach (var k in I.keep) score = Mathf.Min(score, Vector2.Distance(new Vector2(p.x, p.z), new Vector2(k.x, k.y)));
                foreach (var h in I.outHeadings)   // distance to the outgoing lane (centre -> rim)
                {
                    var d = LevelLayout.Dir(h); var rel = p - I.centre; float along = Vector3.Dot(rel, d);
                    float perp = (rel - d * along).magnitude;
                    if (along > -2f) score = Mathf.Min(score, perp + 2f);
                }
                if (farFrom.HasValue && farDist > 0f) score = Mathf.Min(score, clear * Vector2.Distance(new Vector2(p.x, p.z), new Vector2(farFrom.Value.x, farFrom.Value.z)) / farDist);   // >= clear only when far enough
                if (score >= clear) { I.keep.Add(new Vector4(p.x, p.z, 3.5f, 7f)); return p; }
                if (score > bestScore) { bestScore = score; best = p; }
            }
            I.keep.Add(new Vector4(best.x, best.z, 3.5f, 7f));
            return best;
        }

        /// <summary>Seed producer: a tree (trunk + crown) or a low patch of mounds, with fruit in the seed's colour. Drops 2 seeds at start, one more every 60 s while fewer than 2 lie around.</summary>
        static SeedTree SeedSource(Transform parent, Vector3 pos, ItemDefinition item, bool tree, int index, int drop, int maxAlive, Isle isle, bool ambience = true)
        {
            var foliage = item.id == "heat_seed" ? Kit.Mat("M_Foliage") : Scenery.Mat("M_Foliage");   // the Heat Seed mounds stay green: they sit on warm, unfrozen ground
            var root = new GameObject(tree ? "SeedTree_" + item.id : "SeedPatch_" + item.id);
            root.transform.SetParent(parent, true); root.transform.position = pos;
            Material fruitMat = null;
            var fr = item.worldPrefab.GetComponentInChildren<Renderer>();
            if (fr != null) fruitMat = fr.sharedMaterial;
            GameObject Piece(string n, PrimitiveType type, Vector3 local, Vector3 scale, Material m, bool collide)
            {
                var g = GameObject.CreatePrimitive(type); g.name = n; g.transform.SetParent(root.transform, false);
                g.transform.localPosition = local; g.transform.localScale = scale;
                g.GetComponent<Renderer>().sharedMaterial = m;
                if (!collide) Object.DestroyImmediate(g.GetComponent<Collider>());
                else { int gl = LayerMask.NameToLayer(Layers.GroundName); if (gl >= 0) g.layer = gl; }
                return g;
            }
            var rnd = new System.Random(index * 31 + 5);
            Mesh fruitMesh = SeedMeshes.For(item.id) != null ? item.worldPrefab.GetComponentInChildren<MeshFilter>()?.sharedMesh : null;   // T33: the seed's own shape
            if (tree && item.id == "vine_seed")
            {
                // bamboo grove: bright green culms around a solid core, bamboo pieces hanging in the middle
                var cc = root.AddComponent<CapsuleCollider>(); cc.radius = 0.55f; cc.height = 3f; cc.center = new Vector3(0, 1.5f, 0);
                int gl = LayerMask.NameToLayer(Layers.GroundName); if (gl >= 0) root.layer = gl;
                Scenery.BambooGrove(root.transform, pos, fruitMat, fruitMesh, rnd);
            }
            else if (tree)
            {
                var bark = Kit.Mat("M_Bark"); var light = Scenery.Mat("M_FoliageLight");
                // trunk: tapered, flared at the root, brown bark texture; solid collider only for the lower part
                var trunk = new GameObject("Trunk"); trunk.transform.SetParent(root.transform, false);
                trunk.AddComponent<MeshFilter>().sharedMesh = TreeParts.Trunk(3.0f, 0.5f, 0.3f, index + 3);
                trunk.AddComponent<MeshRenderer>().sharedMaterial = bark;
                var cc = trunk.AddComponent<CapsuleCollider>(); cc.radius = 0.45f; cc.height = 3f; cc.center = new Vector3(0, 1.5f, 0);
                int gl = LayerMask.NameToLayer(Layers.GroundName); if (gl >= 0) trunk.layer = gl;
                // three branches leaning out into the crown
                for (int br = 0; br < 3; br++)
                {
                    float a = br * 2.1f + (float)rnd.NextDouble();
                    var b = Piece("Branch" + br, PrimitiveType.Cylinder, new Vector3(Mathf.Cos(a) * 0.55f, 2.7f, Mathf.Sin(a) * 0.55f), new Vector3(0.14f, 0.75f, 0.14f), bark, false);
                    b.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 52f, 0, -Mathf.Cos(a) * 52f);
                }
                // crown: clustered puffs in two greens, fruit hanging at the outside
                Vector3[] puff = { new Vector3(0, 3.7f, 0), new Vector3(1.2f, 3.3f, 0.4f), new Vector3(-1.1f, 3.4f, 0.6f), new Vector3(0.2f, 3.2f, -1.2f), new Vector3(0.1f, 4.5f, 0.2f), new Vector3(-0.5f, 3.4f, 1.3f) };
                float[] size = { 2.8f, 2.0f, 2.1f, 1.9f, 1.8f, 1.7f };
                for (int c = 0; c < puff.Length; c++)
                {
                    var lc = Scenery.LeafCrown(root.transform, pos + puff[c], size[c] * 0.62f, 0.8f, c % 2 == 0 ? Color.white : new Color(0.85f, 1f, 0.8f), rnd, 26);
                    if (lc == null) Piece("Crown" + c, PrimitiveType.Sphere, puff[c], new Vector3(size[c], size[c] * 0.8f, size[c]), c % 2 == 0 ? foliage : light, false);
                }
                for (int f = 0; f < 7; f++)
                {
                    float a = (float)(rnd.NextDouble() * 6.28), h = 3.0f + (float)rnd.NextDouble() * 1.0f, rad = 1.7f + (float)rnd.NextDouble() * 0.4f;
                    Piece("Fruit" + f, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * rad, h, Mathf.Sin(a) * rad), Vector3.one * 0.32f, fruitMat, false);
                }
            }
            else if (item.id == "beanstalk_seed") { Scenery.BeanPatch(root.transform, pos, fruitMat, fruitMesh, rnd); }
            else if (item.id == "shelf_seed") { Scenery.ShelfPatch(root.transform, pos, fruitMat, fruitMesh, rnd); }
            else if (item.id == "dew_seed") { Scenery.SucculentPatch(root.transform, pos, fruitMat, fruitMesh, rnd); }
            else if (item.id == "parachute_leaf") { Scenery.LeafPlant(root.transform, pos, fruitMat, rnd); }
            else if (item.id == "heat_seed" && fruitMat != null) { Scenery.MelonPatch(root.transform, pos, fruitMat, rnd); }
            else
            {
                for (int m = 0; m < 4; m++)
                {
                    float a = m * 1.6f + (float)rnd.NextDouble();
                    Piece("Mound" + m, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 0.8f, 0.1f, Mathf.Sin(a) * 0.8f), new Vector3(1.4f, 0.8f, 1.4f), foliage, false);
                    Piece("Fruit" + m, PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 0.8f, 0.55f, Mathf.Sin(a) * 0.8f), Vector3.one * 0.28f, fruitMat, false);
                }
            }
            var src = root.AddComponent<SeedTree>();
            if (ambience) root.AddComponent<Audio.SeedAmbience>();
            src.possibleSeeds = new[] { item };
            src.seedsToDrop = drop; src.maxAlive = maxAlive; src.regrowSeconds = 60f;
            src.dropRadius = 3f; src.dropHeight = tree ? 2.4f : 0.9f; src.sandboxSeed = index + 1;
            return src;
        }

        /// <summary>
        /// Collector-only levels. singleUse (first island): a Water Collector already grown with one charge that never refills.
        /// Otherwise (second island): a soil patch for the Water Collector plus the dew seed lying next to it.
        /// </summary>
        static void CollectorSpot(Isle I, System.Random rnd, float[] heads, Vector3 spawnPos, bool singleUse)
        {
            var terrain = Scenery.Mat("M_Terrain");
            var pos = Spot(I, rnd, 0.3f, 0.75f, 4.5f, heads, spawnPos, SeedFromSpawn);
            var soil = Box("Soil_Collector", pos + Vector3.up * 0.05f, new Vector3(1.2f, 0.3f, 1.2f), terrain);
            soil.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
            SoilPatchMesh.Apply(soil, PlantKind.Collector);
            soil.transform.SetParent(I.go.transform, true);
            var target = soil.AddComponent<SandboxTarget>();
            target.kind = PlantKind.Collector; target.acceptedItemId = "dew_seed"; target.growDirection = Vector3.up;
            target.plantPrefabs = Kit.Plants(PlantKind.Collector); target.variantIndex = 0;
            int interactable = LayerMask.NameToLayer(Layers.InteractableName);
            if (interactable >= 0) soil.layer = interactable;
            var item = Kit.Item("dew_seed");
            if (singleUse) { target.PlantSingleUse(); return; }
            if (item != null && item.worldPrefab != null) SeedSource(I.go.transform, Spot(I, rnd, 0.3f, 0.75f, 4f, heads, spawnPos, SeedFromSpawn), item, false, 9, 1, 1, I);
        }

        /// <summary>
        /// A lake (a cluster of overlapping lobes, sometimes with an inlet) or a thin winding stream (sometimes ending in a pool).
        /// The shape is a list of discs: they carve a hollow into the island terrain (IslandTerrain) and the water surface fills it,
        /// so the shore is irregular. One WaterSource trigger covers the whole feature.
        /// </summary>
        static void WaterFeature(Isle I, System.Random rnd, float[] heads, bool forceLake = false, bool nearRim = false, bool murky = false)   // nearRim: the pond sits close to the edge so its waterfall can pour over (heard in the fog)
        {
            double pick = rnd.NextDouble();   // some water is darker (deeper) than other
            var wmat = murky ? MurkyMat() : pick < 0.35 ? Kit.Mat("M_Water") : pick < 0.7 ? Kit.Mat("M_WaterMid") : Kit.Mat("M_WaterDeep");
            var root = new GameObject("Water"); root.transform.SetParent(I.go.transform, true);
            int water = LayerMask.NameToLayer(Layers.WaterName);
            var terrain = I.go.GetComponent<IslandTerrain>();

            bool Ok(Vector3 p, float r)
            {
                var rel = p - I.centre; rel.y = 0;
                if (forceLake)   // the showcase lake sits clear of the entry and exit sides so its waterfall can pour over a free stretch of rim
                {
                    float pa = Mathf.Atan2(-rel.z, rel.x) * Mathf.Rad2Deg;
                    if (Mathf.Abs(Mathf.DeltaAngle(pa, I.entryAngle)) < 60f) return false;
                    foreach (var h in I.outHeadings) if (Mathf.Abs(Mathf.DeltaAngle(pa, h)) < 60f) return false;
                }
                if (rel.magnitude + r * 0.8f > I.shape.Edge(Mathf.Atan2(rel.z, rel.x)) * 0.88f) return false;
                foreach (var k in I.keep) if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(k.x, k.y)) < 1.8f + r) return false;
                foreach (var h in I.outHeadings)
                {
                    var d = LevelLayout.Dir(h); float along = Vector3.Dot(rel, d); float perp = (rel - d * along).magnitude;
                    if (along > -2f && perp < 2.2f + r) return false;
                }
                return true;
            }

            bool stream = rnd.NextDouble() < 0.5 && !forceLake;
            var discs = new List<Vector4>();   // x, y(unused), z, r
            for (int attempt = 0; attempt < 80; attempt++)
            {
                var c = Spot(I, rnd, forceLake ? 0.42f : nearRim ? 0.5f : 0.2f, nearRim ? 0.75f : 0.6f, 4f, heads); I.keep.RemoveAt(I.keep.Count - 1);   // only the centre is wanted here
                c.y = I.centre.y;
                discs.Clear();
                float yaw = (float)(rnd.NextDouble() * 360.0), shrink = 1f - attempt * 0.008f;
                if (stream)
                {
                    float len = I.radius * (0.9f + (float)rnd.NextDouble() * 0.6f) * shrink, amp = 1.2f + (float)rnd.NextDouble() * 1.6f;
                    float ph = (float)rnd.NextDouble() * 6.28f, freq = 3.5f + (float)rnd.NextDouble() * 3f, w0 = 0.65f + (float)rnd.NextDouble() * 0.5f;
                    var along = Quaternion.Euler(0, yaw, 0) * Vector3.right; var perp = Vector3.Cross(Vector3.up, along);
                    int n = Mathf.CeilToInt(len / 0.7f);
                    for (int i = 0; i < n; i++)
                    {
                        float t = i / (float)(n - 1) - 0.5f;
                        float w = w0 * (0.8f + 0.5f * Mathf.PerlinNoise(i * 0.35f + ph, 3.7f));   // narrow and wide stretches
                        var p = c + along * t * len + perp * Mathf.Sin(t * freq + ph) * amp;
                        discs.Add(new Vector4(p.x, 0, p.z, w));
                    }
                    if (rnd.NextDouble() < 0.5)   // a pool at one end
                    {
                        var e = discs[rnd.NextDouble() < 0.5 ? 0 : n - 1]; float pr = 1.7f + (float)rnd.NextDouble() * 1.0f;
                        discs.Add(new Vector4(e.x, 0, e.z, pr));
                        discs.Add(new Vector4(e.x + (float)(rnd.NextDouble() - 0.5) * 1.4f, 0, e.z + (float)(rnd.NextDouble() - 0.5) * 1.4f, pr * 0.8f));
                    }
                }
                else
                {
                    float core = 2.1f + (float)rnd.NextDouble() * 1.5f;
                    discs.Add(new Vector4(c.x, 0, c.z, core));
                    int lobes = 3 + rnd.Next(4);
                    for (int i = 0; i < lobes; i++)
                    {
                        float a = (float)(rnd.NextDouble() * 6.28), lr = core * (0.55f + (float)rnd.NextDouble() * 0.45f), off = core * (0.7f + (float)rnd.NextDouble() * 0.6f);
                        discs.Add(new Vector4(c.x + Mathf.Cos(a) * off, 0, c.z + Mathf.Sin(a) * off, lr));
                    }
                    if (rnd.NextDouble() < 0.5)   // a short inlet stream
                    {
                        float a = (float)(rnd.NextDouble() * 6.28), ph = (float)(rnd.NextDouble() * 6.28);
                        for (int i = 1; i <= 6; i++)
                        {
                            float d = core * 0.8f + i * 0.7f, side = Mathf.Sin(i * 0.9f + ph) * 0.5f;
                            discs.Add(new Vector4(c.x + Mathf.Cos(a) * d - Mathf.Sin(a) * side, 0, c.z + Mathf.Sin(a) * d + Mathf.Cos(a) * side, Mathf.Max(0.55f, 1.0f - i * 0.08f)));
                        }
                    }
                }
                bool ok = true; foreach (var d in discs) if (!Ok(new Vector3(d.x, 0, d.z), d.w)) { ok = false; break; }
                if (ok) break;
                discs.Clear();
            }
            if (discs.Count == 0)   // nothing fitted: a plain pond at a free spot
            {
                var f = Spot(I, rnd, 0.3f, 0.6f, 4f, heads);
                discs.Add(new Vector4(f.x, 0, f.z, 2.2f));
            }

            // carve the hollow, keep scenery away, and place one trigger over the whole feature
            var trig = new GameObject("Water_Trigger"); trig.transform.SetParent(root.transform, true);
            if (!murky) { trig.AddComponent<WaterSource>().kind = WaterKind.Lake; if (water >= 0) trig.layer = water; }
            for (int i = 0; i < discs.Count; i++)
            {
                var d = discs[i];
                terrain.carves.Add(new Vector4(d.x, d.z, d.w, IslandTerrain.BasinDepth));
                if (i % 2 == 0 || d.w > 1.5f) I.keep.Add(new Vector4(d.x, d.z, d.w + 1.6f, d.w + 2.2f));
                I.pondDiscs.Add(d);
                if (murky) continue;   // swamp water gives nothing to drink: no trigger boxes
                var box = new GameObject("Box"); box.transform.SetParent(trig.transform, true);
                box.transform.position = new Vector3(d.x, I.centre.y, d.z);
                if (water >= 0) box.layer = water;
                var bc = box.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(d.w * 2f, 40f, d.w * 2f);
            }
            if (murky) { I.murky = true; I.murkyMat = wmat; I.murkyRoot = root.transform; return; }
            var ws = new GameObject("WaterSound"); ws.transform.SetParent(root.transform, true); ws.transform.position = new Vector3(discs[0].x, I.centre.y, discs[0].z);
            var we = ws.AddComponent<Audio.LoopEmitter>(); we.clipName = "water_babble"; we.volume = 0.35f; we.maxDistance = 16f;   // T43: ponds and streams babble
            I.waterMat = wmat; I.waterRoot = root.transform;
        }

        static Material _murky;
        /// <summary>Swamp water: the pond shader tinted green-brown and dark, with a murky ripple. Never a water source.</summary>
        static Material MurkyMat()
        {
            if (_murky != null) return _murky;
            var src = Kit.Mat("M_WaterMid"); _murky = new Material(src) { name = "M_WaterMurky", hideFlags = HideFlags.DontSave };
            _murky.SetColor("_ShallowColor", new Color(0.36f, 0.45f, 0.2f)); _murky.SetColor("_DeepColor", new Color(0.13f, 0.2f, 0.1f));
            _murky.SetColor("_RimColor", new Color(0.55f, 0.65f, 0.3f)); _murky.SetFloat("_Depth", 0.8f); _murky.SetFloat("_Glint", 0.15f);
            return _murky;
        }

        /// <summary>Terrain is final: rebuild the island meshes with their hollows, add the water surfaces, and sit everything on the bumpy ground.</summary>
        static void FinishTerrain(Isle I)
        {
            var terrain = I.go.GetComponent<IslandTerrain>(); if (terrain == null) return;
            terrain.Rebuild();
            if (I.murky && I.murkyMat != null && I.murkyRoot != null)
            {
                var s = new GameObject("Water_Surface"); s.transform.SetParent(I.murkyRoot, false);
                s.transform.position = I.go.transform.position;
                s.AddComponent<MeshFilter>().sharedMesh = terrain.BuildWaterSurface();
                s.AddComponent<MeshRenderer>().sharedMaterial = I.murkyMat;
            }
            if (I.pondDiscs.Count > 0) WaterPlants.Build(I, terrain, I.pondDiscs, I.murky, (int)(I.centre.x * 13f) ^ (int)(I.centre.z * 7f), Scenery.Theme);
            if (I.waterMat != null && I.waterRoot != null)
            {
                var s = new GameObject("Water_Surface"); s.transform.SetParent(I.waterRoot, false);
                s.transform.position = I.go.transform.position;
                s.AddComponent<MeshFilter>().sharedMesh = terrain.BuildWaterSurface();
                var sr = s.AddComponent<MeshRenderer>(); sr.sharedMaterial = I.frozen ? WaterSource.IceMat() : I.waterMat;
                var wsrc = I.waterRoot.GetComponentInChildren<WaterSource>(); if (wsrc != null) { wsrc.surface = sr; wsrc.liquid = I.waterMat; }
                int water = LayerMask.NameToLayer(Layers.WaterName); if (water >= 0) s.layer = water;
            }
            void Lift(Transform t)
            {
                var p = t.position; float dy = terrain.HeightAtWorld(p);
                if (Mathf.Abs(p.y - I.centre.y) < 3f && dy != 0f) t.position = p + Vector3.up * Mathf.Max(dy, -0.1f);
            }
            foreach (Transform c in I.go.transform)
            {
                if (c.name == "Water" || c.name == "WaterPlants" || c.name == "Waterfall" || c.name == "Overgrowth" || c.name == "SideDecor") continue;
                if (c.name == "Scenery") { foreach (Transform sc in c) Lift(sc); continue; }
                Lift(c);
            }
        }

        static GameObject Box(string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.position = pos; go.transform.localScale = size;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            int ground = LayerMask.NameToLayer(Layers.GroundName);
            if (ground >= 0) go.layer = ground;
            return go;
        }
    }
}
