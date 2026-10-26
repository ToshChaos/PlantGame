using System.Collections.Generic;
using System.Text;
using UnityEngine;
using PlantGame.Core;
using PlantGame.Plants;

namespace PlantGame.World
{
    /// <summary>One gap: a plant on island `from` that leads to the next new island (link i creates island i + 1, or the side island).</summary>
    public struct LinkPlan { public int from; public PlantKind kind; public float heading; public string seedItem; }

    /// <summary>A tree or patch on an island that hands out seeds of one type.</summary>
    public struct SeedSourcePlan { public int island; public string item; public int index, drop, maxAlive; }

    /// <summary>What the world generator builds: islands, gaps (plant kind per gap), seed supply and water. Pure data, deterministic from the RunConfig.</summary>
    public class LevelPlan
    {
        public int layoutSeed;
        public int islandCount;
        public readonly List<LinkPlan> links = new List<LinkPlan>();
        public readonly List<SeedSourcePlan> sources = new List<SeedSourcePlan>();
        public readonly HashSet<int> waterIslands = new HashSet<int>();
        public bool collectorOnly, roundTrip, spiral;
        public int attempt;
        /// <summary>Island that holds the goal (-1 = the last island). Optional side islands come after it in the list.</summary>
        public int goalIsland = -1;
        /// <summary>T38: index of one island built much larger (radius about 1.9x), -1 for none.</summary>
        public int bigIsland = -1;
        /// <summary>Index of the island that carries one huge tree (3x a normal tree), -1 for none.</summary>
        public int hugeTreeIsland = -1;
        /// <summary>Links 0..mainLinks-1 form the required chain; later links lead to optional side islands (dead ends with a bonus).</summary>
        public int mainLinks = -1;
        public int MainLinks => mainLinks >= 0 ? mainLinks : links.Count;
        public int GoalIsland(int isleCount) => goalIsland >= 0 ? goalIsland : isleCount - 1;

        public string Describe()
        {
            var sb = new StringBuilder($"seed {layoutSeed}, {islandCount} islands, water on [{string.Join(",", waterIslands)}]{(collectorOnly ? " (collector only)" : "")}\n");
            for (int i = 0; i < links.Count; i++) sb.AppendLine($"  link {i}: island {links[i].from} -> {i + 1} {links[i].kind} (needs {links[i].seedItem})");
            if (MainLinks < links.Count) sb.AppendLine($"  optional side islands from link {MainLinks} on");
            foreach (var s in sources) sb.AppendLine($"  seeds: island {s.island} {s.item} x{s.drop}");
            return sb.ToString();
        }
    }

    public static class SeedItems
    {
        public static string For(PlantKind k) => k == PlantKind.Glider ? "parachute_leaf" : k == PlantKind.Bridge ? "vine_seed" : k == PlantKind.Ladder ? "beanstalk_seed" : k == PlantKind.Platform ? "shelf_seed" : "dew_seed";
    }

    public static class LevelPlanner
    {
        /// <summary>The hand-authored level (level_5) is a puzzle (7 islands, a side island, 6 gaps); everything else is generated.</summary>
        public static LevelPlan Plan(RunConfig c)
        {
            if (c.levelId == GameBootstrap.HandLevelId) return Level1(c);
            return Generate(c);
        }

        /// <summary>
        /// Gaps in order: A->B bridge, B->S bridge (S is a side island), B->C ladder, C->D platform, D->E bridge, E->F ladder (goal on F).
        /// Seeds do not sit next to the gap they are for: A has the vine tree and the shelf patch, the beanstalk patch is only on S.
        /// </summary>
        static LevelPlan Level1(RunConfig c)
        {
            var p = new LevelPlan { layoutSeed = 20261, islandCount = 7, bigIsland = 3, hugeTreeIsland = 2, collectorOnly = WorldTheme.ResolveWater(c) == WaterMode.CollectorOnly };
            void L(int from, PlantKind k, float h) => p.links.Add(new LinkPlan { from = from, kind = k, heading = h, seedItem = SeedItems.For(k) });
            L(0, PlantKind.Bridge, 0f); L(1, PlantKind.Bridge, -78f); L(1, PlantKind.Ladder, 24f);
            L(3, PlantKind.Platform, 8f); L(4, PlantKind.Bridge, -22f); L(5, PlantKind.Ladder, 28f);
            p.sources.Add(new SeedSourcePlan { island = 0, item = "vine_seed", index = 1, drop = 3, maxAlive = 3 });
            p.sources.Add(new SeedSourcePlan { island = 0, item = "shelf_seed", index = 2, drop = 2, maxAlive = 2 });
            p.sources.Add(new SeedSourcePlan { island = 2, item = "beanstalk_seed", index = 3, drop = 3, maxAlive = 3 });
            p.sources.Add(new SeedSourcePlan { island = 4, item = "parachute_leaf", index = 4, drop = 1, maxAlive = 1 });
            foreach (var i in new[] { 0, 2, 3, 5 }) p.waterIslands.Add(i);
            return p;
        }

