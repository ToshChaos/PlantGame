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

        public static int Ground => LayerMask.NameToLayer(GroundName);
        public static int Plant => LayerMask.NameToLayer(PlantName);
        public static int Climbable => LayerMask.NameToLayer(ClimbableName);
        public static int Interactable => LayerMask.NameToLayer(InteractableName);
        public static int Water => LayerMask.NameToLayer(WaterName);
        public static int Player => LayerMask.NameToLayer(PlayerName);

        /// <summary>Everything the player can stand on.</summary>
        public static int WalkableMask => LayerMask.GetMask("Default", GroundName, PlantName);
        public static int InteractableMask => LayerMask.GetMask(InteractableName, WaterName);
    }
}
