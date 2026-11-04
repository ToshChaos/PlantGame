using NUnit.Framework;
using PlantGame.Core;

namespace PlantGame.Tests
{
    public class WorldThemeTests
    {
        static RunConfig Cfg(int seed) => new RunConfig { seed = seed, islandCount = 6, allowedKinds = SeedCode.KindsFromMask(7) };

        [Test] public void RandomLevelSettingsAreDeterministic()
        {
            for (int s = 0; s < 200; s++)
            {
                var c = Cfg(s * 7919);
                Assert.AreEqual(WorldTheme.Resolve(c), WorldTheme.Resolve(c));
                Assert.AreEqual(WorldTheme.ResolveWater(c), WorldTheme.ResolveWater(c));
                Assert.AreEqual(WorldTheme.ResolveWind(c), WorldTheme.ResolveWind(c));
                Assert.AreEqual(WorldTheme.ResolveGoal(c), WorldTheme.ResolveGoal(c));
            }
        }

        [Test] public void RandomLevelsUseMoreThanOneTheme()
        {
            var seen = new System.Collections.Generic.HashSet<LevelTheme>();
            for (int s = 0; s < 200; s++) seen.Add(WorldTheme.Resolve(Cfg(s * 7919)));
            Assert.Greater(seen.Count, 1);
        }
    }
}
