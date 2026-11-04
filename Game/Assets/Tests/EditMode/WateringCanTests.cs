using NUnit.Framework;
using UnityEngine;
using PlantGame.Plants;

namespace PlantGame.Tests
{
    public class WateringCanTests
    {
        GameObject _go; WateringCan _can;
        [SetUp] public void Up() { _go = new GameObject("can"); _can = _go.AddComponent<WateringCan>(); }
        [TearDown] public void Down() { Object.DestroyImmediate(_go); }

        [Test] public void StartsEmptyAndRefusesToPour() { Assert.AreEqual(0, _can.Charges); Assert.IsFalse(_can.TryConsume()); }

        [Test] public void RefillIsCappedAtMaxCharges()
        {
            Assert.AreEqual(WateringCan.MaxCharges, _can.Refill(99));
            Assert.IsTrue(_can.IsFull);
            Assert.AreEqual(0, _can.Refill(1));
        }

        [Test] public void RefillReturnsOnlyWhatFitted()
        {
            Assert.AreEqual(1, _can.Refill(1));
            Assert.AreEqual(1, _can.Refill(5));
            Assert.AreEqual(WateringCan.MaxCharges, _can.Charges);
        }

        [Test] public void NegativeRefillAddsNothing() { Assert.AreEqual(0, _can.Refill(-3)); Assert.AreEqual(0, _can.Charges); }

        [Test] public void EachPourCostsOneCharge()
        {
            _can.Refill(2);
            Assert.IsTrue(_can.TryConsume()); Assert.AreEqual(1, _can.Charges);
            Assert.IsTrue(_can.TryConsume()); Assert.AreEqual(0, _can.Charges);
            Assert.IsFalse(_can.TryConsume());
        }
    }
}
