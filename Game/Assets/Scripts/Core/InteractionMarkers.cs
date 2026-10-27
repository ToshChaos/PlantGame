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
        [Tooltip("Pickups closer than this wear their seed colour and a thick outline; further ones are thin and orange.")]
        public float closeRadius = 4.5f;
        public Color nearColor = new Color(1f, 0.85f, 0.2f, 0.95f);
        [System.NonSerialized] public Color readyColor = new Color(0.8f, 0.3f, 1f, 1f);   // purple: the focused, actionable pickup / usable

        static readonly Color WaterNear = new Color(0.45f, 0.72f, 1f, 0.9f), WaterReady = new Color(0.15f, 0.5f, 1f, 1f);

        [Tooltip("After a pickup the next target stays yellow this long, so two things never look 'ready' in a row.")]
        public float readyDelayAfterPickup = 0.3f;

        /// <summary>True while a refill marker (lake, waterfall, ready collector) is drawn as the thick circle: the HUD hint turns purple with it.</summary>
        public static bool RefillRingThick { get; private set; }

        /// <summary>A marker drawn this frame: what a tap on it would act on (touch).</summary>
        public struct Tappable { public Vector3 position; public Collider collider; public InteractionSensor.Target target; public PlantGame.Player.ClimbableVolume climb; }
        public static readonly List<Tappable> Tappables = new List<Tappable>();

        PlayerInteraction _pi;
        float _blockReadyUntil;
        Camera _cam;
        readonly Collider[] _hits = new Collider[64];
        readonly List<SpriteRenderer> _pool = new List<SpriteRenderer>();
        readonly HashSet<Component> _seen = new HashSet<Component>();
        static int Mask => LayerMask.GetMask(Layers.InteractableName, Layers.WaterName, Layers.PlantName);

        void Awake() { _pi = GetComponent<PlayerInteraction>(); }
        void OnEnable() { GameEvents.ItemPickedUp += OnPicked; }
        void OnPicked(Inventory.ItemDefinition _) { _blockReadyUntil = Time.unscaledTime + readyDelayAfterPickup; }

        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            int used = 0;
            bool thickRefill = false;
            _seen.Clear();
            Tappables.Clear();
            if (_pi != null && _cam != null)
            {
                int n = Physics.OverlapSphereNonAlloc(transform.position, showRadius, _hits, Mask, QueryTriggerInteraction.Collide);
                var focus = _pi.Sensor.Focus.component;
                var item = _pi.Inventory.SelectedItem;
                for (int i = 0; i < n; i++)
                {
                    var c = _hits[i];
                    if (!InteractionSensor.TryResolve(c, out var t) || !_seen.Add(t.component)) continue;

                    bool pickup = false, ready, seedColoured = false, soilRing = false, waterRing = false, wetGoal = false;
                    Inventory.ItemDefinition tint = null;
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
                    else if (t.usable is SandboxTarget soil && !soil.planted && !soil.frozen && (Kit.Item(soil.acceptedItemId) ?? item) != null)
                    {
                        // soil always shows the seed it wants: faded and thin until that seed is selected, then bright and thick
                        soilRing = true; tint = Kit.Item(soil.acceptedItemId) ?? item;
                        ready = item != null && soil.CanUseItem(item, gameObject);
                    }
                    else if (t.usable != null && item != null && t.usable.CanUseItem(item, gameObject))
                    {
                        ready = true; seedColoured = true;
                    }
                    else if (t.waterable != null && t.waterable.NeedsWater)
                        { ready = false; waterRing = true; }   // a sprout waiting for F: thin blue ring
                    else continue;

                    var r = Get(used++);
                    var b = c.bounds;
                    if (t.interactable is WaterSource wsrc)   // a pond / stream is one trigger: the marker sits on the patch next to the player, not the middle of the whole thing
                    { var pc = wsrc.MarkerCentre(transform.position); b = new Bounds(new Vector3(pc.x, b.center.y, pc.z), new Vector3(1f, b.size.y, 1f)); }
                    bool focused = soilRing ? ready : ready && t.component == focus && Time.unscaledTime >= _blockReadyUntil;
                    // floating: a slow bob (out of step per target) so the markers catch the eye
                    float bob = 0.14f * Mathf.Sin(Time.unscaledTime * 2.6f + b.center.x * 1.7f + b.center.z * 1.3f);
                    Vector3 pos = b.center + Vector3.up * (Mathf.Min(b.extents.y, 0.6f) + 0.75f + bob + (waterRing ? 0.9f : 0f));   // the sprout ring floats higher: sprouts are tiny
                    r.transform.position = pos;
                    Tappables.Add(new Tappable { position = pos, collider = c, target = t });
                    r.transform.rotation = Quaternion.LookRotation(pos - _cam.transform.position);
                    r.transform.localScale = Vector3.one * (size * (focused ? 1.25f : 1f) * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 4f)));
                    bool water = t.interactable is WaterSource || t.interactable is WaterCollector;   // water points are blue
                    var near = water ? WaterNear : nearColor; var rdy = water ? WaterReady : readyColor;
                    bool close = Vector3.Distance(transform.position, b.center) <= closeRadius;
                    bool thick = pickup && (close || focused);
                    if (soilRing)
                    {
                        var ic = PlayerHud.ItemColor(tint);
                        near = new Color(ic.r, ic.g, ic.b, 0.4f); rdy = Color.Lerp(ic, Color.white, 0.25f); rdy.a = 1f;
                    }
                    else if (pickup && tint != null && close)   // a close pickup wears its own colour; the focused one turns purple
                    {
                        var ic = PlayerHud.ItemColor(tint); near = new Color(ic.r, ic.g, ic.b, 1f);
                        rdy = Color.Lerp(near, Color.white, 0.3f);   // aimed at: the same seed colour, brighter (and bigger)
                    }
                    else if (seedColoured)
                    {
                        var ic = PlayerHud.ItemColor(item); ic.a = 1f;
                        near = new Color(ic.r, ic.g, ic.b, 0.85f); rdy = Color.Lerp(ic, Color.white, 0.3f);
                    }
                    if (waterRing) { near = WaterNear; rdy = WaterReady; focused = close; }
                    // use circles stay slim: hairline when faded / far, medium when it is the right thing
                    bool slim = waterRing || wetGoal || (soilRing ? !ready : water && !close && !focused);
                    r.sprite = pickup ? (thick ? UiSprites.DiamondThick : UiSprites.DiamondThin) : slim ? UiSprites.RingThin : soilRing ? UiSprites.RingSlim : UiSprites.Ring;
                    r.color = focused ? rdy : near;
                    if (water && !slim && ready) thickRefill = true;
                }
            }
            if (_pi != null && _cam != null)
            {
                // grown beanstalks: a circle at the foot (and at the top, to climb back down); tapping it climbs the whole stalk (touch)
                var pc = GetComponent<PlantGame.Player.PlayerController>();
                if (pc != null && !pc.IsClimbing)
                    foreach (var v in PlantGame.Player.ClimbableVolume.All)
                    {
                        Vector3 foot = v.ClimbLinePoint + v.SurfaceNormal * 0.5f;
                        bool atTop = transform.position.y >= v.TopY - 2.5f;
                        float baseY = atTop ? v.TopY + 1.2f : v.GetComponent<BoxCollider>().bounds.min.y + 1.6f;
                        var pos = new Vector3(foot.x, baseY, foot.z);
                        float dist = Vector2.Distance(new Vector2(pos.x, pos.z), new Vector2(transform.position.x, transform.position.z));
                        if (dist > showRadius) continue;
                        var r = Get(used++);
                        bool ready = dist <= 3.5f;
                        pos.y += 0.14f * Mathf.Sin(Time.unscaledTime * 2.6f + pos.x * 1.7f);
                        r.transform.position = pos;
                        r.transform.rotation = Quaternion.LookRotation(pos - _cam.transform.position);
                        r.transform.localScale = Vector3.one * (size * (ready ? 1.25f : 1f) * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 4f)));
                        r.sprite = ready ? UiSprites.Ring : UiSprites.RingSlim;
                        r.color = ready ? readyColor : nearColor;
                        Tappables.Add(new Tappable { position = pos, climb = v });
                    }
            }
            RefillRingThick = thickRefill;
            for (int i = 0; i < _pool.Count; i++) _pool[i].gameObject.SetActive(i < used);
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
            _pool[i].gameObject.SetActive(true);
            return _pool[i];
        }

        void OnDisable() { GameEvents.ItemPickedUp -= OnPicked; foreach (var r in _pool) if (r != null) r.gameObject.SetActive(false); }
        void OnDestroy() { foreach (var r in _pool) if (r != null) Destroy(r.gameObject); }
    }
}
