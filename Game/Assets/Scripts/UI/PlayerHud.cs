using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PlantGame.Core;
using PlantGame.Inventory;
using PlantGame.Plants;
using PlantGame.Player;

namespace PlantGame.UI
{
    /// <summary>
    /// MVP HUD (built at runtime, no prefab): 4-slot hotbar with the selected slot highlighted, water charges, the interaction
    /// prompt with its key, and an Esc pause menu (Resume / Restart / Quit) that freezes time and frees the cursor.
    /// Full HUD styling is T19; this is the functional version.
    /// </summary>
    [DefaultExecutionOrder(40)]
    public class PlayerHud : MonoBehaviour
    {
        static float SlotSize => TouchInput.Active ? 130f : 92f;   // touch: thumb-sized slots (T60)
        const float Gap = 28f;
        const string PauseTip = "Tip: the plants for later gaps grow on other islands.\nYour bag holds only 4, so plan ahead - or walk back.";

        PlayerInteraction _pi;
        PlayerInputReader _input;
        PlayerController _pc;
        WateringCan _can;
        Font _font;

        Image[] _slotBg, _slotIcon;
        Text[] _slotNum;
        Text _selName, _prompt, _waterLabel, _waterWarn, _hints, _hintsButtonLabel;
        float _flashUntil;
        Text _timer, _endTitle, _endBody;
        GameObject _endPanel, _nextButton;
        Transform[] _endButtons;
        float _endAt = -1f;
        bool _ended;
        Image[] _drops;
        static readonly Color WarnOrange = new Color(1f, 0.72f, 0.3f);   // lighter than the EMPTY label's orange
        GameObject _pausePanel;
        Text _pauseHint, _retryLabel, _glowLabelP, _objective, _musicLabelP, _soundLabelP, _followLabelP, _fpsLabelP, _pauseTitle;
        static readonly Color GoalGreen = new Color(0.35f, 1f, 0.5f);   // bright green: readable on sky and scenery
        GoalFlag _goal;
        GameObject _copyCode;
        static bool _skipMenu;   // set by Restart (and by the title screen) so the run starts without bouncing back to the title screen
        public static void SkipMenuOnce() { _skipMenu = true; }
        public bool Paused { get; private set; }

        void Awake()
        {
            _pi = GetComponent<PlayerInteraction>();
            _input = GetComponent<PlayerInputReader>();
            _pc = GetComponent<PlayerController>();
            _can = GetComponent<WateringCan>();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>();
            }
            Build();
        }

        void Start()
        {
            _goal = FindAnyObjectByType<GoalFlag>();
            bool skip = _skipMenu; _skipMenu = false;
            // Main (the game scene) without a run handed over by the title screen: go to the real menu there. Test scenes just start.
            if (!skip && SceneManager.GetActiveScene().name == GameScene && Application.CanStreamedLevelBeLoaded(TitleScene)) { Time.timeScale = 1f; SceneManager.LoadScene(TitleScene); return; }
            if (_input != null) _input.Enabled = true;
            if (_pc != null) _pc.SetCursorLocked(true);
            var rs0 = RunSession.Instance; if (rs0 != null) rs0.StartRun(rs0.Config);
        }

        void OnSlotTapped(int i)
        {
            if (Paused || _ended || _pi == null) return;
            var inv = _pi.Inventory;
            bool again = i == _lastTapSlot && Time.unscaledTime - _lastTapTime < DoubleTapSeconds;
            _lastTapSlot = i; _lastTapTime = Time.unscaledTime;
            if (inv.Selected != i) inv.Select(i);   // a tap selects ...
            if (again && inv.Get(i) != null) { _pi.Drop(); _lastTapSlot = -1; }   // ... a double tap drops what the slot holds
        }

        const float DoubleTapSeconds = 0.4f;
        int _lastTapSlot = -1; float _lastTapTime;
        static RectTransform _hotbarRect;
        /// <summary>True when the screen point is over the hotbar (the touch layer leaves those touches to the slots).</summary>
        public static bool OverHotbar(Vector2 screenPos) => _hotbarRect != null && _hotbarRect.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(_hotbarRect, screenPos, null);

