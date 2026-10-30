using System;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Inventory
{
    /// <summary>
    /// T05: 4 slots, no stacking (contract 5.5). Lives on the Player. Pure data + selection; input is read by
    /// PlayerInteraction (Tab / Shift+Tab / 1-4 / wheel), which calls Cycle / Select.
    /// TryAdd fills the first empty slot; the selection always moves to the slot the item went into.
    /// A refused add raises GameEvents.InventoryFull.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        public const int Slots = 4;

        readonly ItemDefinition[] _slots = new ItemDefinition[Slots];

        public int Selected { get; private set; }
        public ItemDefinition SelectedItem => _slots[Selected];
        public event Action Changed;

        public ItemDefinition Get(int slot) => slot >= 0 && slot < Slots ? _slots[slot] : null;

        public int Count
        {
            get { int n = 0; for (int i = 0; i < Slots; i++) if (_slots[i] != null) n++; return n; }
        }

        public bool IsFull => Count == Slots;

        public int FirstEmpty
        {
            get { for (int i = 0; i < Slots; i++) if (_slots[i] == null) return i; return -1; }
        }

        public bool TryAdd(ItemDefinition i)
        {
            if (i == null) return false;
            int slot = FirstEmpty;
            if (slot < 0) { GameEvents.RaiseInventoryFull(i); return false; }
            _slots[slot] = i;
            Selected = slot;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Empties the slot and returns what was in it (null for an empty or invalid slot).</summary>
        public ItemDefinition Remove(int slot)
        {
            var item = Get(slot);
            if (item == null) return null;
            _slots[slot] = null;
            Changed?.Invoke();
            return item;
        }

        /// <summary>After a use: selects the next non-empty slot after the current one (wrapping); stays put when no other slot holds anything.</summary>
        public void SelectNextFilled()
        {
            for (int d = 1; d < Slots; d++)
            {
                int i = (Selected + d) % Slots;
                if (_slots[i] != null) { Select(i); return; }
            }
        }

        public void Select(int slot)
        {
            if (slot < 0 || slot >= Slots || slot == Selected) return;
            Selected = slot;
            Changed?.Invoke();
        }

        /// <summary>+1 / -1 with wraparound; empty slots can be selected (5.8).</summary>
        public void Cycle(int delta)
        {
            if (delta == 0) return;
            Select(((Selected + delta) % Slots + Slots) % Slots);
        }

        public void Clear()
        {
            Array.Clear(_slots, 0, Slots);
            Selected = 0;
            Changed?.Invoke();
        }
    }
}
