using System.Collections.Generic;
using UnityEngine;
using PlantGame.Plants;
using PlantGame.UI;
using PlantGame.World;

namespace PlantGame.Core
{
    /// <summary>
    /// World markers over things the player can act on: a diamond outline over pickups, a circle over things the selected
    /// item can be used on (and Use-interactables such as water). Yellow = nearby but not usable yet, blue = the player can
    /// act on it right now (it is the focused target within range). Billboarded sprites, pooled.
    /// </summary>
    [DefaultExecutionOrder(30)]
    public class InteractionMarkers : MonoBehaviour
    {
        public float showRadius = 9f;
        public float size = 0.85f;
        [Tooltip("A pickup this much beyond reach shows its seed colour (thin) when the player faces it.")] public float earlyColourExtra = 2f;
        public float earlyColourAngle = 40f;
        public Color nearColor = new Color(1f, 0.85f, 0.2f, 0.95f);
        [System.NonSerialized] public Color readyColor = new Color(0.8f, 0.3f, 1f, 1f);   // purple: the focused, actionable pickup / usable

        static readonly Color SoilFar = new Color(1f, 0.55f, 0.1f, 0.95f);   // orange: a planting spot seen from afar
        static readonly Color WaterNear = new Color(0.45f, 0.72f, 1f, 0.9f), WaterReady = new Color(0.15f, 0.5f, 1f, 1f);

        [Tooltip("After a pickup the next target stays yellow this long, so two things never look 'ready' in a row.")]
        public float readyDelayAfterPickup = 0.3f;

        /// <summary>True while a refill marker (lake, waterfall, ready collector) is drawn as the thick circle: the HUD hint turns purple with it.</summary>
        public static bool RefillRingThick { get; private set; }

        /// <summary>A marker drawn this frame: what a tap on it would act on (touch).</summary>
        public struct Tappable { public Vector3 position; public Collider collider; public InteractionSensor.Target target; public PlantGame.Player.ClimbableVolume climb; public Vector3 bridgeFrom, bridgeTo; public bool bridge; public bool cap; public Vector3 capPoint; }
        public static readonly List<Tappable> Tappables = new List<Tappable>();

        readonly List<CapHops.Option> _hops = new List<CapHops.Option>();
        PlayerInteraction _pi;
        float _blockReadyUntil;
        Camera _cam;
        readonly Collider[] _hits = new Collider[64];
        readonly List<SpriteRenderer> _pool = new List<SpriteRenderer>();
        readonly HashSet<Component> _seen = new HashSet<Component>();
        static int Mask => Layers.InteractMask;

        PlantGame.Player.PlayerController _pc;

        void Awake() { _pi = GetComponent<PlayerInteraction>(); _pc = GetComponent<PlantGame.Player.PlayerController>(); }
        void OnEnable() { GameEvents.ItemPickedUp += OnPicked; }
        void OnPicked(Inventory.ItemDefinition _) { _blockReadyUntil = Time.unscaledTime + readyDelayAfterPickup; }