        void OnEnable() { GameEvents.CanEmptyTried += OnCanEmptyTried; GameEvents.HintRequested += OnHint; GameEvents.RunFinished += OnRunFinished; }
        void OnDisable() { GameEvents.CanEmptyTried -= OnCanEmptyTried; GameEvents.HintRequested -= OnHint; GameEvents.RunFinished -= OnRunFinished; if (Paused) SetPaused(false); }

        // the end panel appears shortly after the event so the flag can rise first
        void OnRunFinished(float seconds, bool newBest)
        {
            var rs = RunSession.Instance;
            float? best = rs != null ? SaveService.BestTime(rs.SaveKey) : null;
            _endTitle.text = "Goal reached!"; _retryLabel.text = "Play again";
            _endBody.text = "Time  " + Fmt(seconds) + "\n" + (newBest ? "New best!" : best.HasValue ? "Best  " + Fmt(best.Value) : "");
            if (SeedLine().Length > 0) _endBody.text += "\n" + SeedLine();
            _endAt = Time.unscaledTime + 1.8f;
            LayoutEndButtons(NextCuratedLevel() != null);
        }

        void ShowEnd()
        {
            _ended = true; _endAt = -1f;
            if (_pc != null) _pc.PauseOrbit = true;
            _endPanel.SetActive(true);
            Time.timeScale = 0f;
            if (_input != null) _input.Enabled = false;
            if (_pc != null) _pc.SetCursorLocked(false);
            else { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        }

        static string Fmt(float t) { int s = (int)t; return (s / 60).ToString("00") + ":" + (s % 60).ToString("00"); }
        string _hint; float _hintUntil;
        void OnHint(string t) { _hint = t; _hintUntil = Time.unscaledTime + 3.5f; }
        void OnCanEmptyTried() { _flashUntil = Time.unscaledTime + 1.5f; }

        Text _fps;
        float _fpsAcc; int _fpsFrames;

        void UpdateFps()
        {
            _fpsAcc += Time.unscaledDeltaTime; _fpsFrames++;
            if (_fpsAcc < 0.5f) return;
            _fps.text = Mathf.RoundToInt(_fpsFrames / _fpsAcc) + " FPS";
            _fpsAcc = 0f; _fpsFrames = 0;
        }

        void Update()
        {
            if (_fps.enabled != GameSettings.Fps) _fps.enabled = GameSettings.Fps;
            UpdateFps();
            if (_endAt >= 0f && Time.unscaledTime >= _endAt && !_ended) ShowEnd();
            if (_ended) return;
            if (_input != null && _input.PausePressed) SetPaused(!Paused);
            if (Paused) return;
            Refresh();
        }

        // ---------------------------------------------------------------- pause
        public void SetPaused(bool pause)
        {
            Paused = pause;
            if (_pc != null) _pc.PauseOrbit = pause;
            _pausePanel.SetActive(pause);
            if (pause) RefreshOptionLabels();
            if (pause && _pauseHint != null)
            {
                _pauseHint.gameObject.SetActive(SeedLine().Length > 0 || (GameSettings.Hints));
                if (_copyCode != null) _copyCode.SetActive(SeedLine().Length > 0);
                _pauseHint.text = SeedLine().Length > 0 ? SeedLine() + "   (share it: the same code gives the same world)" : PauseTip;
            }
            Time.timeScale = pause ? 0f : 1f;
            if (_input != null) _input.Enabled = !pause;
            if (_pc != null) _pc.SetCursorLocked(!pause);
            else { Cursor.lockState = pause ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = pause; }
        }

        void Restart() { RestartRun(); }

        /// <summary>The curated level after the current one (wraps from the last back to the first), null for random / code levels.</summary>
        static LevelDefinition NextCuratedLevel()
        {
            var levels = PlantGame.World.Kit.Asset != null ? PlantGame.World.Kit.Asset.levels : null;
            string id = GameBootstrap.Current.levelId;
            if (levels == null || levels.Length == 0 || string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < levels.Length; i++) if (levels[i] != null && levels[i].id == id) return levels[(i + 1) % levels.Length];
            return null;
        }

        void NextLevel()
        {
            var next = NextCuratedLevel();
            if (next == null) return;
            GameBootstrap.PendingRun = next.ToConfig();
            RestartRun();
        }

        void LayoutEndButtons(bool showNext)
        {
            _nextButton.SetActive(showNext);
            float y = -70f;
            if (showNext) y -= 100f;
            foreach (var t in _endButtons) { ((RectTransform)t).anchoredPosition = new Vector2(0, y); y -= 100f; }
        }

        /// <summary>Reloads the scene and goes straight into the run (same RunConfig, same world).</summary>
        public void RestartRun() { _skipMenu = true; Reload(); }

        /// <summary>Reloads the scene and shows the start menu again.</summary>
        void ToMenu()
        {
            _skipMenu = false;
            if (Application.CanStreamedLevelBeLoaded(TitleScene)) { SetPaused(false); Time.timeScale = 1f; SceneManager.LoadScene(TitleScene); }
            else Reload();
        }
        public const string TitleScene = "Title", GameScene = "Main";

        /// <summary>The seed code of a random / code level, empty for curated levels.</summary>
        static string SeedLine()
        {
            var c = GameBootstrap.Current;
            return string.IsNullOrEmpty(c.levelId) ? "Seed: " + SeedCode.Encode(c) : "";
        }

        void Reload()
        {
            SetPaused(false);
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex >= 0 ? SceneManager.GetActiveScene().buildIndex : 0);
        }

