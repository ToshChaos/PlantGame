using UnityEngine;
using PlantGame.Inventory;
using PlantGame.Plants;
using PlantGame.Player;

namespace PlantGame.Core
{
    /// <summary>
    /// T04: resolves Q / E / F / Tab exactly as AGENTS.md 5.8 says, using InteractionSensor focus and the Inventory.
    /// Q: focused Pickup interactable + free slot -> pick up; otherwise drop the selected item.
    /// E: selected item + focused IUsableTarget -> UseItem; otherwise focused Use interactable; otherwise UseRejected.
    /// F: focused IWaterable within 3 m that NeedsWater -> spend a charge and Water; empty can -> CanEmptyTried.
    /// Publishes GameEvents.FocusChanged(prompt) whenever the prompt text changes (HUD, T19).
    /// </summary>
    [RequireComponent(typeof(Inventory.Inventory))]
    [RequireComponent(typeof(InteractionSensor))]
    [DefaultExecutionOrder(20)]
    public class PlayerInteraction : MonoBehaviour
    {
        public PlayerInputReader input;
        public Transform model;                 // facing for drops; falls back to this transform
        public float dropDistance = 1.2f;
        public float dropImpulse = 0.6f;
        public float waterRange = 3f;

        public Inventory.Inventory Inventory { get; private set; }
        public InteractionSensor Sensor { get; private set; }
        public WateringCan Can { get; private set; }
        public string CurrentPrompt { get; private set; } = "";
        /// <summary>Key to show next to the prompt: Pickup, Use or Water.</summary>
        public string CurrentPromptKey { get; private set; } = "";
        /// <summary>The player is at (or standing in) a lake / waterfall, or next to a Water Collector with water: F would refill.</summary>
        public bool NearRefillSource { get; private set; }

        void Awake()
        {
            Inventory = GetComponent<Inventory.Inventory>();
            Sensor = GetComponent<InteractionSensor>();
            Can = GetComponent<WateringCan>();
            if (input == null) input = GetComponent<PlayerInputReader>();
        }

        void Update()
        {
            if (input != null)
            {
                int delta = input.CycleDelta;
                if (delta != 0) Inventory.Cycle(delta);
                int slot = input.SlotPressed;
                if (slot >= 0) Inventory.Select(slot);
                if (input.PickupDropPressed) PickupOrDrop();
                if (input.UsePressed) Use();
                if (input.WaterPressed) Water();
            }
            UpdatePrompt();
        }

        public void PickupOrDrop()
        {
            var target = Sensor.Interactable;
            if (target != null && target.Action == InteractAction.Pickup && target.CanInteract(gameObject))
            {
                target.Interact(gameObject);
                return;
            }
            // Looking at a pickup with no free slot: tell UI/SFX it was refused, then fall through to the drop (5.8).
            if (target is WorldItem full && target.Action == InteractAction.Pickup && Inventory.IsFull) GameEvents.RaiseInventoryFull(full.item);
            Drop();
        }

        public bool Drop()
        {
            var item = Inventory.SelectedItem;
            if (item == null || item.worldPrefab == null) { GameEvents.RaiseUseRejected(); return false; }

            Vector3 fwd = (model != null ? model.forward : transform.forward); fwd.y = 0f;
            fwd = fwd.sqrMagnitude < 1e-4f ? transform.forward : fwd.normalized;
            Vector3 chest = transform.position + Vector3.up * 1.0f;
            int solid = LayerMask.GetMask("Default", Layers.GroundName);

            // Never inside geometry: shorten the throw until a small sphere fits, finally drop at the feet.
            Vector3 pos = transform.position + Vector3.up * 0.6f;
            for (float d = dropDistance; d >= 0f; d -= 0.4f)
            {
                Vector3 p = chest + fwd * d;
                if (Physics.Linecast(chest, p, solid, QueryTriggerInteraction.Ignore)) continue;
                if (Physics.CheckSphere(p, 0.25f, solid, QueryTriggerInteraction.Ignore)) continue;
                pos = p; break;
            }

            Inventory.Remove(Inventory.Selected);
            var go = Instantiate(item.worldPrefab, pos, Quaternion.identity);
            var wi = go.GetComponent<WorldItem>();
            if (wi != null) wi.item = item;
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(fwd * dropImpulse, ForceMode.VelocityChange);
            GameEvents.RaiseItemDropped(item);
            return true;
        }

        public void Use()
        {
            var item = Inventory.SelectedItem;
            if (item != null && item.id == "parachute_leaf")
            {
                // E in mid-air with a leaf: glide. The leaf is spent on activation (never reusable); on the ground it is kept.
                var pc = GetComponent<PlayerController>();
                if (pc != null && pc.TryStartGlide()) { Inventory.Remove(Inventory.Selected); Inventory.SelectNextFilled(); GameEvents.RaiseItemUsed(item); return; }
                // on the ground E does nothing with the leaf, except at the goal: that still completes the level
                var goal = Sensor.Interactable as GoalFlag;
                if (goal != null && goal.CanInteract(gameObject)) { goal.Interact(gameObject); return; }
                GameEvents.RaiseHint("The leaf only works in the air: jump off an edge, then press E to glide");
                GameEvents.RaiseUseRejected(); return;
            }
            var usable = Sensor.Usable;
            if (item != null && usable != null && usable.CanUseItem(item, gameObject))
            {
                usable.UseItem(item, gameObject);
                GameEvents.RaiseItemUsed(item);
                if (item.kind == ItemKind.Seed || item.id == "heat_seed") { Inventory.Remove(Inventory.Selected); Inventory.SelectNextFilled(); }   // planting consumes the seed (targets must not remove it themselves)
                // planting only makes a sprout: F waters it (the Dew Seed needs no water)
                return;
            }
            var target = Sensor.Interactable;
            if (target != null && target.Action == InteractAction.Use && !IsRefillTarget(target) && target.CanInteract(gameObject))   // refilling the can is F only (Water)
            {
                target.Interact(gameObject);
                return;
            }
            if (TryThaw()) return;   // E next to frozen water: a Heat Seed melts it
            if ((item != null && item.kind == ItemKind.Seed) || usable is SandboxTarget { frozen: true })
                {
                if (usable is SandboxTarget { frozen: true }) GameEvents.RaiseHint("The soil is frozen: melt it with a Heat Seed first");
                else if (usable == null) GameEvents.RaiseHint("Seeds need a patch of soil: walk up to one and look at it, then press E");
                // wrong seed for this soil: no text, the soil's ring already shows which seed it wants
            }
            GameEvents.RaiseUseRejected();
        }

