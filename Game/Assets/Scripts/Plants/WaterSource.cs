using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Plants
{
    public enum WaterKind { Lake, Waterfall }

    /// <summary>
    /// Contract 5.5: unlimited water (lake, waterfall, sandbox puddle). F (Water) fills the watering can instantly.
    /// Minimal version for the sandbox; T27/T29 add the kit pieces and feedback. Layer Water, trigger collider.
    /// </summary>
    public class WaterSource : MonoBehaviour, IInteractable
    {
        public WaterKind kind;
        [Tooltip("T45/T50: frozen water cannot refill the can and gives no refill hint until a Heat Seed melts it.")] public bool frozen;
        [System.NonSerialized] public Renderer surface; [System.NonSerialized] public Material liquid;
        static Material _ice;
        public static Material IceMat()
        {
            if (_ice != null) return _ice;
            _ice = RuntimeMats.NewLit("M_Ice", new Color(0.80f, 0.92f, 1f));
            if (_ice.HasProperty("_Smoothness")) _ice.SetFloat("_Smoothness", 0.95f);
            _ice.EnableKeyword("_EMISSION"); _ice.SetColor("_EmissionColor", new Color(0.45f, 0.75f, 1f) * 0.45f);   // a cold glow marks frozen water from a distance
            return _ice;
        }
        public void Freeze() { frozen = true; if (surface != null) surface.sharedMaterial = IceMat(); }
        /// <summary>Melts the ice: the pond shows its water again and can refill the can.</summary>
        public void Thaw() { frozen = false; if (surface != null && liquid != null) surface.sharedMaterial = liquid; }

        public InteractAction Action => InteractAction.Use;
        public string Prompt => frozen ? "Frozen" : "Refill watering can";

        public bool CanInteract(GameObject actor)
        {
            var can = actor != null ? actor.GetComponent<WateringCan>() : null;
            return !frozen && can != null && !can.IsFull;
        }

        public void Interact(GameObject actor)
        {
            var can = actor != null ? actor.GetComponent<WateringCan>() : null;
            if (can != null) can.Refill(WateringCan.MaxCharges);
        }
    }
}
