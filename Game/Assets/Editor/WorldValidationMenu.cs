using System.Diagnostics;
using System.Text;
using UnityEditor;
using PlantGame.Core;
using PlantGame.Plants;
using PlantGame.World;

namespace PlantGame.Editor
{
    /// <summary>Menu PlantGame > 15 Validate Seeds: SeedCode round trips and N generated levels through planner, layout and validator (nothing is built).</summary>
    public static class WorldValidationMenu
    {
        [MenuItem("PlantGame/16 Validate Seeds", false, 16)]
        public static void Run() { UnityEngine.Debug.Log(Report(200)); }

        public static string Report(int seeds)
        {
            var sb = new StringBuilder();
            var rnd = new System.Random(1);
            int codeFail = 0;
            for (int i = 0; i < 10000; i++)
            {
                var c = new RunConfig { seed = rnd.Next(0, SeedCode.SeedMask + 1), islandCount = 4 + rnd.Next(5), allowedKinds = SeedCode.KindsFromMask(1 + rnd.Next(7)) };
                var code = SeedCode.Encode(c);
                if (!SeedCode.TryDecode(code.ToUpperInvariant().Replace("-", " "), out var d) || d.seed != c.seed || d.islandCount != c.islandCount || SeedCode.KindsMask(d.allowedKinds) != SeedCode.KindsMask(c.allowedKinds)) codeFail++;
            }
            sb.AppendLine($"SeedCode round trip: {codeFail} failures of 10000");
            int bad = 0, rerolled = 0; var sw = Stopwatch.StartNew();
            for (int s = 0; s < seeds; s++)
            {
                var c = new RunConfig { seed = 1000 + s * 37, islandCount = 4 + s % 5, allowedKinds = SeedCode.KindsFromMask(1 + s % 7), waterMode = WaterMode.Sources };
                var plan = WorldGenerator.MakePlan(c, out var lay, out _);
                var issues = WorldValidator.Validate(plan, lay);
                if (plan.attempt > 0) rerolled++;
                if (issues.Count > 0) { bad++; if (bad <= 5) sb.AppendLine($"seed {c.seed} n={c.islandCount}: {string.Join("; ", issues)}"); }
            }
            sb.AppendLine($"{seeds} seeds: {bad} invalid, {rerolled} needed a re-roll, {sw.ElapsedMilliseconds} ms");
            return sb.ToString();
        }
    }
}
