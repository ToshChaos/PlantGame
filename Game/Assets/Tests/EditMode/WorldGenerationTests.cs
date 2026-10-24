using System.Text;
using NUnit.Framework;
using UnityEngine;
using PlantGame.Core;
using PlantGame.World;

namespace PlantGame.Tests
{
    public class WorldGenerationTests
    {
        static RunConfig Cfg(int seed, int n, int mask) => new RunConfig { seed = seed, islandCount = n, allowedKinds = SeedCode.KindsFromMask(mask), waterMode = WaterMode.Sources };

        /// <summary>Stable summary of a world's plan and island positions (FNV-1a over the text).</summary>
        public static string Fingerprint(RunConfig c)
        {
            var plan = WorldGenerator.MakePlan(c, out var lay, out _);
            var sb = new StringBuilder(plan.Describe());
            foreach (var i in lay.isles) sb.Append($"{i.centre.x:0.00},{i.centre.y:0.00},{i.centre.z:0.00};");
            uint h = 2166136261; foreach (char ch in sb.ToString()) { h ^= ch; h *= 16777619; }
            return $"{plan.islandCount}i/{plan.links.Count}l/{h:X8}";
        }

        [Test]
        public void SameConfigGivesTheSameWorld()
        {
            for (int s = 0; s < 20; s++) Assert.AreEqual(Fingerprint(Cfg(5000 + s * 13, 4 + s % 5, 1 + s % 7)), Fingerprint(Cfg(5000 + s * 13, 4 + s % 5, 1 + s % 7)));
        }

        [Test]
        public void DifferentSeedsGiveDifferentWorlds()
        {
            Assert.AreNotEqual(Fingerprint(Cfg(1, 6, 7)), Fingerprint(Cfg(2, 6, 7)));
        }

        [Test]
        public void HundredSeedsValidate()
        {
            for (int s = 0; s < 100; s++)
            {
                var c = Cfg(1000 + s * 37, 4 + s % 5, 1 + s % 7);
                var plan = WorldGenerator.MakePlan(c, out var lay, out _);
                var issues = WorldValidator.Validate(plan, lay);
                Assert.IsEmpty(issues, $"seed {c.seed} n={c.islandCount}: {string.Join("; ", issues)}");
            }
        }

        [Test]
        public void CollectorOnlySeedsValidate()
        {
            for (int s = 0; s < 30; s++)
            {
                var c = Cfg(9000 + s * 11, 4 + s % 5, 7); c.waterMode = WaterMode.CollectorOnly;
                var plan = WorldGenerator.MakePlan(c, out var lay, out _);
                Assert.IsEmpty(WorldValidator.Validate(plan, lay), "seed " + c.seed);
            }
        }

        // Golden worlds: the seed-code promise. If one of these changes, a generator change altered existing worlds:
        // bump SeedCode.GeneratorVersion if that was intended, then re-record the values (run this test and copy the actual).
        [TestCase(1101, 3, 1, "5i/4l/0EEAF62A")]
        [TestCase(14032468, 8, 7, "8i/9l/4CD236CC")]
        [TestCase(15386617, 8, 7, "8i/9l/E982576F")]
        [TestCase(4299, 8, 7, "8i/9l/3F7D02CB")]
        [TestCase(123456, 5, 7, "5i/4l/8507F029")]
        [TestCase(7777, 6, 3, "6i/6l/4EEFD0D9")]
        public void GoldenSeedsKeepTheirWorld(int seed, int n, int mask, string expected)
        {
            Assert.AreEqual(expected, Fingerprint(Cfg(seed, n, mask)));
        }
    }
}