        /// <summary>The focused interactable is a refill point (lake / waterfall / collector): only F (Water) refills there.</summary>
        bool IsRefillTarget(IInteractable t) => t is WaterSource || t is WaterCollector;

        readonly Collider[] _waterHits = new Collider[48];

        /// <summary>The nearest sprout that still needs water within `waterRange` of the player (distance to its collider), or null.</summary>
        IWaterable NearbyWaterable()
        {
            var chest = Sensor.Chest;
            int n = Physics.OverlapSphereNonAlloc(chest, Mathf.Max(waterRange, 3.5f), _waterHits, LayerMask.GetMask(Layers.InteractableName, Layers.PlantName), QueryTriggerInteraction.Collide);
            IWaterable best = null; float bestD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var w = _waterHits[i].GetComponentInParent<IWaterable>();
                if (w == null || !w.NeedsWater) continue;
                float d = (_waterHits[i].ClosestPoint(chest) - chest).sqrMagnitude;
                if (d < bestD) { bestD = d; best = w; }
            }
            return best;
        }

        public void Water()
        {
            var refill = Sensor.Interactable;
            if (refill != null && IsRefillTarget(refill) && refill.CanInteract(gameObject)) { refill.Interact(gameObject); return; }   // F at water refills the can
            var w = NearbyWaterable();   // F waters the nearest thirsty sprout around the player, no aiming needed
            if (w == null)
            {
                if (FindFrozenSource() != null) { GameEvents.RaiseHint("The water is frozen: it needs a Heat Seed (use it with E)"); GameEvents.RaiseUseRejected(); }
                return;
            }
            if (Can == null || !Can.TryConsume()) { GameEvents.RaiseCanEmptyTried(); return; }
            w.Water(gameObject);
            GameEvents.RaiseWatered();
        }

        /// <summary>T50: a frozen pond within reach (null when none).</summary>
        WaterSource FindFrozenSource()
        {
            int n = Physics.OverlapSphereNonAlloc(Sensor.Chest, 2.5f, _waterHits, LayerMask.GetMask(Layers.InteractableName, Layers.WaterName), QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++) { var ws = _waterHits[i].GetComponentInParent<WaterSource>(); if (ws != null && ws.frozen) return ws; }
            return null;
        }

        /// <summary>E next to frozen water: a selected Heat Seed melts it (and is used up), anything else just explains.</summary>
        bool TryThaw()
        {
            var ice = FindFrozenSource(); if (ice == null) return false;
            var item = Inventory.SelectedItem;
            if (item != null && item.id == "heat_seed")
            {
                ice.Thaw(); Inventory.Remove(Inventory.Selected); Inventory.SelectNextFilled(); GameEvents.RaiseItemUsed(item);
                GameEvents.RaiseHint("The ice melts: the water is yours again");
            }
            else { GameEvents.RaiseHint("The water is frozen: melt it with a Heat Seed"); GameEvents.RaiseUseRejected(); }
            return true;
        }

        bool FindRefillSource()
        {
            int n = Physics.OverlapSphereNonAlloc(Sensor.Chest, 2.5f, _waterHits, LayerMask.GetMask(Layers.InteractableName, Layers.WaterName), QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                var ws = _waterHits[i].GetComponentInParent<WaterSource>();
                if (ws != null && !ws.frozen) return true;   // frozen water offers no refill hint
                var c = _waterHits[i].GetComponentInParent<WaterCollector>();
                if (c != null && c.Stored > 0) return true;
            }
            return false;
        }

        void UpdatePrompt()
        {
            NearRefillSource = FindRefillSource();
            string p = "", k = "";
            var target = Sensor.Interactable;
            if (target != null && target.CanInteract(gameObject)) { p = target.Prompt; k = target.Action == InteractAction.Pickup ? "Pickup" : IsRefillTarget(target) ? "Water" : "Use"; }
            else if (Sensor.Usable != null && Inventory.SelectedItem != null && Sensor.Usable.CanUseItem(Inventory.SelectedItem, gameObject))
            { p = "Use " + Inventory.SelectedItem.displayName; k = "Use"; }
            else if (NearbyWaterable() != null) { p = "Water sprout"; k = "Water"; }
            else if (FindFrozenSource() != null)   // frozen pond: say so, and what melts it
            {
                var held = Inventory.SelectedItem;
                if (held != null && held.id == "heat_seed") { p = "Melt the ice with the Heat Seed"; k = "Use"; }
                else { p = "Frozen water: it needs a Heat Seed"; k = "Frozen"; }
            }
            CurrentPromptKey = k;
            if (p == CurrentPrompt) return;
            CurrentPrompt = p;
            GameEvents.RaiseFocusChanged(p);
        }
    }
}
