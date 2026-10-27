using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using PlantGame.Core;
using PlantGame.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace PlantGame.Player
{
    /// <summary>
    /// T57 / T58: the on-screen layer for phones (docs/mobile-controls.md). A floating stick on the left 40 percent of the
    /// screen walks (pushed to the edge = run), dragging anywhere else looks, and round buttons in the right thumb's arc jump,
    /// run the context action (pick up / plant / use / drop, label from PlayerInteraction) and water; the hotbar slots are tappable (PlayerHud).
    /// Touches are polled (EnhancedTouch) and classified when they begin: a touch that starts on a button never becomes a
    /// look drag, so stick, look and a button can be held at once. Results go to TouchInput; PlayerInputReader reads them.
    /// In the editor, GameSettings.TouchControls turns the layer on and TouchSimulation lets the mouse act as one finger.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class TouchControls : MonoBehaviour
    {
        const float StickRadiusRef = 120f, Deadzone = 0.15f, RunThreshold = 0.85f, LookCountsPerInch = 140f, StickZone = 0.4f, AxisSnap = 0.2f;

        enum Role { None, Stick, Look, Button }
        enum Btn { Jump, Context, Water, Pause, Count }

        struct Slot { public bool used; public Role role; public int btn; public Vector2 origin; public int id; }

        PlayerInputReader _input;
        PlayerInteraction _pi;
        Canvas _canvas;
        RectTransform _stickBase, _stickKnob;
        readonly RectTransform[] _btn = new RectTransform[(int)Btn.Count];
        readonly Image[] _btnImg = new Image[(int)Btn.Count];
        readonly Text[] _btnLabel = new Text[(int)Btn.Count];
        readonly bool[] _btnHeld = new bool[(int)Btn.Count];
        readonly Slot[] _slots = new Slot[16];
        Font _font;
        bool _simulating, _running;
        Vector2 _restPos;
        float _waterAlpha;
        static readonly Color LabelGreen = new Color(0.07f, 0.24f, 0.14f);

        void Awake()
        {
            _input = GetComponent<PlayerInputReader>();
            _pi = GetComponent<PlayerInteraction>();
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
                TouchInput.Move = Vector2.zero; TouchInput.Run = false;
                return;
            }
            if (!EnhancedTouchSupport.enabled) { EnhancedTouchSupport.Enable(); }

            float scale = _canvas.scaleFactor;
            float radius = StickRadiusRef * scale;
            UpdateButtonVisibility();
            System.Array.Clear(_btnHeld, 0, _btnHeld.Length);
            bool stickHeld = false;

            foreach (var t in Touch.activeTouches)
            {
                int fi = t.finger.index; if (fi < 0 || fi >= _slots.Length) continue;
                var s = _slots[fi];
                Vector2 pos = t.screenPosition;
                bool ended = t.phase == UnityEngine.InputSystem.TouchPhase.Ended || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled;
                if (t.phase == UnityEngine.InputSystem.TouchPhase.Began || !s.used || s.id != t.touchId)
                {
                    s = new Slot { used = true, id = t.touchId, origin = pos, role = Role.Look };
                    int b = HitButton(pos);
                    if (b >= 0) { s.role = Role.Button; s.btn = b; Pressed((Btn)b); }
                    else if (pos.x < Screen.width * StickZone) { s.role = Role.Stick; PlaceStick(pos); }
                }
                switch (s.role)
                {
                    case Role.Stick:
                        if (!ended)
                        {
                            Vector2 d = Vector2.ClampMagnitude((pos - s.origin) / radius, 1f);
                            float m = d.magnitude;
                            // tapping or holding the centre of the stick (inside the dead zone) walks forward
                            Vector2 v = m < Deadzone ? Vector2.up : d / m * Mathf.InverseLerp(Deadzone, 1f, m);
                            // within ~11 degrees of an axis the stick snaps to it: walking straight along a 1.2 m bridge needs no steady thumb
                            float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y);
                            if (ax < ay * AxisSnap) v.x = 0f; else if (ay < ax * AxisSnap) v.y = 0f;
                            _running = m >= (_running ? RunThreshold - 0.06f : RunThreshold + 0.04f);   // hysteresis: no flicker between walk and run at the edge
                            TouchInput.Move = v; TouchInput.Run = _running;
                            _stickKnob.position = s.origin + d * radius; stickHeld = true;
                        }
                        break;
                    case Role.Look:
                        if (!ended)
                        {
                            float k = LookCountsPerInch / Mathf.Max(Screen.dpi, 96f) * GameSettings.TouchLookSensitivity;
                            Vector2 dl = t.delta * k;
                            if (GameSettings.TouchInvertY) dl.y = -dl.y;
                            TouchInput.Look += dl;
                        }
                        break;
                    case Role.Button:
                        if (!ended) _btnHeld[s.btn] = true;
                        break;
                }
                s.used = !ended;
                _slots[fi] = s;
            }
            if (!stickHeld) { TouchInput.Move = Vector2.zero; TouchInput.Run = false; _stickBase.position = _restPos; _stickKnob.position = _restPos; }
            for (int i = 0; i < _btnImg.Length; i++) if (_btnImg[i] != null) _btnImg[i].color = _btnHeld[i] ? new Color(0.85f, 0.95f, 0.85f, 1f) : new Color(1f, 1f, 1f, 0.95f);
            _stickBase.GetComponent<Image>().color = new Color(1f, 1f, 1f, stickHeld ? 0.7f : 0.35f);
        }

        static void Tint(Text t, bool gold) { t.color = gold ? Palette.Gold : LabelGreen; t.GetComponent<Outline>().enabled = gold; }

        void Pressed(Btn b)
        {
            switch (b)
            {
                case Btn.Jump: TouchInput.JumpPressed = true; break;
                case Btn.Context: TouchInput.ContextPressed = true; break;
                case Btn.Water: TouchInput.WaterPressed = true; break;
                case Btn.Pause: TouchInput.PausePressed = true; break;
            }
        }

        void PlaceStick(Vector2 p) { _stickBase.position = p; _stickKnob.position = p; }

        int HitButton(Vector2 p)
        {
            for (int i = 0; i < _btn.Length; i++)
                if (_btn[i].gameObject.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(_btn[i], p, null) && (i != (int)Btn.Water || _waterAlpha > 0.5f)) return i;
            return -1;
        }

        void UpdateButtonVisibility()
        {
            if (_pi == null) return;
            bool ctx = _pi.Context != PlayerInteraction.ContextKind.None;
            _btn[(int)Btn.Context].gameObject.SetActive(ctx);
            if (ctx)
            {
                string l = _pi.ContextLabel;
                if ((l.StartsWith("Pick up ") || l.StartsWith("Drop ")) && l.EndsWith(" Seed", System.StringComparison.OrdinalIgnoreCase)) l = l.Substring(0, l.Length - 5);   // "Pick up Beanstalk": the word seed does not fit
                _btnLabel[(int)Btn.Context].text = l;
                Tint(_btnLabel[(int)Btn.Context], _pi.Context == PlayerInteraction.ContextKind.Drop);   // Drop reads gold
            }
            bool water = _pi.WaterUseful;
            if (water) { _btnLabel[(int)Btn.Water].text = _pi.WaterLabel; _waterAlpha = 1f; }
            else _waterAlpha = Mathf.MoveTowards(_waterAlpha, 0f, Time.unscaledDeltaTime * 2.5f);   // fades out (e.g. the can is full) instead of vanishing
            _btn[(int)Btn.Water].gameObject.SetActive(_waterAlpha > 0.001f);
            _btn[(int)Btn.Water].GetComponent<CanvasGroup>().alpha = _waterAlpha;
            Tint(_btnLabel[(int)Btn.Water], _btnLabel[(int)Btn.Water].text == "Refill");   // Refill reads gold
        }

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

            _stickBase = Circle("StickBase", root, new Vector2(0f, 0f), new Vector2(260, 260), new Vector2(260, 260), UiSprites.Ring);
            _stickKnob = Circle("StickKnob", root, new Vector2(0f, 0f), new Vector2(110, 110), new Vector2(260, 260), UiSprites.Disc);
            var gold = Palette.Gold; gold.a = 0.85f; _stickKnob.GetComponent<Image>().color = gold;   // ring white, knob gold: the player's colours

            MakeButton(Btn.Jump, root, new Vector2(1f, 0f), new Vector2(-170, 190), 170, "Jump");
            MakeButton(Btn.Context, root, new Vector2(1f, 0f), new Vector2(-420, 150), 190, "");
            MakeButton(Btn.Water, root, new Vector2(1f, 0f), new Vector2(-150, 410), 125, "Water");
            MakeButton(Btn.Pause, root, new Vector2(0f, 1f), new Vector2(120, -170), 150, "Pause");
            LayoutNow();
        }

        void LayoutNow()
        {
            Canvas.ForceUpdateCanvases();
            _restPos = _stickBase.position;
            _stickKnob.position = _restPos;
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
            bool bold = b == Btn.Jump || b == Btn.Context;   // Jump, pick up and use read bold
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.fontSize = b == Btn.Pause ? 30 : b == Btn.Context ? 44 : size > 160 ? 52 : size > 140 ? 44 : 36; t.resizeTextForBestFit = true; t.resizeTextMinSize = 14; t.resizeTextMaxSize = t.fontSize; t.raycastTarget = false;
            var ol = tgo.AddComponent<Outline>(); ol.effectColor = b == Btn.Pause ? new Color(0.45f, 0.45f, 0.45f, 0.8f) : LabelGreen; ol.effectDistance = new Vector2(1.5f, -1.5f); ol.enabled = b == Btn.Pause;   // Pause reads gold too
            if (b == Btn.Pause) t.color = Palette.Gold;   // keeps the gold labels readable on the white disc
            rt.gameObject.AddComponent<CanvasGroup>();
            _btn[(int)b] = rt; _btnImg[(int)b] = img; _btnLabel[(int)b] = t;
        }
    }
}
