using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Plants
{
    /// <summary>
    /// STUB from T04 (contract 5.5) so F / refill can be wired and tested. Owner A2 extends this in T27
    /// (WaterSettings, respawn persistence, feedback). On the Player, not an inventory item; starts empty (user request).
    /// </summary>
    public class WateringCan : MonoBehaviour
    {
        public const int MaxCharges = 2;

        [SerializeField] int charges = 0;   // the can starts empty: fill it at the first island's water

        public int Charges => charges;
        public bool IsFull => charges >= MaxCharges;

        public bool TryConsume()
        {
            if (charges <= 0) return false;
            charges--;
            return true;
        }

        /// <summary>Adds up to `amount` charges; returns how many were actually added.</summary>
        public int Refill(int amount)
        {
            int added = Mathf.Clamp(amount, 0, MaxCharges - charges);
            if (added <= 0) return 0;
            charges += added;
            GameEvents.RaiseCanRefilled();
            return added;
        }
    }
}
