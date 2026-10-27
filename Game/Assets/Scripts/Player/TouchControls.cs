using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using PlantGame.Core;
using PlantGame.Plants;
using PlantGame.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace PlantGame.Player
{
    /// <summary>
    /// T57 / T58: the on-screen layer for phones (docs/mobile-controls.md). Dragging looks around; a short tap does one of three things:
    /// a tap on a world marker (the circle / diamond over a pickup, soil, water, the goal) activates it when the player is in reach
    /// (and walks towards it otherwise), a tap on a beanstalk climbs it (while climbing, a tap above / below the player sets the
    /// direction), and a tap on the ground turns the player towards the spot and walks there at walking speed. Two round buttons
    /// remain: Jump and Pause; the hotbar slots are tappable (PlayerHud, double tap drops).
    /// Touches are polled (EnhancedTouch) and classified when they begin, so a touch that starts on a button or the hotbar never
    /// looks or taps the world. Results go to TouchInput; PlayerInputReader and PlayerInteraction read them.
    /// In the editor, GameSettings.TouchControls turns the layer on and TouchSimulation lets the mouse act as one finger.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class TouchControls : MonoBehaviour
    {
        const float LookCountsPerInch = 140f, TapSeconds = 0.3f, TurnFirstDegrees = 18f, ArriveDistance = 0.3f, WalkTimeout = 25f;

        enum Role { None, Look, Button }
        enum Btn { Jump, Pause, Count }

        struct Slot { public bool used; public Role role; public int btn; public Vector2 origin; public int id; public float t0, maxDist; }

        PlayerInputReader _input;
        PlayerInteraction _pi;
        PlayerController _pc;
        Canvas _canvas;
        readonly RectTransform[] _btn = new RectTransform[(int)Btn.Count];
        readonly Image[] _btnImg = new Image[(int)Btn.Count];
        readonly Text[] _btnLabel = new Text[(int)Btn.Count];
        readonly bool[] _btnHeld = new bool[(int)Btn.Count];
        readonly Slot[] _slots = new Slot[16];
        Font _font;
        bool _simulating;
        static readonly Color LabelGreen = new Color(0.07f, 0.24f, 0.14f);

        // tap-to-walk
        bool _walking, _turning;
        Vector3 _walkPoint, _lastPos;
        Collider _walkCol; InteractionSensor.Target _walkTarget;   // walking up to a marker: stop once it is in reach
        ClimbableVolume _walkClimb;                                // walking up to a beanstalk: grab it as soon as the player stands in its volume
        float _walkT, _stuckT;
        int _climbDir;
        SpriteRenderer _destMarker;

        void Awake()
        {
            _input = GetComponent<PlayerInputReader>();
            _pi = GetComponent<PlayerInteraction>();
            _pc = GetComponent<PlayerController>();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Build();
        }

        void OnEnable()
        {
            if (!TouchInput.Active) return;
            EnhancedTouchSupport.Enable();
            if (!Application.isMobilePlatform && Touchscreen.current == null) { TouchSimulation.Enable(); _simulating = true; }
        }

        void OnDisable()
        {
            if (_simulating) { TouchSimulation.Disable(); _simulating = false; }
            if (EnhancedTouchSupport.enabled && TouchInput.Active) EnhancedTouchSupport.Disable();
            CancelWalk();
            TouchInput.ClearAll();
        }

        void Update()
        {
            TouchInput.ClearFrame();
            bool show = TouchInput.Active && _input != null && _input.Enabled;
            if (_canvas.enabled != show) _canvas.enabled = show;
            if (!show)
            {
                for (int i = 0; i < _slots.Length; i++) _slots[i].used = false;
                CancelWalk(); _climbDir = 0;
                TouchInput.Move = Vector2.zero;
                return;
            }
            if (!EnhancedTouchSupport.enabled) { EnhancedTouchSupport.Enable(); }

            System.Array.Clear(_btnHeld, 0, _btnHeld.Length);
            float slop = Mathf.Max(24f, Screen.dpi * 0.12f);

            foreach (var t in Touch.activeTouches)
            {
                int fi = t.finger.index; if (fi < 0 || fi >= _slots.Length) continue;
                var s = _slots[fi];
                Vector2 pos = t.screenPosition;
                bool ended = t.phase == UnityEngine.InputSystem.TouchPhase.Ended || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled;
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began || !s.used || s.id != t.touchId)
                {
                    s = new Slot { used = true, id = t.touchId, origin = pos, role = Role.Look, t0 = Time.unscaledTime };
                    int b = HitButton(pos);
                    if (b >= 0) { s.role = Role.Button; s.btn = b; Pressed((Btn)b); }
                    else if (PlayerHud.OverHotbar(pos)) s.role = Role.None;   // the slots own these touches
                }
                s.maxDist = Mathf.Max(s.maxDist, (pos - s.origin).magnitude);
                switch (s.role)
                {
                    case Role.Look:
                        if (!ended)
                        {
                            float k = LookCountsPerInch / Mathf.Max(Screen.dpi, 96f) * GameSettings.TouchLookSensitivity;
                            Vector2 dl = t.delta * k;
                            if (GameSettings.TouchInvertY) dl.y = -dl.y;
                            TouchInput.Look += dl;
                        }
                        else if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended && s.maxDist <= slop && Time.unscaledTime - s.t0 <= TapSeconds) WorldTap(pos);
                        break;
                    case Role.Button:
                        if (!ended) _btnHeld[s.btn] = true;
                        break;
                }
                s.used = !ended;
                _slots[fi] = s;
            }
            for (int i = 0; i < _btnImg.Length; i++) if (_btnImg[i] != null) _btnImg[i].color = _btnHeld[i] ? new Color(0.85f, 0.95f, 0.85f, 1f) : new Color(1f, 1f, 1f, 0.95f);

            UpdateWalk();
        }

        void Pressed(Btn b)
        {
            switch (b)
            {
                case Btn.Jump: TouchInput.JumpPressed = true; break;
                case Btn.Pause: TouchInput.PausePressed = true; break;
            }
        }

        int HitButton(Vector2 p)
        {
            for (int i = 0; i < _btn.Length; i++)
                if (_btn[i].gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(_btn[i], p, null)) return i;
            return -1;
        }

        // ---------------------------------------------------------------- taps on the world

        /// <summary>A short tap at a screen position (also callable from editor tests).</summary>
        public void WorldTap(Vector2 pos)
        {
            var cam = Camera.main;
            if (cam == null || _pc == null) return;

            // on a beanstalk: a tap above or below the player picks the direction (a tap on the player stops)
            if (_pc.IsClimbing)
            {
                float py = cam.WorldToScreenPoint(_pc.transform.position + Vector3.up * 1f).y;
                float dy = pos.y - py;
                _climbDir = Mathf.Abs(dy) < Screen.height * 0.08f ? 0 : dy > 0f ? 1 : -1;
                return;
            }

            // a marker (circle / diamond): act on it when in reach, otherwise walk up to it
            if (_pi != null)
            {
                float radius = Mathf.Max(90f, Screen.dpi * 0.28f), bestScore = 1f;
                bool canFill = _pi.Can != null && !_pi.Can.IsFull;
                InteractionMarkers.Tappable best = default; bool found = false;
                foreach (var m in InteractionMarkers.Tappables)
                {
                    if (m.collider == null && m.climb == null) continue;
                    var sp = cam.WorldToScreenPoint(m.position);
                    if (sp.z <= 0f) continue;
                    // water points are the thing to tap when the can has room: a much wider hit area, and they win against other markers
                    bool water = canFill && (m.target.interactable is WaterSource || m.target.interactable is WaterCollector);
                    float score = ((Vector2)sp - pos).magnitude / (radius * (water ? 2.5f : 1f));
                    if (score < bestScore) { bestScore = score; best = m; found = true; }
                }
                if (found)
                {
                    if (best.climb != null) { ClimbTap(best.climb); return; }
                    if (_pi.Sensor.CanReach(best.collider, best.target)) { CancelWalk(); TouchInput.HasTap = true; TouchInput.Tap = best.target; }
                    else StartWalk(best.collider.bounds.ClosestPoint(_pc.transform.position), best.collider, best.target, null);
                    return;
                }
            }

            var ray = cam.ScreenPointToRay(pos);
            bool ground = Physics.Raycast(ray, out var gh, 300f, Layers.WalkableMask, QueryTriggerInteraction.Ignore);
            if (Physics.Raycast(ray, out var ch, 300f, LayerMask.GetMask(Layers.ClimbableName), QueryTriggerInteraction.Collide) && (!ground || ch.distance < gh.distance + 0.5f))
            {
                var vol = ch.collider.GetComponentInParent<ClimbableVolume>();
                if (vol != null) { ClimbTap(vol); return; }
            }
            if (ground) StartWalk(gh.point, null, default, null);
        }

        /// <summary>A tap on a beanstalk (its circle or the stalk): climb it all the way, walking up to it first when needed.</summary>
        void ClimbTap(ClimbableVolume vol)
        {
            CancelWalk();
            if (_pc.CanGrab) { _pc.Grab(); _climbDir = 1; }
            else if (_pc.CanDescend) { _pc.Descend(); _climbDir = -1; }
            else StartWalk(ClimbSpot(vol), null, default, vol);
        }

        /// <summary>Where the player walks to reach a beanstalk: on its climb line, a little to the player's side.</summary>
        Vector3 ClimbSpot(ClimbableVolume vol) => vol.ClimbLinePoint + vol.SurfaceNormal * 0.45f;   // well inside the volume, in front of the stalk: stopping short of the box edge would never grab

        // ---------------------------------------------------------------- tap-to-walk

        void StartWalk(Vector3 point, Collider col, InteractionSensor.Target target, ClimbableVolume climb)
        {
            _walking = true; _walkPoint = point; _walkCol = col; _walkTarget = target; _walkClimb = climb;
            _walkT = 0f; _stuckT = 0f; _lastPos = _pc.transform.position; _climbDir = 0;
            Vector3 to = point - _pc.transform.position; to.y = 0f;
            _turning = to.sqrMagnitude > 0.01f && FacingError(to.normalized) > TurnFirstDegrees;
            ShowDestination(point, climb == null);
        }

        void CancelWalk()
        {
            _walking = false; _walkCol = null; _walkClimb = null;
            TouchInput.Move = Vector2.zero; TouchInput.WorldMove = Vector3.zero; TouchInput.FaceDir = Vector3.zero;
            if (_destMarker != null) _destMarker.gameObject.SetActive(false);
        }

        float FacingError(Vector3 dir)
        {
            var m = _pc.model != null ? _pc.model : _pc.transform;
            Vector3 f = m.forward; f.y = 0f;
            return f.sqrMagnitude < 1e-4f ? 0f : Vector3.Angle(f, dir);
        }

        void UpdateWalk()
        {
            TouchInput.Move = Vector2.zero; TouchInput.WorldMove = Vector3.zero; TouchInput.FaceDir = Vector3.zero;
            if (_pc == null) return;
            if (_pc.IsClimbing) { CancelWalk(); TouchInput.Move = new Vector2(0f, _climbDir); return; }
            _climbDir = 0;
            if (!_walking) return;

            var pos = _pc.transform.position;
            _walkT += Time.unscaledDeltaTime;
            if ((pos - _lastPos).sqrMagnitude > 25f) { CancelWalk(); return; }   // respawned / teleported

            if (_walkClimb != null)
            {
                if (_pc.CanGrab) { _pc.Grab(); _climbDir = 1; CancelWalk(); return; }
                if (_pc.CanDescend) { _pc.Descend(); _climbDir = -1; CancelWalk(); return; }
            }
            if (_walkCol != null && _pi.Sensor.CanReach(_walkCol, _walkTarget)) { CancelWalk(); return; }

            Vector3 to = _walkPoint - pos; to.y = 0f;
            float dist = to.magnitude;
            if (dist < ArriveDistance || _walkT > WalkTimeout) { CancelWalk(); return; }
            Vector3 dir = to / dist;

            if (_turning)
            {
                if (FacingError(dir) < 12f) _turning = false;
                else { TouchInput.FaceDir = dir; _lastPos = pos; return; }   // turn on the spot first
            }

            // stuck against a wall or an edge: give up
            _stuckT += Time.unscaledDeltaTime;
            if (_stuckT >= 0.6f)
            {
                Vector3 moved = pos - _lastPos; moved.y = 0f;
                if (moved.magnitude < 0.2f) { CancelWalk(); return; }
                _stuckT = 0f; _lastPos = pos;
            }

            TouchInput.WorldMove = dir;
            if (_destMarker != null) _destMarker.transform.localScale = Vector3.one * (0.9f + 0.1f * Mathf.Sin(Time.unscaledTime * 6f));
        }

        void ShowDestination(Vector3 p, bool visible)
        {
            if (_destMarker == null)
            {
                var go = new GameObject("TapDestination") { hideFlags = HideFlags.DontSave };
                _destMarker = go.AddComponent<SpriteRenderer>();
                _destMarker.sprite = UiSprites.Ring; _destMarker.sortingOrder = 90;
                _destMarker.color = new Color(1f, 1f, 1f, 0.8f);
                go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }
            _destMarker.transform.position = p + Vector3.up * 0.06f;
            _destMarker.gameObject.SetActive(visible);
        }

        void OnDestroy() { if (_destMarker != null) Destroy(_destMarker.gameObject); }

        // ---------------------------------------------------------------- build

        void Build()
        {
            var cgo = new GameObject("TouchControls", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            _canvas = cgo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 40;   // under the HUD (50): menus stay on top
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = UiLayout.MatchHeight;
            var root = (RectTransform)cgo.transform;

            MakeButton(Btn.Jump, root, new Vector2(1f, 0f), new Vector2(-170, 190), 170, "Jump");
            MakeButton(Btn.Pause, root, new Vector2(0f, 1f), new Vector2(120, -170), 150, "Pause");
        }

        RectTransform Circle(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 pos, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            var img = go.GetComponent<Image>(); img.sprite = sprite; img.raycastTarget = false;
            return rt;
        }

        void MakeButton(Btn b, Transform parent, Vector2 anchor, Vector2 pos, float size, string label)
        {
            // the player's colours: white disc, dark green text, yellow outline (the outer disc is the outline, the inner one the fill)
            var rt = Circle("Btn_" + b, parent, anchor, new Vector2(size, size), pos, UiSprites.Disc);
            rt.GetComponent<Image>().color = Palette.FlowerYellow;
            var fill = Circle("Fill", rt, new Vector2(0.5f, 0.5f), new Vector2(size - 14f, size - 14f), Vector2.zero, UiSprites.Disc);
            var img = fill.GetComponent<Image>(); img.color = new Color(1f, 1f, 1f, 0.95f);
            var tgo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var trt = (RectTransform)tgo.transform; trt.SetParent(rt, false);
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = new Vector2(10, 10); trt.offsetMax = new Vector2(-10, -10);
            var t = tgo.GetComponent<Text>();
            t.font = _font; t.text = label; t.alignment = TextAnchor.MiddleCenter; t.color = LabelGreen;
            bool bold = b == Btn.Jump;   // Jump reads bold
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.fontSize = b == Btn.Pause ? 30 : size > 160 ? 52 : size > 140 ? 44 : 36; t.resizeTextForBestFit = true; t.resizeTextMinSize = 14; t.resizeTextMaxSize = t.fontSize; t.raycastTarget = false;
            var ol = tgo.AddComponent<Outline>(); ol.effectColor = b == Btn.Pause ? new Color(0.45f, 0.45f, 0.45f, 0.8f) : LabelGreen; ol.effectDistance = new Vector2(1.5f, -1.5f); ol.enabled = b == Btn.Pause;   // Pause reads gold too
            if (b == Btn.Pause) t.color = Palette.Gold;   // keeps the gold labels readable on the white disc
            rt.gameObject.AddComponent<CanvasGroup>();
            _btn[(int)b] = rt; _btnImg[(int)b] = img; _btnLabel[(int)b] = t;
        }
    }
}
