using UnityEngine;
using PlantGame.Plants;

namespace PlantGame.Core
{
    /// <summary>
    /// T04: finds what the player is looking at. Third person (T31): the camera is ~4.5 m behind the player, so the
    /// camera-forward SphereCast starts near the point on the camera ray closest to the player's chest, and every hit is
    /// filtered by distance to the chest (range 3 m), not to the camera. If the ray finds nothing, a small aim-assist cone
    /// around the camera ray picks the best target within range (looking roughly at a seed at your feet is enough).
    /// A target is any collider on `mask` whose parents implement IInteractable, IUsableTarget or IWaterable; a Ground /
    /// Default collider between chest and target blocks it. Highlights the focused object (FocusHighlight, glow cyan).
    /// Runs after PlayerController (camera already placed this frame) and before PlayerInteraction.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class InteractionSensor : MonoBehaviour
    {
        [Tooltip("The view the player aims with: PlayerController.EyeTransform (the camera).")]
        public Transform eye;
        public float range = 3f;
        public float castRadius = 0.3f;
        public float chestHeight = 1.1f;
        [Tooltip("Degrees around the camera ray for the fallback when the cast hits nothing.")]
        public float assistAngle = 14f;
        public bool highlight = true;
        [Tooltip("Touch auto-focus: the player's visual facing (PlayerInteraction.model). Falls back to this transform.")]
        public Transform facing;
        [Tooltip("Touch auto-focus: half-angle of the cone in front of the player.")]
        public float touchCone = 70f;
        [Tooltip("Pickups/targets this close to the player are focusable even when not aimed at (items at your feet).")]
        public float proximityRadius = 1.6f;

        public struct Target
        {
            public Component component;      // the object that carries the interfaces
            public IInteractable interactable;
            public IUsableTarget usable;
            public IWaterable waterable;
            public bool IsValid => component != null;
        }

        public Target Focus { get; private set; }
        public IInteractable Interactable => Focus.interactable;
        public IUsableTarget Usable => Focus.usable;
        public Vector3 Chest => transform.position + Vector3.up * chestHeight;

        static int Mask => Layers.InteractMask;
        static int BlockMask => Layers.SolidMask;

        readonly RaycastHit[] _hits = new RaycastHit[16];
        readonly Collider[] _overlaps = new Collider[32];
        readonly System.Collections.Generic.HashSet<Component> _seen = new System.Collections.Generic.HashSet<Component>();

        void Update() { Sense(); }

        void OnDisable() { SetFocus(default); }

        /// <summary>Re-evaluates the focus now. Public so tests can call it after teleporting the player.</summary>
        public void Sense()
        {
            if (PlantGame.Player.TouchInput.Active) { SenseByFacing(); return; }
            var t = eye != null ? eye : transform;
            Vector3 origin = t.position, dir = t.forward, chest = Chest;

            // 1) SphereCast along the camera ray, starting 1 m before the point nearest the chest.
            float t0 = Mathf.Max(0f, Vector3.Dot(chest - origin, dir) - 1f);
            Vector3 start = origin + dir * t0;
            // Every candidate in reach competes in one score: distance to the chest plus how far it is off the aim
            // (30 degrees count as 1 m), so the thing you face wins and the closest breaks ties. The current focus gets a
            // small bonus so two seeds side by side do not flicker. Only one target is ever focused (and highlighted).
            var seen = _seen; seen.Clear();
            Target best = default; float bestScore = float.MaxValue;
            int n = Physics.SphereCastNonAlloc(start, castRadius, dir, _hits, range + 1.5f, Mask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++) Consider(_hits[i].collider, true, chest, origin, dir, seen, ref best, ref bestScore);

            int m = Physics.OverlapSphereNonAlloc(chest, range, _overlaps, Mask, QueryTriggerInteraction.Collide);
            Vector3 feet = transform.position + Vector3.up * 0.5f;
            for (int i = 0; i < m; i++)
            {
                var c = _overlaps[i];
                bool near = (c.bounds.ClosestPoint(feet) - feet).sqrMagnitude <= proximityRadius * proximityRadius;   // items at your feet
                bool aimed = Vector3.Angle(dir, c.bounds.center - origin) < assistAngle;
                Consider(c, near || aimed, chest, origin, dir, seen, ref best, ref bestScore);
            }

            SetFocus(best);
        }

        /// <summary>
        /// T58: touch has no crosshair, so the player's facing picks the target: everything in range inside a cone in front of
        /// the player (or right at the feet) is scored by distance plus angle; the current focus gets a bonus so it does not flicker.
        /// </summary>
        void SenseByFacing()
        {
            Vector3 chest = Chest;
            var f = facing != null ? facing : transform;
            Vector3 fwd = f.forward; fwd.y = 0f; fwd = fwd.sqrMagnitude < 1e-4f ? Vector3.forward : fwd.normalized;
            int m = Physics.OverlapSphereNonAlloc(chest, range, _overlaps, Mask, QueryTriggerInteraction.Collide);
            Target best = default; float bestScore = float.MaxValue;
            for (int i = 0; i < m; i++)
            {
                var c = _overlaps[i];
                if (!TryResolve(c, out var tg) || Blocked(chest, c, tg)) continue;
                Vector3 p = c.bounds.ClosestPoint(chest);
                float d = Vector3.Distance(p, chest); if (d > range) continue;
                Vector3 to = p - chest; to.y = 0f;
                float a = to.sqrMagnitude < 0.01f ? 0f : Vector3.Angle(fwd, to);
                if (d > proximityRadius && a > touchCone) continue;
                float score = d + a / touchCone;
                if (tg.component == Focus.component) score -= 0.5f;
                if (score < bestScore) { bestScore = score; best = tg; }
            }
            SetFocus(best);
        }

        void Consider(Collider c, bool eligible, Vector3 chest, Vector3 origin, Vector3 dir, System.Collections.Generic.HashSet<Component> seen, ref Target best, ref float bestScore)
        {
            if (!eligible || !InRange(c, chest) || !TryResolve(c, out var tg) || !seen.Add(tg.component) || Blocked(chest, c, tg)) return;
            float d = (c.bounds.ClosestPoint(chest) - chest).magnitude;
            float score = d + Vector3.Angle(dir, c.bounds.center - origin) / 30f;
            if (tg.component == Focus.component) score -= 0.3f;
            if (score < bestScore) { bestScore = score; best = tg; }
        }

        /// <summary>Touch tap on a marker: the collider is close enough to act on (the same range as the focus) and nothing blocks it.</summary>
        public bool CanReach(Collider c, Target t)
        {
            Vector3 chest = Chest;
            return (c.bounds.ClosestPoint(chest) - chest).sqrMagnitude <= range * range;   // no line-of-sight test: a sprout sits in a soil patch whose own collider would block it
        }

        /// <summary>Makes the tapped target the focus until the next Sense (touch tap on a marker).</summary>
        public void Override(Target t) { SetFocus(t); }

        bool InRange(Collider c, Vector3 chest) => (c.bounds.ClosestPoint(chest) - chest).sqrMagnitude <= range * range;

        bool Blocked(Vector3 chest, Collider c, Target tg)
        {
            Vector3 p = c.bounds.ClosestPoint(chest);
            if (!Physics.Linecast(chest, p, out var hit, BlockMask, QueryTriggerInteraction.Ignore)) return false;
            return !hit.collider.transform.IsChildOf(tg.component.transform);   // its own collider does not block it
        }

        public static bool TryResolve(Collider c, out Target t)
        {
            t = default;
            t.interactable = c.GetComponentInParent<IInteractable>();
            t.usable = c.GetComponentInParent<IUsableTarget>();
            t.waterable = c.GetComponentInParent<IWaterable>();
            // The component nearest to the collider wins as the "object" (for highlight and identity).
            t.component = (Component)(t.interactable as Component) ?? (t.usable as Component) ?? (t.waterable as Component);
            return t.component != null;
        }

        void SetFocus(Target t)
        {
            var old = Focus.component;
            Focus = t;
            if (old == t.component || !highlight) return;
            if (old != null) FocusHighlight.Set(old.gameObject, false);
            if (t.component != null) FocusHighlight.Set(t.component.gameObject, true);
        }
    }
}
