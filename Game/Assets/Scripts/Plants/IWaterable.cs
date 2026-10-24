using UnityEngine;

namespace PlantGame.Plants
{
    /// <summary>Sprouts (contract 5.5 / 5.2b). F on a focused IWaterable within 3 m spends one can charge when NeedsWater.</summary>
    public interface IWaterable
    {
        bool NeedsWater { get; }
        void Water(GameObject actor);
    }
}
