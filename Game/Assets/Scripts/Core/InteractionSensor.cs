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
        public IWaterable Waterable => Focus.waterable;
        public GameObject FocusedObject => Focus.component != null ? Focus.component.gameObject : null;
        public Vector3 Chest => transform.position + Vector3.up * chestHeight;

        static int Mask => LayerMask.GetMask(Layers.InteractableName, Layers.WaterName, Layers.PlantName);
        static int BlockMask => LayerMask.GetMask("Default", Layers.GroundName);

        readonly RaycastHit[] _hits = new RaycastHit[16];
        readonly Collider[] _overlaps = new Collider[32];

        void Update() { Sense(); }

        void OnDisable() { SetFocus(default); }

        /// <summary>Re-evaluates the focus now. Public so tests can call it after teleporting the player.</summary>
        public void Sense()
        {
            var t = eye != null ? eye : transform;
            Vector3 origin = t.position, dir = t.forward, chest = Chest;

            // 1) SphereCast along the camera ray, starting 1 m before the point nearest the chest.
            float t0 = Mathf.Max(0f, Vector3.Dot(chest - origin, dir) - 1f);
            Vector3 start = origin + dir * t0;
            int n = Physics.SphereCastNonAlloc(start, castRadius, dir, _hits, range + 1.5f, Mask, QueryTriggerInteraction.Collide);
            Target best = default; float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var c = _hits[i].collider;
                if (!InRange(c, chest) || !TryResolve(c, out var tg) || Blocked(chest, c, tg)) continue;
                // Third person: the camera ray runs through the player, so "first hit along the ray" favours far things.
                // Among everything the ray touches, take the one nearest the player's chest.
                float d = (c.bounds.ClosestPoint(chest) - chest).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = tg; }
            }

            // 2) Aim assist: smallest angle to the camera ray among targets within range.
            if (!best.IsValid)
            {
                int m = Physics.OverlapSphereNonAlloc(chest, range, _overlaps, Mask, QueryTriggerInteraction.Collide);
                float bestAngle = assistAngle;
                for (int i = 0; i < m; i++)
                {
                    var c = _overlaps[i];
                    if (!InRange(c, chest) || !TryResolve(c, out var tg) || Blocked(chest, c, tg)) continue;
                    float a = Vector3.Angle(dir, c.bounds.center - origin);
                    if (a < bestAngle) { bestAngle = a; best = tg; }
                }
            }

            // 3) Proximity: targets right next to the player compete with the aimed one; the nearest to the chest wins
            //    (in third person the camera ray often runs past the thing at your feet and hits something far behind it).
            {
                int m = Physics.OverlapSphereNonAlloc(transform.position + Vector3.up * 0.5f, proximityRadius, _overlaps, Mask, QueryTriggerInteraction.Collide);
                for (int i = 0; i < m; i++)
                {
                    var c = _overlaps[i];
                    if (!TryResolve(c, out var tg) || Blocked(chest, c, tg)) continue;
                    float d = (c.bounds.ClosestPoint(chest) - chest).sqrMagnitude;
                    if (d < bestDist) { bestDist = d; best = tg; }
                }
            }

            SetFocus(best);
        }

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
