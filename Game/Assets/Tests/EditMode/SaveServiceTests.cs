using System.IO;
using NUnit.Framework;
using PlantGame.Core;

namespace PlantGame.Tests
{
    public class SaveServiceTests
    {
        string _path;

        [SetUp] public void SetUp() { _path = Path.Combine(Path.GetTempPath(), "pg_save_" + System.Guid.NewGuid().ToString("N") + ".json"); SaveService.PathOverride = _path; }
        [TearDown] public void TearDown() { SaveService.PathOverride = null; if (File.Exists(_path)) File.Delete(_path); }

        [Test] public void NoTimeBeforeAnyRun() => Assert.IsNull(SaveService.BestTime("level_1", GameMode.Exploration));

        [Test]
        public void FirstTimeIsABest()
        {
            Assert.IsTrue(SaveService.TrySubmitTime("level_1", GameMode.Exploration, 100f));
            Assert.AreEqual(100f, SaveService.BestTime("level_1", GameMode.Exploration));
        }

        [Test]
        public void SlowerOrEqualTimeKeepsTheBest()
        {
            SaveService.TrySubmitTime("k", GameMode.Challenge, 100f);
            Assert.IsFalse(SaveService.TrySubmitTime("k", GameMode.Challenge, 120f));
            Assert.IsFalse(SaveService.TrySubmitTime("k", GameMode.Challenge, 100f));
            Assert.AreEqual(100f, SaveService.BestTime("k", GameMode.Challenge));
        }

        [Test]
        public void FasterTimeReplacesTheBest()
        {
            SaveService.TrySubmitTime("k", GameMode.Challenge, 100f);
            Assert.IsTrue(SaveService.TrySubmitTime("k", GameMode.Challenge, 80f));
            Assert.AreEqual(80f, SaveService.BestTime("k", GameMode.Challenge));
        }

        [Test]
        public void ModesAndKeysAreIndependent()
        {
            SaveService.TrySubmitTime("a", GameMode.Exploration, 50f);
            Assert.IsNull(SaveService.BestTime("a", GameMode.Challenge));
            Assert.IsNull(SaveService.BestTime("b", GameMode.Exploration));
        }

        [Test]
        public void ResetClearsTimesButNotTheLastSeedCode()
        {
            SaveService.TrySubmitTime("a", GameMode.Exploration, 50f); SaveService.SetLastSeedCode("x-y-z");
            SaveService.ResetTimes();
            Assert.IsNull(SaveService.BestTime("a", GameMode.Exploration));
            Assert.IsTrue(File.ReadAllText(_path).Contains("x-y-z"));
        }

        [Test]
        public void CorruptFileStartsFresh()
        {
            File.WriteAllText(_path, "{ not json");
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try { Assert.IsNull(SaveService.BestTime("a", GameMode.Exploration)); Assert.IsTrue(SaveService.TrySubmitTime("a", GameMode.Exploration, 9f)); }
            finally { UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false; }
        }
    }
}
