using NUnit.Framework;
using UnityEngine;
using PlantGame.Core;
using PlantGame.Inventory;

namespace PlantGame.Tests
{
    public class InventoryTests
    {
        GameObject _go;
        Inventory.Inventory _inv;
        ItemDefinition _a, _b, _c, _d, _e;

        static ItemDefinition Item(string id) { var i = ScriptableObject.CreateInstance<ItemDefinition>(); i.id = id; i.displayName = id; return i; }

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("inv");
            _inv = _go.AddComponent<Inventory.Inventory>();
            _a = Item("a"); _b = Item("b"); _c = Item("c"); _d = Item("d"); _e = Item("e");
        }

        [TearDown]
        public void TearDown() { Object.DestroyImmediate(_go); }

        [Test] public void HasExactlyFourSlots() => Assert.AreEqual(4, Inventory.Inventory.Slots);

        [Test]
        public void AddFillsFirstEmptySlot()
        {
            Assert.IsTrue(_inv.TryAdd(_a)); Assert.IsTrue(_inv.TryAdd(_b));
            Assert.AreSame(_a, _inv.Get(0)); Assert.AreSame(_b, _inv.Get(1));
            _inv.Remove(0);
            _inv.TryAdd(_c);
            Assert.AreSame(_c, _inv.Get(0));
        }

        [Test]
        public void FifthPickupIsRefusedWithEvent()
        {
            ItemDefinition refused = null;
            System.Action<ItemDefinition> h = i => refused = i;
            GameEvents.InventoryFull += h;
            try
            {
                _inv.TryAdd(_a); _inv.TryAdd(_b); _inv.TryAdd(_c); _inv.TryAdd(_d);
                Assert.IsFalse(_inv.TryAdd(_e));
                Assert.AreSame(_e, refused);
                Assert.AreEqual(4, _inv.Count);
            }
            finally { GameEvents.InventoryFull -= h; }
        }

        [Test]
        public void RemoveFreesSlotAndReturnsItem()
        {
            _inv.TryAdd(_a);
            Assert.AreSame(_a, _inv.Remove(0));
            Assert.IsNull(_inv.Get(0));
            Assert.IsNull(_inv.Remove(0));
            Assert.IsNull(_inv.Remove(7));
        }

        [Test]
        public void CycleWrapsBothWays()
        {
            Assert.AreEqual(0, _inv.Selected);
            _inv.Cycle(1); _inv.Cycle(1); _inv.Cycle(1); Assert.AreEqual(3, _inv.Selected);
            _inv.Cycle(1); Assert.AreEqual(0, _inv.Selected);
            _inv.Cycle(-1); Assert.AreEqual(3, _inv.Selected);
        }

        [Test]
        public void SelectIgnoresInvalidSlots()
        {
            _inv.Select(2); _inv.Select(9); _inv.Select(-1);
            Assert.AreEqual(2, _inv.Selected);
        }

        [Test]
        public void ChangedFiresOnAddRemoveSelect()
        {
            int n = 0; _inv.Changed += () => n++;
            _inv.TryAdd(_a); _inv.Select(2); _inv.Remove(0);
            Assert.AreEqual(3, n);
        }

        [Test]
        public void AddMovesSelectionOntoAnEmptySelectedSlot()
        {
            _inv.TryAdd(_a);
            Assert.AreEqual(0, _inv.Selected);
            _inv.Select(2);                       // empty slot selected
            _inv.TryAdd(_b);                      // goes to slot 1 (first empty); selection stays on the empty slot 2? -> moves to the new item
            Assert.AreSame(_b, _inv.SelectedItem);
        }

        [Test]
        public void AddSelectsTheNewSlotEvenWhenAnotherItemWasSelected()
        {
            _inv.TryAdd(_a);
            _inv.TryAdd(_b);
            Assert.AreEqual(1, _inv.Selected);
            Assert.AreSame(_b, _inv.SelectedItem);
        }
    }
}
