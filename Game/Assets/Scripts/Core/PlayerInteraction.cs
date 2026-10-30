using System.Collections.Generic;
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
    /// Auto water option (GameSettings.AutoWater): the same F actions run by themselves when water or a thirsty sprout is within reach.
    /// CurrentPrompt / CurrentPromptKey hold the prompt the HUD shows (T19).
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

        public Inventory.Inventory Inventory { get; private set; }
        public InteractionSensor Sensor { get; private set; }
        public WateringCan Can { get; private set; }
        public string CurrentPrompt { get; private set; } = "";
        /// <summary>Key to show next to the prompt: Pickup, Use or Water.</summary>
        public string CurrentPromptKey { get; private set; } = "";
        public bool CurrentPromptIsGoal { get; private set; }   // the level goal prompt stays visible with hints off
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
            if (Sensor.facing == null) Sensor.facing = model;
            if (input != null)
            {
                if (TouchInput.HasTap) HandleTap(TouchInput.Tap);
                if (TouchInput.JumpPressed) TryGlideOnTouch();
                int delta = input.CycleDelta;
                if (delta != 0) Inventory.Cycle(delta);
                int slot = input.SlotPressed;
                if (slot >= 0) Inventory.Select(slot);
                if (input.PickupDropPressed) PickupOrDrop();
                if (input.UsePressed) Use();
                if (input.WaterPressed) Water();
            }
            int n = OverlapNear();
            AutoWater(n);
            UpdatePrompt(n);
        }

        const float AutoWaterPause = 0.6f;   // between two automatic waterings / refills, so the can tip and the sounds do not overlap
        float _autoWaterAt;

        /// <summary>Auto water option: waters the nearest thirsty sprout within reach (if the can holds water), else refills the can at water within reach.</summary>
        void AutoWater(int n)
        {
            if (!GameSettings.AutoWater || Can == null || Time.timeScale <= 0f || Time.time < _autoWaterAt) return;
            var w = Can.Charges > 0 ? NearbyWaterable(n) : null;
            if (w != null && Can.TryConsume()) { w.Water(gameObject); GameEvents.RaiseWatered(); _autoWaterAt = Time.time + AutoWaterPause; return; }
            var refill = Can.IsFull ? null : FindRefillSource(n);
            if (refill != null && refill.CanInteract(gameObject)) { refill.Interact(gameObject); _autoWaterAt = Time.time + AutoWaterPause; }
        }

        /// <summary>Touch: a second jump tap in the air opens the parachute leaf (there is no E key).</summary>
        void TryGlideOnTouch()
        {
            var sel = Inventory.SelectedItem;
            var pc = GetComponent<PlayerController>();
            if (sel != null && sel.id == "parachute_leaf" && pc != null && pc.IsAirborne && !pc.IsGliding) Use();
        }

        /// <summary>
        /// Touch: the player tapped the circle / diamond over a target that is within reach. The target becomes the focus, then the
        /// same checks as the keys run (free slot, right seed, water in the can, not frozen, ...).
        /// </summary>
        void HandleTap(InteractionSensor.Target t)
        {
            Sensor.Override(t);
            if (t.waterable != null && t.waterable.NeedsWater) { Water(); return; }               // a thirsty sprout (also a planted soil patch)
            if (t.usable != null && t.interactable == null) { Use(); return; }                      // soil: plant the selected seed
            var i = t.interactable;
            if (i == null) return;
            if (i is WaterSource ws)
            {
                if (!ws.WaterNear(Sensor.Chest)) TryThaw(); else if (i.CanInteract(gameObject)) i.Interact(gameObject); else GameEvents.RaiseUseRejected();
            }
            else if (IsRefillTarget(i)) { if (i.CanInteract(gameObject)) i.Interact(gameObject); else GameEvents.RaiseUseRejected(); }   // collector
            else if (i.Action == InteractAction.Pickup)
            {
                if (i.CanInteract(gameObject)) i.Interact(gameObject);
                else { if (i is WorldItem full && Inventory.IsFull) GameEvents.RaiseInventoryFull(full.item); GameEvents.RaiseUseRejected(); }
            }
            else Use();   // the goal flag (and the leaf at the goal)
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
            int solid = Layers.SolidMask;

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

        readonly List<CapHops.Option> _hops = new List<CapHops.Option>();
        /// <summary>E near a grown mushroom patch: hop onto the cap best in line with the view.</summary>
        bool TryCapHop()
        {
            var pc = GetComponent<PlayerController>();
            if (pc == null || pc.IsHopping || pc.IsClimbing) return false;
            CapHops.Find(transform.position, _hops);
            var f = Sensor.facing != null ? Sensor.facing : (Camera.main != null ? Camera.main.transform : transform);
            var view = Camera.main != null && !TouchInput.Active ? Camera.main.transform.forward : f.forward;
            if (!CapHops.Pick(_hops, transform.position, view, out var o)) return false;
            pc.HopTo(o.point);
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
            if (TryCapHop()) return;
            if (TryThaw()) return;   // E next to frozen water: a Heat Seed melts it
            if ((item != null && item.kind == ItemKind.Seed) || usable is SandboxTarget { frozen: true })
                {
                if (usable is SandboxTarget { frozen: true }) GameEvents.RaiseHint("Frozen soil, needs heat");
                else if (usable == null) GameEvents.RaiseHint("Plants need a patch of soil: walk up to one and look at it, then press E");
                // wrong seed for this soil: no text, the soil's ring already shows which seed it wants
            }
            GameEvents.RaiseUseRejected();
        }

        /// <summary>The focused interactable is a refill point (lake / waterfall / collector): only F (Water) refills there.</summary>
        bool IsRefillTarget(IInteractable t) => t is WaterSource || t is WaterCollector;

        readonly Collider[] _waterHits = new Collider[64];
        static int _sproutLayers, _waterLayers;
        static int SproutLayers => _sproutLayers != 0 ? _sproutLayers : _sproutLayers = LayerMask.GetMask(Layers.InteractableName, Layers.PlantName);
        static int WaterLayers => _waterLayers != 0 ? _waterLayers : _waterLayers = LayerMask.GetMask(Layers.InteractableName, Layers.WaterName);
        static bool OnLayers(Collider c, int mask) => (mask & (1 << c.gameObject.layer)) != 0;

        /// <summary>One overlap within Sensor.range of the chest fills _waterHits for the sprout, refill and ice lookups below (returns the hit count).</summary>
        int OverlapNear() => Physics.OverlapSphereNonAlloc(Sensor.Chest, Sensor.range, _waterHits, Layers.InteractMask, QueryTriggerInteraction.Collide);

        /// <summary>The nearest sprout that still needs water among the first n _waterHits (distance to its collider), or null.</summary>
        IWaterable NearbyWaterable(int n)
        {
            var chest = Sensor.Chest;
            IWaterable best = null; float bestD = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                if (!OnLayers(_waterHits[i], SproutLayers)) continue;
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
            int n = OverlapNear();
            var w = NearbyWaterable(n);   // F waters the nearest thirsty sprout around the player, no aiming needed
            if (w == null)
            {
                if (FindFrozenSource(n) != null) { GameEvents.RaiseHint("The water is frozen: it needs Heat (press E to melt it)"); GameEvents.RaiseUseRejected(); }
                return;
            }
            if (Can == null || !Can.TryConsume()) { GameEvents.RaiseCanEmptyTried(); return; }
            w.Water(gameObject);
            GameEvents.RaiseWatered();
        }

        /// <summary>T50: a frozen pond within reach (null when none).</summary>
        WaterSource FindFrozenSource(int n)
        {
            for (int i = 0; i < n; i++) { if (!OnLayers(_waterHits[i], WaterLayers)) continue; var ws = _waterHits[i].GetComponentInParent<WaterSource>(); if (ws != null && ws.frozen && ws.FrozenNear(Sensor.Chest)) return ws; }
            return null;
        }

        /// <summary>E next to frozen water: a selected Heat Seed melts it (and is used up), anything else just explains.</summary>
        bool TryThaw()
        {
            var ice = FindFrozenSource(OverlapNear()); if (ice == null) return false;
            var item = Inventory.SelectedItem;
            if (item != null && item.id == "heat_seed")
            {
                ice.ThawNear(Sensor.Chest); Inventory.Remove(Inventory.Selected); Inventory.SelectNextFilled(); GameEvents.RaiseItemUsed(item);
                GameEvents.RaiseHint("The ice melts: this patch of water is yours");
            }
            else { GameEvents.RaiseHint("The water is frozen: melt it with Heat"); GameEvents.RaiseUseRejected(); }
            return true;
        }

        /// <summary>Open water or a Water Collector holding a charge among the first n _waterHits (null when none).</summary>
        IInteractable FindRefillSource(int n)
        {
            for (int i = 0; i < n; i++)
            {
                if (!OnLayers(_waterHits[i], WaterLayers)) continue;
                var ws = _waterHits[i].GetComponentInParent<WaterSource>();
                if (ws != null && ws.WaterNear(Sensor.Chest)) return ws;   // frozen water offers no refill hint
                var c = _waterHits[i].GetComponentInParent<WaterCollector>();
                if (c != null && c.Stored > 0) return c;
            }
            return null;
        }

        void UpdatePrompt(int n)
        {
            NearRefillSource = FindRefillSource(n) != null;
            string p = "", k = "";
            var thirsty = NearbyWaterable(n);
            var target = Sensor.Interactable;
            CurrentPromptIsGoal = target is GoalFlag && target.CanInteract(gameObject);
            if (target != null && target.CanInteract(gameObject)) { p = target.Prompt; k = target.Action == InteractAction.Pickup ? "Pickup" : IsRefillTarget(target) ? "Water" : "Use"; }
            else if (Sensor.Usable is SandboxTarget { frozen: true } && Inventory.SelectedItem != null && Inventory.SelectedItem.id == "heat_seed")
            { p = "Thaw the frozen soil"; k = "Use"; }
            else if (Sensor.Usable != null && Inventory.SelectedItem != null && Sensor.Usable.CanUseItem(Inventory.SelectedItem, gameObject))
            { p = "Plant " + Inventory.SelectedItem.displayName; k = "Use"; }
            else if (Sensor.Usable is SandboxTarget { frozen: true }) { p = "Frozen soil, needs heat"; k = "Frozen"; }
            else if (thirsty != null) { p = "Water sprout"; k = "Water"; }
            else if (FindFrozenSource(n) != null)   // frozen pond: say so, and what melts it
            {
                var held = Inventory.SelectedItem;
                if (held != null && held.id == "heat_seed") { p = "Melt the ice"; k = "Use"; }
                else { p = "Frozen water, can be melted"; k = "Frozen"; }
            }
            CurrentPromptKey = k;
            CurrentPrompt = p;
        }
    }
}