        /// <summary>
        /// Random walk chain of `islandCount` islands, one plant kind per gap from allowedKinds. Seeds of each kind lie within two islands before
        /// every gap that needs them (the start island always has two or three sources); water: the start island plus others, never more than
        /// two water-needing gaps between water. `attempt` salts the random so a layout that fails validation can be re-rolled.
        /// </summary>
        public static LevelPlan Generate(RunConfig c, int attempt = 0)
        {
            var rnd = new System.Random(unchecked(c.seed * 73856093) ^ 0x5EED1 ^ unchecked(attempt * 19349663));
            bool fixedSeq = c.sequence != null && c.sequence.Length > 0;
            int n = fixedSeq ? c.sequence.Length + 1 : Mathf.Clamp(c.islandCount < 4 ? 5 : c.islandCount, 4, 8);
            var kinds = new List<PlantKind>();
            if (c.allowedKinds != null) foreach (var k in c.allowedKinds) if (k != PlantKind.Collector && !kinds.Contains(k)) kinds.Add(k);
            if (kinds.Count == 0) kinds.Add(PlantKind.Bridge);

            var p = new LevelPlan { layoutSeed = c.seed ^ unchecked(attempt * (int)0x9E3779B1), islandCount = n, attempt = attempt, collectorOnly = WorldTheme.ResolveWater(c) == WaterMode.CollectorOnly, roundTrip = c.roundTrip, spiral = c.spiral };
            float heading = 0f; PlantKind prev = PlantKind.Collector; int run = 0;
            for (int i = 0; i < n - 1; i++)
            {
                PlantKind k;
                if (fixedSeq) k = c.sequence[i];
                else if (i == 0 && kinds.Contains(PlantKind.Bridge)) k = PlantKind.Bridge;
                else
                {
                    k = kinds[rnd.Next(kinds.Count)];
                    if (k == PlantKind.Bridge && kinds.Count > 1 && rnd.NextDouble() < 0.35)   // lean toward the climbing plants: more height
                    { var up = kinds.FindAll(x => x == PlantKind.Ladder || x == PlantKind.Platform); if (up.Count > 0) k = up[rnd.Next(up.Count)]; }
                    if (k == prev && run >= 2 && kinds.Count > 1) { var others = kinds.FindAll(x => x != prev); k = others[rnd.Next(others.Count)]; }
                }
                run = k == prev ? run + 1 : 1; prev = k;
                if (c.spiral) { heading += 50f; rnd.NextDouble(); }   // T46: a steady turn, curling round the start (the draw is kept so other layouts are unchanged)
                else heading = Mathf.Clamp(heading + ((float)rnd.NextDouble() - 0.5f) * 130f, -100f, 100f);   // wide turns: the chain winds across the sky instead of running in a line
                p.links.Add(new LinkPlan { from = i, kind = k, heading = heading, seedItem = SeedItems.For(k) });
            }

            p.mainLinks = n - 1; p.goalIsland = n - 1;
            // optional side islands: dead ends off a main island that has water (so a charge is always at hand), stocked with a bonus.
            // They use their own random, so the main chain of a seed does not change with them.
            int sides = fixedSeq || c.roundTrip ? c.sideIslands : (n >= 8 ? 1 + (new System.Random(c.seed ^ 0x51DE).Next(2)) : n >= 6 ? 1 : 0) + c.sideIslands;
            var srnd = new System.Random(unchecked(c.seed * 2654435) ^ 0x51DE5 ^ unchecked(attempt * 40503));
            var sideKinds = kinds.FindAll(x => x != PlantKind.Glider);
            if (sideKinds.Count == 0) sideKinds.Add(PlantKind.Bridge);
            var sideFrom = new List<int>();
            for (int sI = 0; sI < sides; sI++)
            {
                int j = 1 + srnd.Next(Mathf.Max(1, n - 2));   // not the start island, not the goal island
                var kd = sideKinds[srnd.Next(sideKinds.Count)];
                float inc = j > 0 ? p.links[j - 1].heading + 180f : 180f, main = p.links[j].heading;
                float h = main + (srnd.Next(2) == 0 ? -1f : 1f) * (80f + (float)srnd.NextDouble() * 70f);
                if (Mathf.Abs(Mathf.DeltaAngle(h, inc)) < 55f) h = main - (h - main);   // mirror to the other side of the lane
                p.links.Add(new LinkPlan { from = j, kind = kd, heading = h, seedItem = SeedItems.For(kd) });
                sideFrom.Add(j);
            }

            // seed supply: for every gap a source of its kind within two islands before it (or on its island); reuse a source already in range
            int idx = 1;
            for (int i = 0; i < p.links.Count; i++)
            {
                var l = p.links[i];
                int at = l.from;   // main chain: from == i
                bool have = false;
                foreach (var s in p.sources) if (s.item == l.seedItem && s.island >= (l.kind == PlantKind.Glider ? at : at - 2) && s.island <= at) { have = true; break; }
                if (have) continue;
                int isl = l.kind == PlantKind.Glider || i >= p.mainLinks ? at : Mathf.Max(0, at - rnd.Next(3));   // the leaf waits on the island you glide from
                p.sources.Add(new SeedSourcePlan { island = isl, item = l.seedItem, index = idx++, drop = l.seedItem == "vine_seed" ? 3 : 2, maxAlive = l.seedItem == "vine_seed" ? 3 : 2 });
            }
            // the start island has two or three trees / patches (user request): pad with the kinds of the first gaps, then a decoy
            int onStart = p.sources.FindAll(s => s.island == 0).Count;
            for (int j = 0; onStart < 2 && j < p.links.Count + kinds.Count; j++)
            {
                string item = j < p.links.Count ? p.links[j].seedItem : SeedItems.For(kinds[j - p.links.Count]);
                if (p.sources.Exists(s => s.island == 0 && s.item == item)) continue;
                p.sources.Add(new SeedSourcePlan { island = 0, item = item, index = idx++, drop = 2, maxAlive = 2 }); onStart++;
            }
            if (onStart < 2) p.sources.Add(new SeedSourcePlan { island = 0, item = p.links[0].seedItem, index = idx++, drop = 2, maxAlive = 2 });

            // water
            p.waterIslands.Add(0);
            int since = 0;   // gaps planted since the last water island
            for (int i = 0; i < n - 1 && !p.collectorOnly; i++)
            {
                if (i > 0 && !p.waterIslands.Contains(i) && (since >= 2 || rnd.NextDouble() < 0.4)) p.waterIslands.Add(i);
                if (p.waterIslands.Contains(i)) since = 0;
                if (p.links[i].kind != PlantKind.Glider) since++;   // gliding costs no water
            }
            // side islands: water of their own and a bonus (the parachute leaf, or a spare seed of another kind), and a water island to leave from
            for (int sI = 0; sI < sideFrom.Count; sI++)
            {
                int isle = n + sI;   // link n-1+sI creates island n+sI
                if (!p.collectorOnly) { p.waterIslands.Add(sideFrom[sI]); p.waterIslands.Add(isle); }
                string bonus = srnd.NextDouble() < 0.5 ? "parachute_leaf" : SeedItems.For(kinds[srnd.Next(kinds.Count)]);
                p.sources.Add(new SeedSourcePlan { island = isle, item = bonus, index = idx++, drop = bonus == "parachute_leaf" ? 1 : 2, maxAlive = bonus == "parachute_leaf" ? 1 : 2 });
            }
            // a parachute leaf bush (single-use glide, never needed for solvability) on one mid island of longer levels
            if (n >= 5 && !fixedSeq) p.sources.Add(new SeedSourcePlan { island = 1 + rnd.Next(n - 2), item = "parachute_leaf", index = idx++, drop = 1, maxAlive = 1 });
            return p;
        }
    }
}
