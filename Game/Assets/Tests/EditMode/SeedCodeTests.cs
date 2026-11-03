using NUnit.Framework;
using PlantGame.Core;

namespace PlantGame.Tests
{
    public class SeedCodeTests
    {
        static RunConfig Cfg(int seed, int n, int mask) => new RunConfig { seed = seed, islandCount = n, allowedKinds = SeedCode.KindsFromMask(mask) };

        [Test]
        public void RoundTripKeepsSeedIslandsAndKinds()
        {
            var rnd = new System.Random(7);
            for (int i = 0; i < 2000; i++)
            {
                var c = Cfg(rnd.Next(0, SeedCode.SeedMask + 1), 4 + rnd.Next(5), 1 + rnd.Next(7));
                Assert.IsTrue(SeedCode.TryDecode(SeedCode.Encode(c), out var d, out var err), err);
                Assert.AreEqual(c.seed, d.seed); Assert.AreEqual(c.islandCount, d.islandCount);
                Assert.AreEqual(SeedCode.KindsMask(c.allowedKinds), SeedCode.KindsMask(d.allowedKinds));
                Assert.AreEqual("", err);
            }
        }

        [Test]
        public void DecodeAcceptsSpacesUnderscoresAndCapitals()
        {
            var code = SeedCode.Encode(Cfg(123456, 6, 7));
            Assert.IsTrue(SeedCode.TryDecode(code.ToUpperInvariant().Replace("-", " "), out var a));
            Assert.IsTrue(SeedCode.TryDecode("  " + code.Replace("-", "_") + " ", out var b));
            Assert.AreEqual(123456, a.seed); Assert.AreEqual(123456, b.seed);
        }

        [TestCase(null)] [TestCase("")] [TestCase("   ")] [TestCase("one-two")] [TestCase("a-b-c-d-e-f")] [TestCase("zzzz-zzzz-zzzz-zzzz-zzzz")]
        public void BadInputIsRejectedWithAMessage(string code)
        {
            Assert.IsFalse(SeedCode.TryDecode(code, out _, out var err));
            Assert.IsNotEmpty(err);
        }

        [Test]
        public void SwappingWordsBreaksTheChecksum()
        {
            var w = SeedCode.Encode(Cfg(777777, 5, 3)).Split('-');
            int bad = 0, tried = 0;
            for (int i = 0; i < 5; i++) for (int j = i + 1; j < 5; j++)
            {
                if (w[i] == w[j]) continue;
                var s = (string[])w.Clone(); (s[i], s[j]) = (s[j], s[i]); tried++;
                if (!SeedCode.TryDecode(string.Join("-", s), out _)) bad++;
            }
            Assert.Greater(tried, 0);
            Assert.GreaterOrEqual(bad, tried * 0.6f, "most word swaps should fail the checksum");
        }

        [Test]
        public void WrongGeneratorVersionStillDecodesWithAWarning()
        {
            var code = SeedCode.Encode(4242, 5, 7, (SeedCode.GeneratorVersion + 1) & 0xF);
            Assert.IsTrue(SeedCode.TryDecode(code, out var c, out var err));
            Assert.AreEqual(4242, c.seed); Assert.IsNotEmpty(err);
        }

        [Test]
        public void IslandCountOutOfRangeIsRejected()
        {
            Assert.IsFalse(SeedCode.TryDecode(SeedCode.Encode(1, 3, 7, SeedCode.GeneratorVersion), out _));
            Assert.IsFalse(SeedCode.TryDecode(SeedCode.Encode(1, 9, 7, SeedCode.GeneratorVersion), out _));
        }

        [Test]
        public void NoPlantsMaskIsRejected() => Assert.IsFalse(SeedCode.TryDecode(SeedCode.Encode(1, 5, 0, SeedCode.GeneratorVersion), out _));
    }
}
