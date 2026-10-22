using UnityEngine;

namespace PlantGame.Core
{
    /// <summary>Pickup = Q, Use = E (AGENTS.md 5.5 / 5.8).</summary>
    public enum InteractAction { Pickup, Use }

    /// <summary>Anything that Q or E can act on. WorldItem = Pickup; WaterSource, WaterCollector = Use.</summary>
    public interface IInteractable
    {
        InteractAction Action { get; }
        /// <summary>Prompt text, e.g. "Pick up Vine Seed".</summary>
        string Prompt { get; }
        bool CanInteract(GameObject actor);
        void Interact(GameObject actor);
    }
}
