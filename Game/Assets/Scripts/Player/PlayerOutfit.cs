using UnityEngine;

namespace PlantGame.Player
{
    /// <summary>Material slots of the player model. The Blender meshes end in the role name (Head_Skin, Hat_Gear, ...).</summary>
    public enum PlayerRole { Skin, Hair, Cloth, Cloak, Gear, Trim, Glow, Leaf, Dark }

    /// <summary>
    /// A reskin of the player: one material per role. The geometry and animation stay the same, so a new look
    /// (a Heart Tree robe, a winter coat, ...) is a new outfit asset (PlantGame > 4 Build Player Prefab creates Outfit_Explorer).
    /// </summary>
    [CreateAssetMenu(menuName = "PlantGame/Player Outfit", fileName = "Outfit_New")]
    public class PlayerOutfit : ScriptableObject
    {
        public Material[] materials = new Material[System.Enum.GetValues(typeof(PlayerRole)).Length];

        public Material Get(PlayerRole role) { int i = (int)role; return i < materials.Length ? materials[i] : null; }

        /// <summary>Role from a mesh name ("Hat_Gear" -> Gear); false when the suffix is not a role.</summary>
        public static bool TryRole(string meshName, out PlayerRole role)
        {
            int u = meshName.LastIndexOf('_');
            return System.Enum.TryParse(u >= 0 ? meshName.Substring(u + 1) : "", out role);
        }
    }
}