        void Quit()
        {
            SetPaused(false);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------------------------------------------------------------- refresh
        void Refresh()
        {
            if (_goal == null) _goal = FindAnyObjectByType<GoalFlag>();
            if (_goal != null)
            {
                bool near = _goal.PlayerNear;
                _objective.text = near ? "You are at the goal - activate it" : _goal.Objective;
                _objective.color = near ? Palette.GlowCyan : GoalGreen;
            }
            var inv = _pi.Inventory;
            for (int i = 0; i < Inventory.Inventory.Slots; i++)
            {
                var it = inv.Get(i);
                bool sel = i == inv.Selected;
                _slotBg[i].color = sel ? new Color(0.49f, 0.95f, 0.9f, 0.9f) : new Color(0.05f, 0.1f, 0.15f, 0.55f);
                _slotIcon[i].enabled = it != null;
                if (it != null)
                {
                    _slotIcon[i].sprite = it.icon != null ? it.icon : UiSprites.ForItem(it.id) ?? UiSprites.Disc;
                    _slotIcon[i].color = it.icon != null ? Color.white : ItemColor(it);
                }
            }
            var s = inv.SelectedItem;
            _selName.text = s != null ? s.displayName : "";

            int charges = _can != null ? _can.Charges : 0;
            bool empty = charges <= 0, flashing = Time.unscaledTime < _flashUntil;
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * (flashing ? 18f : 5f));
            for (int i = 0; i < _drops.Length; i++)
            {
                _drops[i].enabled = !empty;   // no droplets on an empty can
                if (i < charges) _drops[i].color = new Color(0.44f, 0.66f, 0.86f, 1f);
                else _drops[i].color = new Color(0.44f, 0.66f, 0.86f, 0.22f);
            }
            _waterLabel.supportRichText = true;
            _waterLabel.text = empty ? "Water      <color=#" + ColorUtility.ToHtmlStringRGB(Palette.TextOrange) + ">EMPTY</color>" : "Water";
            _waterLabel.color = TouchInput.Active ? new Color(0.06f, 0.33f, 0.16f, 1f) : new Color(0.44f, 0.66f, 0.86f, 1f);   // desktop: same blue as the droplets; touch: dark green, readable on the sky
            bool nearWater = _pi != null && InteractionMarkers.RefillRingThick && _can != null && !_can.IsFull && GameSettings.Hints && !Paused;
            if (nearWater)
            {   // at the water with room in the can: purple refill hint
                _waterWarn.enabled = true; _waterWarn.text = "Refill water at source (pond, stream, plant)"; _waterWarn.color = Color.white;
            }
            else
            {
                _waterWarn.enabled = empty && GameSettings.Hints;
                _waterWarn.text = "Refill water at source (pond, stream, plant)";
                _waterWarn.color = Color.white;
            }

            _hints.enabled = GameSettings.Hints && !TouchInput.Active;   // key hints make no sense on the touch layout
            if (_hints.enabled)
                _hints.text = Key("Pickup", "Q") + " - pickup / drop\n" + Key("Use", "E") + " - plant\n" + Key("Water", "F") + " - water / refill can\nSpace - Jump" + (_pc != null && (_pc.IsClimbing || _pc.CurrentClimbHint != PlayerController.ClimbHint.None) ? "\nW / S or Up / Down - climb" : "") + "\nTab - next slot";

            var rs = RunSession.Instance;
            if (rs == null || rs.State == RunState.Idle) _timer.text = "";
            else { _timer.text = Fmt(rs.Elapsed); _timer.color = Palette.TextOrange; }

            string p = Paused ? "" : _pi.CurrentPrompt, key = _pi.CurrentPromptKey;
            if (string.IsNullOrEmpty(p)) _prompt.text = "";
            else if (TouchInput.Active && key != "Frozen") _prompt.text = key == "Water" ? (_pi.NearRefillSource ? "Refill water" : "Water the sprout") + (empty && !_pi.NearRefillSource ? "  (can is empty!)" : "") : p;   // touch: the button says it
            else if (key == "Water") _prompt.text = "Press [" + (_input != null ? _input.KeyLabel("Water") : "F") + "] to " + (_pi.NearRefillSource ? "refill water" : "water") + (empty && !_pi.NearRefillSource ? "  (can is empty!)" : "");
            else if (key == "Frozen") _prompt.text = p;
            else _prompt.text = "[" + (_input != null ? _input.KeyLabel(key) : "E") + "]  " + p;
            _prompt.color = key == "Water" && !string.IsNullOrEmpty(p) ? Palette.TextPurple : key == "Frozen" ? new Color(0.65f, 0.9f, 1f) : Palette.TextOrange;
            if (string.IsNullOrEmpty(_prompt.text) && _pc != null && _pc.IsAirborne && !_pc.IsGliding && _pi.Inventory.SelectedItem != null && _pi.Inventory.SelectedItem.id == "parachute_leaf")
                _prompt.text = TouchInput.Active ? "Tap Jump to open the leaf" : "Press [" + Key("Use", "E") + "] to glide";
            if (string.IsNullOrEmpty(_prompt.text) && Time.unscaledTime < _hintUntil) _prompt.text = _hint;
            if (string.IsNullOrEmpty(_prompt.text) && _pc != null && TouchInput.Active)   // touch ladder hints: tap the stalk
                switch (_pc.CurrentClimbHint)
                {
                    case PlayerController.ClimbHint.Climb: _prompt.text = "Tap the beanstalk to climb"; break;
                    case PlayerController.ClimbHint.Descend: _prompt.text = "Tap the beanstalk to climb down"; break;
                    case PlayerController.ClimbHint.Climbing: _prompt.text = "Tap above or below to climb    Jump to let go"; break;
                }
            if (string.IsNullOrEmpty(_prompt.text) && _pc != null)   // ladder hints (W / S climb are fixed by the controls spec)
                switch (_pc.CurrentClimbHint)
                {
                    case PlayerController.ClimbHint.Climb: _prompt.text = "Press [W] or [Up] to climb"; break;
                    case PlayerController.ClimbHint.Descend: _prompt.text = "Press [S] or [Down] to climb down"; break;
                    case PlayerController.ClimbHint.Climbing: _prompt.text = "[W / Up] up    [S / Down] down    [Space] jump off"; break;
                }
            // one rule: with Hints off only the level goal and its interaction stay (the objective line above is never gated)
            if (!GameSettings.Hints && !_pi.CurrentPromptIsGoal) _prompt.text = "";
        }