        int _used, _shown;      // markers handed out this frame / left active by the last one
        float _pulse;           // the shared size pulse of this frame
        Vector3 _camPos;

        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            _used = 0;
            bool thickRefill = false;
            _seen.Clear();
            Tappables.Clear();
            if (_pi != null && _cam != null)
            {
                _pulse = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 4f);
                _camPos = _cam.transform.position;
                thickRefill = MarkTargets();
                if (_pc != null && !_pc.IsClimbing && !PlantGame.Player.TouchControls.AutoWalking)   // hidden while climbing and during tap walks
                {
                    if (PlantGame.Player.TouchInput.Active) { MarkBeanstalks(); MarkBridges(); }
                    if (!_pc.IsHopping) MarkCaps();
                }
            }
            RefillRingThick = thickRefill;
            for (int i = _used; i < _shown; i++) _pool[i].gameObject.SetActive(false);   // only the markers that are no longer needed
            _shown = _used;
        }

        /// <summary>Pickups, soil, sprouts, water and the goal around the player. True when a refill marker is drawn as the thick circle.</summary>
        bool MarkTargets()
        {
            bool thickRefill = false;
            var sensor = _pi.Sensor;
            Vector3 chest = sensor.Chest; float reach2 = sensor.range * sensor.range, now = Time.unscaledTime;
            int n = Physics.OverlapSphereNonAlloc(transform.position, showRadius, _hits, Mask, QueryTriggerInteraction.Collide);
            var focus = sensor.Focus.component;
            var item = _pi.Inventory.SelectedItem;
            for (int i = 0; i < n; i++)
            {
                var c = _hits[i];
                if (!InteractionSensor.TryResolve(c, out var t) || !_seen.Add(t.component)) continue;

                bool pickup = false, ready, seedColoured = false, soilRing = false, waterRing = false, wetGoal = false;
                Inventory.ItemDefinition tint = null, wanted;
                if (t.interactable != null && t.interactable.Action == InteractAction.Pickup)
                {
                    pickup = true;
                    ready = t.interactable.CanInteract(gameObject);
                    tint = (t.interactable as Inventory.WorldItem)?.item;
                }
                else if (t.interactable != null && t.interactable.Action == InteractAction.Use)
                {
                    ready = t.interactable.CanInteract(gameObject);
                    if (t.interactable is GoalFlag { TooWet: true }) { ready = false; wetGoal = true; }   // a campfire in the rain: thin, faded ring like unplantable soil
                    if (!ready && !wetGoal && (t.interactable is WaterCollector || t.interactable is GoalFlag)) continue;   // an empty collector (or a full can) offers nothing on F, a used goal nothing on E: no marker
                }
                else if (t.usable is SoilPatch soil && !soil.planted && !soil.frozen && (wanted = Kit.Item(soil.acceptedItemId) ?? item) != null)
                {
                    // soil always shows the seed it wants: faded and thin until that seed is selected, then bright and thick
                    soilRing = true; tint = wanted;
                    ready = item != null && soil.CanUseItem(item, gameObject);
                }
                else if (t.usable != null && item != null && t.usable.CanUseItem(item, gameObject))
                {
                    ready = true; seedColoured = true;
                }
                else if (t.waterable != null && t.waterable.NeedsWater)
                    { ready = false; waterRing = true; }   // a sprout waiting for F: thin blue ring
                else continue;

                var own = c.bounds; var b = own;
                if (t.interactable is WaterSource wsrc)   // a pond / stream is one trigger: the marker sits on the patch next to the player, not the middle of the whole thing
                { var pc = wsrc.MarkerCentre(transform.position); b = new Bounds(new Vector3(pc.x, b.center.y, pc.z), new Vector3(1f, b.size.y, 1f)); }
                bool focused = soilRing ? ready : ready && t.component == focus && now >= _blockReadyUntil;
                // floating: a slow bob (out of step per target) so the markers catch the eye; the sprout ring floats higher: sprouts are tiny
                Vector3 pos = b.center + Vector3.up * (Mathf.Min(b.extents.y, 0.6f) + 0.75f + Bob(b.center.x * 1.7f + b.center.z * 1.3f) + (waterRing ? 0.9f : 0f));
                float scale = focused ? 1.25f : 1f;
                bool water = t.interactable is WaterSource || t.interactable is WaterCollector;   // water points are blue
                var near = water ? WaterNear : nearColor; var rdy = water ? WaterReady : readyColor;
                bool close = (own.ClosestPoint(chest) - chest).sqrMagnitude <= reach2;   // the same reach as the sensor / F: marker and action agree
                bool thick = pickup && focused && tint != null;   // only the one pickup that F/Q would act on is thick
                if (soilRing)
                {
                    // orange from afar; the seed colour once in reach or when the player faces it (like pickups)
                    var ic = PlayerHud.ItemColor(tint);
                    rdy = Color.Lerp(ic, Color.white, 0.25f); rdy.a = 1f;
                    near = close || FacingEarly(own) ? new Color(ic.r, ic.g, ic.b, 1f) : SoilFar;
                }
                else if (pickup)
                {   // yellow from afar; seed colour and thin once in reach; seed colour and thick when it is the one Q takes (never purple)
                    near = nearColor; rdy = nearColor;
                    if (tint != null && (close || focused || FacingEarly(own)))
                    {
                        var ic = PlayerHud.ItemColor(tint); near = new Color(ic.r, ic.g, ic.b, 1f);
                        rdy = Color.Lerp(near, Color.white, 0.3f);
                    }
                }
                else if (seedColoured)
                {
                    var ic = PlayerHud.ItemColor(item); ic.a = 1f;
                    near = new Color(ic.r, ic.g, ic.b, 0.85f); rdy = Color.Lerp(ic, Color.white, 0.3f);
                }
                if (waterRing) { near = WaterNear; rdy = WaterReady; focused = close; }
                // use circles stay slim: hairline when faded / far, medium when it is the right thing
                bool slim = waterRing || wetGoal || (soilRing ? !ready : water && !close && !focused);
                var sprite = pickup ? (thick ? UiSprites.DiamondThick : UiSprites.DiamondThin) : slim ? UiSprites.RingThin : soilRing ? UiSprites.RingSlim : UiSprites.Ring;
                var colour = focused ? rdy : near;
                if (water && !slim && ready) thickRefill = true;
                if (t.interactable is GoalFlag { kind: GoalKind.Gate } gate)
                {   // the portal fills the middle of a gate: one circle in front of it and one behind it (centred) instead
                    Vector3 side = gate.transform.forward * 0.6f;
                    for (int k = -1; k <= 1; k += 2)
                    {
                        Place(pos + side * k, scale, sprite, colour);
                        Tappables.Add(new Tappable { position = pos + side * k, collider = c, target = t });
                    }
                }
                else
                {
                    Place(pos, scale, sprite, colour);
                    Tappables.Add(new Tappable { position = pos, collider = c, target = t });
                }
            }
            return thickRefill;
        }

        /// <summary>Touch, grown beanstalks: a circle at the foot (and at the top, to climb back down); tapping it climbs the whole stalk.</summary>
        void MarkBeanstalks()
        {
            Vector3 me = transform.position;
            var volumes = PlantGame.Player.ClimbableVolume.All;
            for (int i = 0; i < volumes.Count; i++)
            {
                var v = volumes[i];
                Vector3 foot = v.ClimbLinePoint + v.SurfaceNormal * 0.5f;
                bool atTop = me.y >= v.TopY - 2.5f;
                if (atTop)   // the top circle sits on the far side of the stalk (180 degrees round it), where the end island is
                {
                    Vector3 c = v.Bounds.center;
                    foot = new Vector3(2f * c.x - foot.x, foot.y, 2f * c.z - foot.z);
                }
                var pos = new Vector3(foot.x, (atTop ? v.TopY : v.Bounds.min.y) + 2.1f, foot.z);
                float dist = FlatDistance(pos, me);
                if (dist > showRadius) continue;
                pos.y += Bob(pos.x * 1.7f);
                PlaceRing(pos, dist <= _pi.Sensor.range);
                Tappables.Add(new Tappable { position = pos, climb = v });
            }
        }

        /// <summary>Touch, grown vine bridges: a circle at each end; tapping one walks to that end and then across to the other.</summary>
        void MarkBridges()
        {
            Vector3 me = transform.position;
            var plants = PlantInstance.All;
            for (int i = 0; i < plants.Count; i++)
            {
                var pl = plants[i];
                if (pl.kind != PlantKind.Bridge || !pl.IsGrown || !pl.TryGetEnds(out var a, out var t)) continue;
                if (OnBridge(a, t, me)) continue;   // already on the vine: no circles
                for (int e = 0; e < 2; e++)
                {
                    Vector3 from = e == 0 ? a : t, to = e == 0 ? t : a;
                    float dist = FlatDistance(from, me);
                    if (dist > showRadius) continue;
                    Vector3 along = to - from; along.y = 0f; along.Normalize();
                    var pos = from + along * 0.8f + Vector3.up * (1.6f + Bob(from.x * 1.7f));
                    PlaceRing(pos, dist <= _pi.Sensor.range);
                    Tappables.Add(new Tappable { position = pos, bridge = true, bridgeFrom = from - along * 0.3f, bridgeTo = to + along * 1.0f });
                }
            }
        }

        /// <summary>Mushroom caps: a circle over each cap the player can hop to from here.</summary>
        void MarkCaps()
        {
            Vector3 me = transform.position;
            CapHops.Find(me, _hops);
            foreach (var o in _hops)
            {
                float bob = Bob(o.point.x * 1.7f);
                var pos = o.point + Vector3.up * (1.5f + bob);
                // standing on a cap: the neighbour's ring would sit right next to (or under) the player, so it floats out towards that cap at a readable distance
                Vector3 flat = o.point - me; flat.y = 0f;
                if (flat.magnitude < 3f && Mathf.Abs(me.y - o.point.y) < 6f)
                {
                    Vector3 dir = flat.sqrMagnitude > 0.01f ? flat.normalized : transform.forward;
                    pos = me + dir * 3f; pos.y = Mathf.Max(o.point.y, me.y) + 2f + bob;
                }
                Place(pos, 1.25f, UiSprites.Ring, readyColor);
                Tappables.Add(new Tappable { position = pos, cap = true, capPoint = o.point });
            }
        }

        /// <summary>The next pooled marker, put at pos facing the camera; `scale` multiplies the marker size and the shared pulse.</summary>
        void Place(Vector3 pos, float scale, Sprite sprite, Color colour)
        {
            var r = Get(_used++);
            r.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(pos - _camPos));
            r.transform.localScale = Vector3.one * (size * scale * _pulse);
            r.sprite = sprite; r.color = colour;
        }

        /// <summary>A touch circle (beanstalk, bridge end): big and bright within reach, slim and yellow beyond.</summary>
        void PlaceRing(Vector3 pos, bool ready) => Place(pos, ready ? 1.25f : 1f, ready ? UiSprites.Ring : UiSprites.RingSlim, ready ? readyColor : nearColor);

        static float Bob(float phase) => 0.14f * Mathf.Sin(Time.unscaledTime * 2.6f + phase);
        static float FlatDistance(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        /// <summary>A pickup a little beyond reach already shows its seed colour (thin) while the player faces it.</summary>
        bool FacingEarly(Bounds bounds)
        {
            Vector3 to = bounds.center - transform.position; to.y = 0f;
            float reach = _pi.Sensor.range + earlyColourExtra;
            if (to.sqrMagnitude > reach * reach || to.sqrMagnitude < 1e-4f) return false;
            var f = PlantGame.Player.TouchInput.Active && _pi.Sensor.facing != null ? _pi.Sensor.facing : _cam.transform;
            Vector3 fwd = f.forward; fwd.y = 0f;
            return fwd.sqrMagnitude > 1e-4f && Vector3.Angle(fwd, to) <= earlyColourAngle;
        }

        /// <summary>True when the point stands on the bridge between its two ends (along it, near its width, near its height).</summary>
        static bool OnBridge(Vector3 a, Vector3 t, Vector3 p)
        {
            Vector2 a2 = new Vector2(a.x, a.z), d = new Vector2(t.x, t.z) - a2, q = new Vector2(p.x, p.z) - a2;
            float len2 = d.sqrMagnitude;
            if (len2 < 0.01f) return false;
            float k = Vector2.Dot(q, d) / len2;
            if (k < 0f || k > 1f) return false;
            if ((q - d * k).magnitude > 1.6f) return false;
            return Mathf.Abs(p.y - Mathf.Lerp(a.y, t.y, k)) < 2f;
        }

        SpriteRenderer Get(int i)
        {
            while (_pool.Count <= i)
            {
                var go = new GameObject("InteractionMarker") { hideFlags = HideFlags.DontSave };
                var r = go.AddComponent<SpriteRenderer>();
                r.sortingOrder = 100;
                _pool.Add(r);
            }
            if (i >= _shown) _pool[i].gameObject.SetActive(true);   // the first _shown are still active from the last frame
            return _pool[i];
        }

        void OnDisable() { GameEvents.ItemPickedUp -= OnPicked; foreach (var r in _pool) if (r != null) r.gameObject.SetActive(false); _shown = 0; }
        void OnDestroy() { foreach (var r in _pool) if (r != null) Destroy(r.gameObject); }
    }
}
