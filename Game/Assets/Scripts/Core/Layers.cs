using UnityEngine;

namespace PlantGame.Core
{
    /// <summary>
    /// Layer / tag names from AGENTS.md 5.6. The layers are created by the editor menu
    /// "PlantGame > 1 Setup Project" at fixed indices (8..13) so masks are stable.
    /// </summary>
    public static class Layers
    {
        public const string GroundName = "Ground";
        public const string PlantName = "Plant";
        public const string ClimbableName = "Climbable";
        public const string InteractableName = "Interactable";
        public const string WaterName = "Water";
        public const string PlayerName = "Player";

        public const string PlayerTag = "Player";
        public const string RespawnTag = "Respawn";

        /// <summary>Everything the player can stand on (cached: the layers sit at fixed indices).</summary>
        public static int WalkableMask => _walkable != 0 ? _walkable : _walkable = LayerMask.GetMask("Default", GroundName, PlantName);
        static int _walkable;
        /// <summary>Everything the player can act on: interactables, water and plants (focus, marker circles, watering).</summary>
        public static int InteractMask => _interact != 0 ? _interact : _interact = LayerMask.GetMask(InteractableName, WaterName, PlantName);
        static int _interact;
        /// <summary>Solid scenery that blocks the focus line of sight or a throw.</summary>
        public static int SolidMask => _solid != 0 ? _solid : _solid = LayerMask.GetMask("Default", GroundName);
        static int _solid;
    }
}
