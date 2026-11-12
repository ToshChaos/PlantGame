using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Plants
{
    /// <summary>
    /// The watering can (contract 5.5): 2 charges, on the Player, not an inventory item. Starts empty; charges survive a respawn.
    /// </summary>
    public class WateringCan : MonoBehaviour
    {
        public const int MaxCharges = 2;

        [SerializeField] int charges = 0;   // the can starts empty: fill it at the first island's water

        public const float RefillSecondsPerCharge = 1.1f;   // the HUD droplets and the can fill gradually; Charges itself changes at once

        float _shown = -1f;

        public int Charges => charges;
        public bool IsFull => charges >= MaxCharges;
        /// <summary>Animated water level 0..MaxCharges: rises toward Charges while refilling, drops quickly after a pour.</summary>
        public float Shown => _shown < 0f ? charges : _shown;
        public bool IsRefilling => Shown < charges - 0.001f;

        void Update()
        {
            if (_shown < 0f) _shown = charges;
            float step = (_shown < charges ? 1f / RefillSecondsPerCharge : 6f) * Time.unscaledDeltaTime;
            _shown = Mathf.MoveTowards(_shown, charges, step);
        }

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
