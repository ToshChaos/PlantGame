using System.Linq;
using NUnit.Framework;
using UnityEditor;
using PlantGame.Core;
using PlantGame.World;

namespace PlantGame.Tests
{
    /// <summary>Regression checks over the shipped curated levels (ScriptableObjects/World).</summary>
    public class CuratedLevelTests
    {
        static LevelDefinition[] Levels() =>
            AssetDatabase.FindAssets("t:LevelDefinition", new[] { "Assets/ScriptableObjects/World" })
                .Select(g => AssetDatabase.LoadAssetAtPath<LevelDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(l => l != null).OrderBy(l => l.id).ToArray();

        /// <summary>The hand-authored level (GameBootstrap.HandLevelId) and the 3-island tutorial (level_1) break the random-level rules the validator checks (it wants 2-3 seed sources on the start island), so those checks skip them.</summary>
        static LevelDefinition[] Generated() => Levels().Where(l => l.id != GameBootstrap.HandLevelId && l.islandCount >= 4).ToArray();

        [Test]
        public void TenCuratedLevelsExistWithUniqueIds()
        {
            var ls = Levels();
            Assert.AreEqual(10, ls.Length, "curated level count changed: update this test on purpose");
            Assert.AreEqual(ls.Length, ls.Select(l => l.id).Distinct().Count(), "duplicate level id");
            foreach (var l in ls) { Assert.IsFalse(string.IsNullOrEmpty(l.id), l.name); Assert.IsFalse(string.IsNullOrEmpty(l.title), l.id); }
        }

        [Test]
        public void CuratedLevelsStayInRange()
        {
            foreach (var l in Levels())
            {
                Assert.That(l.islandCount, Is.InRange(3, 8), l.id);
                Assert.That(l.seed, Is.InRange(0, SeedCode.SeedMask), l.id + " seed does not fit a seed code");
                Assert.IsNotEmpty(l.allowedKinds, l.id);
            }
        }

        [Test]
        public void CuratedLevelsAreBeatable()
        {
            foreach (var l in Generated())
            {
                var c = l.ToConfig();
                var plan = WorldGenerator.MakePlan(c, out var lay, out _);
                Assert.IsEmpty(WorldValidator.Validate(plan, lay), l.id);
            }
        }

        [Test]
        public void CuratedLevelPlansAreDeterministic()
        {
            foreach (var l in Generated())
                Assert.AreEqual(WorldGenerationTests.Fingerprint(l.ToConfig()), WorldGenerationTests.Fingerprint(l.ToConfig()), l.id);
        }
    }
}