        public static Color ItemColor(ItemDefinition it)
        {
            if (it.id == "parachute_leaf") return Palette.LeafYellow;   // the glide leaf is yellow in the hotbar as well as in the world
            if (it.id == "heat_seed") return new Color(1f, 0.66f, 0.14f);   // golden melon
            if (it.id == "heart_seed") return new Color(0.9f, 0.14f, 0.2f);   // the Heart Seed is a red heart
            if (it.id == "shelf_seed") return Palette.CloudWhite;       // T33: seeds keep their pickup colour in the hotbar
            if (it.worldPrefab != null)
            {
                var r = it.worldPrefab.GetComponentInChildren<Renderer>();
                if (r != null && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor")) return r.sharedMaterial.GetColor("_BaseColor");
            }
            return Palette.LeafGreen;
        }

        // ---------------------------------------------------------------- build
        void Build()
        {
            var cgo = new GameObject("HUD", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = UiLayout.MatchHeight;
            cgo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)cgo.transform;

            // hotbar
            int n = Inventory.Inventory.Slots;
            float total = n * SlotSize + (n - 1) * Gap;
            // touch: the hotbar uses the extra width of a phone screen, in the top right corner, clear of both thumbs (the sticks and buttons own the bottom)
            bool touch = TouchInput.Active;
            var bar = touch ? Rect("Hotbar", root, new Vector2(1f, 1f), new Vector2(total, SlotSize), new Vector2(-(total * 0.5f + 40f), -(SlotSize * 0.5f + 30f)))
                            : Rect("Hotbar", root, new Vector2(0.5f, 0f), new Vector2(total, SlotSize), new Vector2(0, 90));
            _hotbarRect = bar;
            _slotBg = new Image[n]; _slotIcon = new Image[n]; _slotNum = new Text[n];
            for (int i = 0; i < n; i++)
            {
                var slot = Rect("Slot" + (i + 1), bar, new Vector2(0f, 0.5f), new Vector2(SlotSize, SlotSize), new Vector2(i * (SlotSize + Gap) + SlotSize * 0.5f, 0));
                var rim = Img("Rim", slot, RoundedSquare(), Palette.Gold, SlotSize + 10f); rim.type = Image.Type.Sliced;   // gold outline
                _slotBg[i] = Img("Bg", slot, UiSprites.Disc, Color.white, SlotSize);
                _slotBg[i].sprite = RoundedSquare();
                _slotBg[i].type = Image.Type.Sliced;
                _slotBg[i].raycastTarget = true;
                int slotIndex = i;
                slot.gameObject.AddComponent<Button>().onClick.AddListener(() => OnSlotTapped(slotIndex));   // tap selects; tapping the selected slot again drops it
                _slotIcon[i] = Img("Icon", slot, UiSprites.Disc, Color.white, SlotSize * 0.82f);
            }
            _selName = touch ? Label("SelectedName", root, "", 28, TextAnchor.MiddleCenter, new Vector2(-(total * 0.5f + 40f), -(SlotSize + 66f)), new Vector2(total, 40), new Vector2(1f, 1f))
                             : Label("SelectedName", root, "", 26, TextAnchor.MiddleCenter, new Vector2(0, SlotSize + 110), new Vector2(600, 36), new Vector2(0.5f, 0f));
            _hints = Label("Hints", root, "", 26, TextAnchor.LowerRight, new Vector2(-300, 120), new Vector2(520, 160), new Vector2(1f, 0f), false);
            _hints.color = Palette.TextOrange;
            _timer = Label("Timer", root, "", TouchInput.Active ? 62 : 46, TextAnchor.MiddleCenter, new Vector2(0, -50), new Vector2(500, 70), new Vector2(0.5f, 1f), false);
            _prompt = Label("Prompt", root, "", TouchInput.Active ? 44 : 36, TextAnchor.MiddleCenter, new Vector2(0, TouchInput.Active ? 0 : 70), new Vector2(1000, 54), new Vector2(0.5f, 0.2f), false);
            _prompt.color = Palette.TextOrange;
            _objective = Label("Objective", root, "", 26, TextAnchor.UpperCenter, new Vector2(0, -135), new Vector2(1400, 80), new Vector2(0.5f, 1f));
            _objective.color = GoalGreen;
            _fps = Label("Fps", root, "", 28, TextAnchor.UpperLeft, new Vector2(160, -38), new Vector2(240, 40), new Vector2(0f, 1f));   // top left (touch: the Pause button sits under it)
            _fps.color = new Color(0.2f, 1f, 0.35f);   // bright green with a dark shadow: readable on sky and scenery
            _fps.GetComponent<Shadow>().effectColor = new Color(0f, 0.1f, 0.05f, 0.9f);

            // water charges, bottom left
            var water = touch ? Rect("Water", root, new Vector2(1f, 1f), new Vector2(300, 50), new Vector2(-150, -(SlotSize + 175f)), new Vector2(1f, 0.5f))   // touch: top right, a line below the selected item name and clear of it
                              : Rect("Water", root, new Vector2(0f, 0f), new Vector2(300, 50), new Vector2(40, 150), new Vector2(0f, 0.5f));
            _waterLabel = Label("WaterLabel", water, "Water", 24, TextAnchor.MiddleLeft, new Vector2(230, 22), new Vector2(460, 50), new Vector2(0f, 0.5f));
            _waterWarn = Label("WaterWarn", root, "", 26, TextAnchor.MiddleLeft, touch ? new Vector2(270, 206) : new Vector2(370, 86), touch ? new Vector2(620, 76) : new Vector2(660, 36), new Vector2(0f, 0f));   // below the water indicator, raised half its font size (13)
            if (touch) { var ol = _waterLabel.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1f, 1f, 1f, 0.85f); ol.effectDistance = new Vector2(2, -2); }
            _drops = new Image[WateringCan.MaxCharges];
            for (int i = 0; i < _drops.Length; i++)
            {
                float ds = touch ? 48f : 40f;   // touch droplets are 20% bigger
                var d = Rect("Drop" + i, water, new Vector2(0f, 0.5f), new Vector2(ds, ds), new Vector2((touch ? 190 : 140) + i * (touch ? 55 : 46), 22));
                _drops[i] = Img("Fill", d, UiSprites.Drop, Color.white, ds);
            }

            // pause menu
            _pausePanel = new GameObject("Pause", typeof(RectTransform));
            var pr = (RectTransform)_pausePanel.transform; pr.SetParent(root, false);
            pr.anchorMin = Vector2.zero; pr.anchorMax = Vector2.one; pr.offsetMin = pr.offsetMax = Vector2.zero;
            var dim = _pausePanel.AddComponent<Image>(); dim.color = new Color(0.04f, 0.09f, 0.14f, 0.72f);
            // touch (phones are wide): two columns, options and sliders on the left, actions on the right; desktop keeps the centred stack
            bool two = TouchInput.Active;
            Vector2 V(Vector2 desk, Vector2 wide) => two ? wide : desk;
            _pauseTitle = Label("Title", pr, "Paused", 72, TextAnchor.MiddleCenter, new Vector2(0, 380), new Vector2(1200, 100), new Vector2(0.5f, 0.5f));
            _hintsButtonLabel = Button("Hints", pr, V(new Vector2(0, 260), new Vector2(-650, 250)), () => { GameSettings.Hints = !GameSettings.Hints; RefreshOptionLabels(); }, two ? 400 : 300, 30);
            _musicLabelP = Button("Music", pr, V(new Vector2(-330, 160), new Vector2(-650, 150)), () => { GameSettings.Music = !GameSettings.Music; RefreshOptionLabels(); }, two ? 400 : 300, 30);
            _soundLabelP = Button("Sound", pr, V(new Vector2(0, 160), new Vector2(-200, 150)), () => { GameSettings.Sound = !GameSettings.Sound; RefreshOptionLabels(); }, two ? 400 : 300, 30);
            _followLabelP = Button("Camera", pr, V(new Vector2(380, 260), new Vector2(-650, 50)), () => { GameSettings.CameraFollow = !GameSettings.CameraFollow; RefreshOptionLabels(); }, 400, 30);
            _glowLabelP = Button("Postprocessing", pr, V(new Vector2(380, 160), new Vector2(-200, 50)), () => { GameSettings.Bloom = !GameSettings.Bloom; RefreshOptionLabels(); }, 400, 30);
            _fpsLabelP = Button("Fps", pr, V(new Vector2(-330, 260), new Vector2(-200, 250)), () => { GameSettings.Fps = !GameSettings.Fps; RefreshOptionLabels(); }, two ? 400 : 300, 30);
            if (!TouchInput.Active)   // phones: only the system volume works
            UiKit.Slider(pr, _font, "Music volume", V(new Vector2(-640, 60), new Vector2(-435, -70)), 380, GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
            UiKit.Slider(pr, _font, "Sound volume", V(new Vector2(640, 60), new Vector2(-435, -190)), 380, GameSettings.SoundVolume, v => { GameSettings.SoundVolume = v; Audio.AudioManager.PlaySfx("ui_click"); });
            var resumeText = Button("Resume", pr, V(new Vector2(0, 60), new Vector2(420, 220)), () => SetPaused(false));
            resumeText.transform.parent.GetComponent<Image>().color = new Color(0.22f, 0.58f, 0.32f, 0.97f);   // green: the main action
            Button("Restart", pr, V(new Vector2(0, -40), new Vector2(420, 110)), Restart);
            Button("Menu", pr, V(new Vector2(0, -140), new Vector2(420, 0)), ToMenu);
            TitleMenu.Danger(Button("Quit", pr, V(new Vector2(0, -240), new Vector2(420, -110)), Quit));
            _copyCode = Button("Copy seed code", pr, V(new Vector2(640, -60), new Vector2(420, -230)), () => GUIUtility.systemCopyBuffer = SeedCode.Encode(GameBootstrap.Current), 360, 28).transform.parent.gameObject;
            _pauseHint = Label("PuzzleHint", pr, PauseTip, 28, TextAnchor.MiddleCenter, new Vector2(0, -400), new Vector2(1500, 110), new Vector2(0.5f, 0.5f), true, true);
            _pauseHint.color = Color.white;
            _pausePanel.SetActive(false);

            // end panel (win / fail)
            _endPanel = new GameObject("End", typeof(RectTransform));
            var er = (RectTransform)_endPanel.transform; er.SetParent(root, false);
            er.anchorMin = Vector2.zero; er.anchorMax = Vector2.one; er.offsetMin = er.offsetMax = Vector2.zero;
            var edim = _endPanel.AddComponent<Image>(); edim.color = new Color(0.04f, 0.09f, 0.14f, 0.78f);
            _endTitle = Label("EndTitle", er, "", 84, TextAnchor.MiddleCenter, new Vector2(0, 200), new Vector2(1000, 120), new Vector2(0.5f, 0.5f));
            _endBody = Label("EndBody", er, "", 44, TextAnchor.MiddleCenter, new Vector2(0, 80), new Vector2(1000, 150), new Vector2(0.5f, 0.5f), true, true);
            _endBody.color = Palette.TextOrange;
            _nextButton = Button("Next level", er, new Vector2(0, -70), NextLevel).transform.parent.gameObject;
            _retryLabel = Button("Play again", er, new Vector2(0, -70), Restart);
            _endButtons = new[] { _retryLabel.transform.parent, Button("Menu", er, new Vector2(0, -170), ToMenu).transform.parent, TitleMenu.Danger(Button("Quit", er, new Vector2(0, -270), Quit)).transform.parent };
            _endPanel.SetActive(false);
        }

        static Sprite _square;
        static Sprite RoundedSquare()
        {
            if (_square != null) return _square;
            const int S = 32, R = 8;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = Mathf.Max(R - x, x - (S - 1 - R), 0), dy = Mathf.Max(R - y, y - (S - 1 - R), 0);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(R + 0.5f - Mathf.Sqrt(dx * dx + dy * dy))));
            }
            tex.Apply();
            _square = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
            _square.hideFlags = HideFlags.HideAndDontSave;
            return _square;
        }

        RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 pos, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor; rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            return rt;
        }

        Image Img(string name, Transform parent, Sprite sprite, Color c, float size)
        {
            var rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(size, size), Vector2.zero);
            var im = rt.gameObject.AddComponent<Image>(); im.sprite = sprite; im.color = c; im.raycastTarget = false;
            return im;
        }

        Text Label(string name, Transform parent, string text, int size, TextAnchor align, Vector2 pos, Vector2 box, Vector2? anchor = null, bool scale = true, bool menu = false)
        {
            var rt = Rect(name, parent, anchor ?? new Vector2(0f, 1f), box, pos, anchor == null ? new Vector2(0f, 1f) : (Vector2?)null);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font; t.text = text; if (scale) UiLayout.Fit(t, size, menu); else t.fontSize = size; t.alignment = align; t.raycastTarget = false;
            t.color = Palette.TextPurple;
            var sh = rt.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.6f); sh.effectDistance = new Vector2(2, -2);
            return t;
        }

        static string OnOff(string n, bool v) => n + ": " + (v ? "On" : "Off");
        void RefreshOptionLabels()
        {
            foreach (var t in new[] { _hintsButtonLabel }) if (t != null) t.text = OnOff("Hints", GameSettings.Hints);
            if (_pauseHint != null && _pausePanel.activeSelf && SeedLine().Length == 0) _pauseHint.gameObject.SetActive(GameSettings.Hints);   // the tip follows the Hints toggle
            if (_pauseTitle != null) _pauseTitle.text = GameSettings.Hints && !TouchInput.Active ? "Paused  <size=26><color=#BFD9EE>Esc to resume</color></size>" : "Paused";
            if (_fpsLabelP != null) _fpsLabelP.text = OnOff("FPS counter", GameSettings.Fps);
            if (_glowLabelP != null) _glowLabelP.text = OnOff("Postprocessing", GameSettings.Bloom);
            if (_followLabelP != null) _followLabelP.text = "Camera follows: " + (GameSettings.CameraFollow ? "On" : "Off");
            foreach (var t in new[] { _musicLabelP }) if (t != null) t.text = OnOff("Music", GameSettings.Music);
            foreach (var t in new[] { _soundLabelP }) if (t != null) t.text = OnOff("Sound", GameSettings.Sound);
        }

        string Key(string action, string fallback) => _input != null ? _input.KeyLabel(action) : fallback;

        Text Button(string label, Transform parent, Vector2 pos, UnityEngine.Events.UnityAction click, float width = 360f, int fontSize = 36)
        {
            float height = 76f;
            if (TouchInput.Active) { width *= 1.2f; height = 92f; }   // phones: bigger touch targets
            var rt = Rect(label + "Button", parent, new Vector2(0.5f, 0.5f), new Vector2(width, height), pos);
            var im = rt.gameObject.AddComponent<Image>(); im.sprite = RoundedSquare(); im.type = Image.Type.Sliced; im.color = new Color(0.17f, 0.37f, 0.56f, 0.95f);
            var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = im; b.onClick.AddListener(click);
            b.onClick.AddListener(() => Audio.AudioManager.PlaySfx("ui_click"));
            var trig = rt.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => Audio.AudioManager.PlaySfx("ui_hover"));
            trig.triggers.Add(enter);
            var cb = b.colors; cb.highlightedColor = new Color(0.49f, 0.95f, 0.9f, 1f); cb.pressedColor = Palette.SkyBlue; b.colors = cb;
            var t = Label("Text", rt, label, fontSize, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(width, height), new Vector2(0.5f, 0.5f), true, true);
            t.color = Palette.TextOrange;
            return t;
        }
    }
}
