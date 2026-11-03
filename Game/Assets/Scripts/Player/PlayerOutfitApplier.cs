using UnityEngine;

namespace PlantGame.Player
{
    /// <summary>On the player's Model: assigns the outfit's materials to every role-named mesh (also callable at runtime to switch outfits).</summary>
    public class PlayerOutfitApplier : MonoBehaviour
    {
        public PlayerOutfit outfit;

        void Awake() { Apply(); }

        public void SetOutfit(PlayerOutfit o) { outfit = o; Apply(); }

        public void Apply()
        {
            if (outfit == null) return;
            foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
                if (PlayerOutfit.TryRole(r.name, out var role) && outfit.Get(role) != null) r.sharedMaterial = outfit.Get(role);
        }
    }
}
