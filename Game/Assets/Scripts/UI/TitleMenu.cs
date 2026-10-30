using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PlantGame.Core;
using PlantGame.Player;
using PlantGame.Plants;
using PlantGame.World;

namespace PlantGame.UI
{
    /// <summary>
    /// T26: title screen and level select, built at runtime. Home (Play / Quit), then the level screen: curated level cards with best times,
    /// a Random Level block (difficulty, its seed code is shown before you start and can be copied), a seed code field (type or Paste) with live
    /// validation. Start puts the RunConfig in GameBootstrap.PendingRun and loads Main.
    /// </summary>
    public class TitleMenu : MonoBehaviour
    {
        public string gameScene = "Main";

        Font _font;
        WorldGenerator _showcase; Vector3[] _tour, _tourLook;
        GameObject _home, _levels, _options, _generate;
        Text _musicToggle, _soundToggle, _hintsToggle, _fpsToggle, _followToggle, _glowToggle, _autoWaterToggle, _fps30Toggle, _qualityToggle;
        Text _diffLabel, _randomCode, _codeStatus, _resetNote;
        InputField _codeField;
        Text[] _cardInfo;
        int _difficulty = 1;   // 0 easy (4 islands), 1 normal (6), 2 hard (8)
        int _randomSeed;
        RunConfig _codeConfig; bool _codeValid;
        Sprite _square;

        static readonly (string label, string scene)[] TestScenes =
        {
            ("Interaction test", "Sandbox"),
        };
        static readonly Color ExploreGreen = new Color(0.18f, 0.52f, 0.32f, 0.97f);
        static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard" };
        static readonly int[] DifficultyIslands = { 4, 6, 8 };

        void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (FindAnyObjectByType<EventSystem>() == null) { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>(); }
            Build();
            _showcase = FindAnyObjectByType<WorldGenerator>();
            if (_showcase != null) StartCoroutine(GrowShowcase());
            NewRandomSeed();
            Show(_home);
        }

        void RefreshOptions()
        {
            _musicToggle.text = "Music: " + (GameSettings.Music ? "On" : "Off");
            _soundToggle.text = "Sound: " + (GameSettings.Sound ? "On" : "Off");
            _hintsToggle.text = "Hints: " + (GameSettings.Hints ? "On" : "Off");
            _fpsToggle.text = "FPS counter: " + (GameSettings.Fps ? "On" : "Off");
            if (_fps30Toggle != null) _fps30Toggle.text = "Battery saver: " + (GameSettings.Fps30 ? "On" : "Off");
            _autoWaterToggle.text = "Auto water: " + (GameSettings.AutoWater ? "On" : "Off");
            _followToggle.text = "Camera follows: " + (GameSettings.CameraFollow ? "On" : "Off");
            _glowToggle.text = "Postprocessing: " + (GameSettings.Bloom ? "On" : "Off");
            _qualityToggle.text = "Quality: " + QualityTier.Names[GameSettings.Quality];
        }

        void Update()
        {
            // Esc / Android Back: back out of a sub-panel to the home screen
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame && _home != null && !_home.activeSelf && !_codeField.isFocused) Show(_home);
            var cam = Camera.main;
            if (cam == null) return;
            if (_showcase != null && _showcase.Layout != null && _showcase.Layout.isles.Count > 1)
            {
                // a looping fly-by: close on each island in turn, wide and high over the group in between (see SetupTour)
                if (_tour == null) SetupTour(_showcase.Layout.isles);
                int n = _tour.Length; float u = Time.unscaledTime / TourSegmentSeconds; int k = Mathf.FloorToInt(u); float t = u - k;
                Vector3 At(Vector3[] pts, int i) => pts[((i % n) + n) % n];
                Vector3 Spline(Vector3[] pts) => CatmullRom(At(pts, k - 1), At(pts, k), At(pts, k + 1), At(pts, k + 2), t);
                cam.transform.position = Spline(_tour);
                cam.transform.LookAt(Spline(_tourLook));
            }
            else cam.transform.rotation = Quaternion.Euler(8f, Time.unscaledTime * 3f, 0f);
        }

        const float TourSegmentSeconds = 8f;

        /// <summary>
        /// The title camera's loop: the islands sorted by their direction round the group's centre; for each, a close view from its outer side
        /// (just above its ground, looking at it), then a wide, high view of the whole group from between it and the next. Every point lies
        /// outside the group, so the spline through them never cuts through an island. The one backdrop islet sits below the middle island.
        /// </summary>
        void SetupTour(System.Collections.Generic.List<Isle> isles)
        {
            var mid = Vector3.zero; foreach (var i in isles) mid += i.centre; mid /= isles.Count;
            float reach = 0f; foreach (var i in isles) { var d = i.centre - mid; d.y = 0f; reach = Mathf.Max(reach, d.magnitude + i.radius); }
            float Dir(Isle i) => Mathf.Atan2(i.centre.z - mid.z, i.centre.x - mid.x);
            var order = new System.Collections.Generic.List<Isle>(isles); order.Sort((a, b) => Dir(a).CompareTo(Dir(b)));
            var pos = new System.Collections.Generic.List<Vector3>(); var look = new System.Collections.Generic.List<Vector3>();
            for (int k = 0; k < order.Count; k++)
            {
                var i = order[k]; float a = Dir(i), b = Dir(order[(k + 1) % order.Count]); if (b <= a) b += Mathf.PI * 2f;
                var outward = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                pos.Add(i.centre + outward * (i.radius + 17f) + Vector3.up * 7f); look.Add(i.centre + Vector3.up * 2f);   // close on this island
                float w = (a + b) * 0.5f;
                pos.Add(mid + new Vector3(Mathf.Cos(w), 0f, Mathf.Sin(w)) * (reach + 16f) + Vector3.up * 20f); look.Add(mid);   // wide over the group
            }
            _tour = pos.ToArray(); _tourLook = look.ToArray();

            var hub = isles[isles.Count >= 3 ? 1 : 0];
            var group = GameObject.Find("Islets"); if (group == null || group.transform.childCount == 0) return;
            var away = hub.centre - mid; away.y = 0f; if (away.sqrMagnitude < 1f) away = Vector3.right;
            var p = hub.centre + Quaternion.Euler(0f, 30f, 0f) * away.normalized * (hub.radius + 11f) + Vector3.down * 12f;
            var islet = group.transform.GetChild(0);
            var bob = islet.GetComponent<FloatBob>(); if (bob != null) bob.MoveBase(p); else islet.position = p;
        }

        static Vector3 CatmullRom(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t) =>
            0.5f * (2f * b + (c - a) * t + (2f * a - 5f * b + 4f * c - d) * (t * t) + (3f * b - a - 3f * c + d) * (t * t * t));

        /// <summary>The title world plants and waters everything one by one, so the plants grow in front of the menu.</summary>
        System.Collections.IEnumerator GrowShowcase()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            foreach (var soil in FindObjectsByType<SandboxTarget>())
            {
                var item = Kit.Item(soil.acceptedItemId);
                if (item == null) continue;
                soil.UseItem(item, null);
                yield return new WaitForSecondsRealtime(0.8f);
                soil.Water(null);
                yield return new WaitForSecondsRealtime(2.2f);
            }
        }

        // ------------------------------------------------------------------ actions
        void Show(GameObject screen)
        {
            foreach (var g in new[] { _home, _levels, _options, _generate }) g.SetActive(g == screen);
            RefreshLevels();
            if (screen == _generate) OnCodeChanged(_codeField.text);
        }

        void NewRandomSeed() { _randomSeed = (int)(System.DateTime.UtcNow.Ticks & SeedCode.SeedMask); RefreshRandom(); }

        RunConfig RandomConfig()
        {
            int n = DifficultyIslands[_difficulty];
            return new RunConfig
            {
                seed = _randomSeed, islandCount = n, levelId = null, waterMode = WaterMode.Sources,
                allowedKinds = _difficulty == 0 ? new[] { PlantKind.Bridge, PlantKind.Ladder } : new[] { PlantKind.Bridge, PlantKind.Ladder, PlantKind.Platform },
            };
        }

        void StartRun(RunConfig c)
        {
            GameBootstrap.PendingRun = c;
            if (string.IsNullOrEmpty(c.levelId)) SaveService.SetLastSeedCode(SeedCode.Encode(c));
            PlayerHud.SkipMenuOnce();
            SceneManager.LoadScene(gameScene);
        }

        void OnCodeChanged(string text)
        {
            // codes are lowercase words joined by dashes: typed or pasted spaces become dashes
            if (text != null && (text.IndexOf(' ') >= 0 || text != text.ToLowerInvariant()))
            {
                string n = text.ToLowerInvariant().Replace(' ', '-');
                if (n != text) { _codeField.text = n; return; }
            }
            _codeValid = SeedCode.TryDecode(text, out _codeConfig, out string msg);
            if (string.IsNullOrWhiteSpace(text)) _codeStatus.text = "Five words joined by dashes, like " + SeedCode.Encode(RandomConfig());
            else if (!_codeValid) _codeStatus.text = msg;
            else _codeStatus.text = (msg.Length > 0 ? msg + "\n" : "") + $"{_codeConfig.islandCount} islands, {_codeConfig.allowedKinds.Length} plant kinds - ready.";
            _codeStatus.color = _codeValid ? (msg.Length > 0 ? Palette.TextOrange : Palette.LeafGreen) : new Color(1f, 0.45f, 0.35f);
        }

        void RefreshRandom()
        {
            if (_randomCode == null) return;
            _randomCode.text = SeedCode.Encode(RandomConfig());
            _diffLabel.text = "Difficulty: " + DifficultyNames[_difficulty] + " (" + DifficultyIslands[_difficulty] + " islands)";
        }

        void RefreshLevels()
        {
            var levels = Kit.Asset != null ? Kit.Asset.levels : null;
            if (levels == null) return;
            for (int i = 0; i < _cardInfo.Length && i < levels.Length; i++)
            {
                var l = levels[i];
                string world = l.theme == LevelTheme.Sunny ? "Sunny" : l.theme == LevelTheme.Rain ? "Rain" : l.theme == LevelTheme.Snow ? "Snow" : l.theme == LevelTheme.Fog ? "Dense fog" : l.theme == LevelTheme.Sunset ? "Sunset" : l.theme == LevelTheme.Sunrise ? "Sunrise" : "Autumn";
                _cardInfo[i].text = l.islandCount + " islands\nGoal: " + GoalName(l.goal) + "\nWorld: " + world + (l.wind ? ", windy" : "") + "\n" + BestLines(l.id);
            }
            RefreshRandom();
        }

        /// <summary>The best time of a level, or -- when it was not finished yet.</summary>
        string BestLines(string key) { var b = SaveService.BestTime(key); return "Best time: " + (b.HasValue ? Fmt(b.Value) : "--"); }

        static string GoalName(GoalKind g) => g == GoalKind.Fruit ? "golden fruit" : g == GoalKind.Gate ? "gate" : g == GoalKind.Fire ? "campfire" : "flag";

        static string Fmt(float t) { int s = (int)t; return (s / 60).ToString("00") + ":" + (s % 60).ToString("00"); }

        // ------------------------------------------------------------------ build
        void Build()
        {
            var cgo = new GameObject("TitleUI", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = UiLayout.MatchHeight;
            cgo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)cgo.transform;

            var levels = Kit.Asset != null ? Kit.Asset.levels : new LevelDefinition[0];
            _home = Screen("Home", root, 0.15f);
            if (UiLayout.WideTouch)
            {
                // phones are wide and short: one column of five buttons on the right over a translucent panel, the logo low on the left so the islands stay visible
                float cw = 1080f * UnityEngine.Screen.width / Mathf.Max(1, UnityEngine.Screen.height), colX = cw / 2f - 300f;
                Panel(_home.transform, new Vector2(colX, 0), new Vector2(520, 720), new Color(0.04f, 0.09f, 0.14f, 0.6f));
                Title(_home.transform, new Vector2(-280, -230), 0.8f);
                Label(_home.transform, "Grow plants to cross the floating islands.", 30, new Vector2(-280, -340), 880, Color.white);
                Accent(Button(_home.transform, "Play", new Vector2(colX, 260), () => { if (levels.Length > 0) StartRun(levels[0].ToConfig()); }, 380));
                Button(_home.transform, "Select level", new Vector2(colX, 130), () => Show(_levels), 380);
                Button(_home.transform, "Generate level", new Vector2(colX, 0), () => Show(_generate), 380);
                Button(_home.transform, "Options", new Vector2(colX, -130), () => Show(_options), 380);
                Danger(Button(_home.transform, "Quit", new Vector2(colX, -260), Quit, 380));
            }
            else
            {
            Title(_home.transform, new Vector2(0, 340));
            Label(_home.transform, "Grow plants to cross the floating islands.", 36, new Vector2(0, 230), 1500, Color.white);
            Accent(Button(_home.transform, "Play", new Vector2(0, 50), () => { if (levels.Length > 0) StartRun(levels[0].ToConfig()); }));
            Button(_home.transform, "Select level", new Vector2(0, -65), () => Show(_levels));
            Button(_home.transform, "Generate level", new Vector2(0, -160), () => Show(_generate));
            Button(_home.transform, "Options", new Vector2(0, -255), () => Show(_options));
            Danger(Button(_home.transform, "Quit", new Vector2(0, -350), Quit));
            }

            _options = Screen("Options", root, 0.85f);
            Label(_options.transform, "Options", 80, new Vector2(0, 380), 1000, Palette.TextOrange);
            // two columns: toggles on the left, sliders on the right; camera follow, glow and the FPS counter sit below the sliders.
            // Touch has no music slider and no camera toggle, so the right column moves up into their rows and Reset / Back follow
            var o = _options.transform;
            bool tc = TouchInput.Active;
            _musicToggle = Button(o, "Music", new Vector2(-330, 270), () => { GameSettings.Music = !GameSettings.Music; RefreshOptions(); }, 420, 34);
            if (!TouchInput.Active)   // phones: only the system volume works
            UiKit.Slider(o, _font, "Music volume", new Vector2(330, 270), 420, GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
            _soundToggle = Button(o, "Sound", new Vector2(-330, 160), () => { GameSettings.Sound = !GameSettings.Sound; RefreshOptions(); }, 420, 34);
            UiKit.Slider(o, _font, "Sound volume", new Vector2(330, tc ? 270 : 160), 420, GameSettings.SoundVolume, v => { GameSettings.SoundVolume = v; Audio.AudioManager.PlaySfx("ui_click"); });
            _autoWaterToggle = Button(o, "Auto water", new Vector2(-330, 50), () => { GameSettings.AutoWater = !GameSettings.AutoWater; RefreshOptions(); }, 420, 34);
            _followToggle = Button(o, "Camera", new Vector2(330, 50), () => { GameSettings.CameraFollow = !GameSettings.CameraFollow; RefreshOptions(); }, 420, 34);
            if (tc) _followToggle.transform.parent.gameObject.SetActive(false);   // touch: the camera always follows
            _hintsToggle = Button(o, "Hints", new Vector2(-330, -60), () => { GameSettings.Hints = !GameSettings.Hints; RefreshOptions(); }, 420, 34);
            _glowToggle = Button(o, "Postprocessing", new Vector2(330, tc ? 160 : -60), () => { GameSettings.Bloom = !GameSettings.Bloom; RefreshOptions(); }, 420, 34);
            _fpsToggle = Button(o, "FPS", new Vector2(330, tc ? 50 : -170), () => { GameSettings.Fps = !GameSettings.Fps; RefreshOptions(); }, 420, 34);
            if (tc) _fps30Toggle = Button(o, "Fps30", new Vector2(330, -60), () => { GameSettings.Fps30 = !GameSettings.Fps30; RefreshOptions(); }, 420, 34);
            _qualityToggle = Button(o, "Quality", new Vector2(-330, -170), () => { GameSettings.Quality = (GameSettings.Quality + 1) % 3; RefreshOptions(); }, 420, 34);   // T63: Low / Medium / High
            Button(o, "Reset all best times", new Vector2(0, -280), () => { SaveService.ResetTimes(); RefreshLevels(); _resetNote.text = "All best times cleared."; }, 520, 30);
            _resetNote = Label(o, "", 26, new Vector2(0, -335), 800, Palette.TextOrange);
            Button(o, "Back", new Vector2(0, -400), () => { _resetNote.text = ""; Show(_home); }, 300, 34);
            RefreshOptions();

            // select level: the curated levels with best times, plus the test scenes
            _levels = Screen("Levels", root, 0.82f);
            Label(_levels.transform, "Select level", 70, new Vector2(0, 450), 1200, Palette.TextOrange);
            _cardInfo = new Text[levels.Length];
            int perRow = levels.Length > 6 ? (levels.Length + 1) / 2 : Mathf.Max(1, levels.Length);   // T50: ten levels go in two rows of five
            bool two = levels.Length > 6;
            for (int i = 0; i < levels.Length; i++)
            {
                var l = levels[i];
                int row = i / perRow, col = i % perRow, inRow = Mathf.Min(perRow, levels.Length - row * perRow);
                // tc (touch): taller cards with bigger text (the screen has the vertical room)
                float x = (col - (inRow - 1) * 0.5f) * 314f, y = two ? (tc ? (row == 0 ? 185f : -235f) : (row == 0 ? 155f : -175f)) : 130f, h = two ? (tc ? 380f : 270f) : 330f;
                var card = Panel(_levels.transform, new Vector2(x, y), new Vector2(304, h), new Color(0.07f, 0.16f, 0.25f, 0.9f));
                Label(card, l.title, two ? (tc ? 24 : 21) : 23, new Vector2(0, h / 2 - (tc ? 55 : 35)), 290, Palette.TextOrange);
                _cardInfo[i] = Label(card, "", two ? (tc ? 19 : 16) : 19, new Vector2(0, two ? 8 : 12), 280, Color.white);
                _cardInfo[i].alignment = TextAnchor.MiddleCenter; _cardInfo[i].lineSpacing = 1.6f;
                ((RectTransform)_cardInfo[i].transform).sizeDelta = new Vector2(280, h - (tc ? 140 : 100));
                Button(card, "Play", new Vector2(0, -h / 2 + (tc ? 52 : 32)), () => StartRun(l.ToConfig()), 150, 24, 50f);
            }
            // asset / mechanics test scenes (not runs: no timer, no best times)
            float testY = two ? (TouchInput.Active ? -470f : -345f) : -150f;
            for (int i = 0; i < TestScenes.Length; i++)
            {
                string scene = TestScenes[i].scene;
                if (!Application.CanStreamedLevelBeLoaded(scene)) continue;
                var tb = Button(_levels.transform, TestScenes[i].label, new Vector2(i * 360, testY - 20f), () => { Time.timeScale = 1f; SceneManager.LoadScene(scene); }, 340, 26);
                tb.transform.parent.GetComponent<Image>().color = ExploreGreen;
            }
            Button(_levels.transform, "Back", new Vector2(two ? 630 : 0, two ? 440 : -420), () => Show(_home), 300, 34);   // right edge in line with the cards (a 4:3 tablet is only ~1660 wide)

            // generate level: random seed, difficulty, shows the code before starting
            _generate = Screen("Generate", root, 0.82f);
            Label(_generate.transform, "Generate level", 70, new Vector2(0, 450), 1200, Palette.TextOrange);
            _difficulty = GameSettings.Difficulty;   // the last generated level's
            bool gt = TouchInput.Active;   // phones: wider buttons (x1.15), so more room between them
            float gx = gt ? 280f : 215f, gy1 = gt ? -15f : -25f, gy2 = gt ? -125f : -115f;   // buttons inside the random panel
            var rnd = Panel(_generate.transform, new Vector2(0, 215), new Vector2(gt ? 1200 : 900, 330), new Color(0.07f, 0.16f, 0.25f, 0.9f));
            Label(rnd, "Your seed", 34, new Vector2(0, 125), 800, Palette.TextOrange);
            _randomCode = Label(rnd, "", 40, new Vector2(0, 62), 860, Color.white);
            _diffLabel = Button(rnd, "Difficulty", new Vector2(-gx, gy1), () => { _difficulty = (_difficulty + 1) % 3; GameSettings.Difficulty = _difficulty; RefreshRandom(); }, 420, 26);
            Button(rnd, "New seed", new Vector2(gx, gy1), NewRandomSeed, 420, 30);
            Button(rnd, "Play", new Vector2(-gx, gy2), () => StartRun(RandomConfig()), 420, 34);
            Button(rnd, "Copy code", new Vector2(gx, gy2), () => GUIUtility.systemCopyBuffer = SeedCode.Encode(RandomConfig()), 420, 30);
            // input seed: type or paste a code below the random one
            var code = Panel(_generate.transform, new Vector2(0, -150), new Vector2(gt ? 1200 : 900, 340), new Color(0.07f, 0.16f, 0.25f, 0.9f));
            Label(code, "Or type / paste a seed code", 34, new Vector2(0, 125), 860, Palette.TextOrange);
            _codeField = InputBox(code, new Vector2(-150, 50), new Vector2(gt ? 800 : 560, 64));
            Button(code, "Paste", new Vector2(gt ? 400 : 305, 50), () => { _codeField.text = GUIUtility.systemCopyBuffer; }, 220, 28);
            _codeStatus = Label(code, "", 26, new Vector2(0, -35), 860, Color.white);
            ((RectTransform)_codeStatus.transform).sizeDelta = new Vector2(860, 70);
            Button(code, "Play code", new Vector2(0, -115), () => { if (_codeValid) StartRun(_codeConfig); }, 420, 34);
            _codeField.onValueChanged.AddListener(OnCodeChanged);
            OnCodeChanged("");
            Button(_generate.transform, "Back", new Vector2(0, -400), () => Show(_home), 300, 34);
        }

        /// <summary>"Overgrown" in leaf green; the O is a ring wrapped in a vine with leaves and a curling tendril.</summary>
        void Title(Transform parent, Vector2 pos, float scale = 1f)
        {
            const int font = 170; const float oSize = 185f;
            var holder = Rect("Title", parent, new Vector2(1400, 220), pos); holder.localScale = Vector3.one * scale;
            var rest = Label(holder, "vergrown", font, Vector2.zero, 1000, Palette.LeafGreen);
            rest.alignment = TextAnchor.MiddleLeft;
            float pw = rest.preferredWidth + 10f, total = oSize * 0.8f + pw;
            var rrt = (RectTransform)rest.transform; rrt.pivot = new Vector2(0f, 0.5f); rrt.sizeDelta = new Vector2(pw, font * 1.4f);
            float left = -total / 2f;
            rrt.anchoredPosition = new Vector2(left + oSize * 0.8f, 0f);
            var ort = Rect("VineO", holder, new Vector2(oSize, oSize), new Vector2(left + oSize * 0.5f, font * 0.05f));
            var im = ort.gameObject.AddComponent<Image>(); im.sprite = VineO(); im.raycastTarget = false;
            var sh = ort.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.5f); sh.effectDistance = new Vector2(3, -3);
        }

        static Sprite _vineO;
        public static Sprite VineO()
        {
            if (_vineO != null) return _vineO;
            const int S = 384; const float aa = 1.5f / S, ow = 0.009f;
            var dark = new Color(0.07f, 0.24f, 0.14f);
            var px = new Color[S * S];
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S - 0.5f, v = (y + 0.5f) / S - 0.5f;
                float r = Mathf.Sqrt(u * u + v * v), th = Mathf.Atan2(v, u);
                var c = new Color(0, 0, 0, 0);   // straight alpha, composited shape by shape
                void Shape(float dist, Color fill)
                {
                    c = Over(c, dark, Mathf.Clamp01(0.5f - (dist - ow) / aa));
                    c = Over(c, fill, Mathf.Clamp01(0.5f - dist / aa));
                }
                Shape(Mathf.Abs(r - 0.30f) - 0.075f, Color.Lerp(new Color(0.18f, 0.49f, 0.31f), new Color(0.36f, 0.71f, 0.42f), 0.5f + 0.5f * Mathf.Sin(th + 1f)));
                Shape(Mathf.Abs(r - (0.30f + 0.07f * Mathf.Sin(7f * th))) - 0.016f, new Color(0.66f, 0.93f, 0.48f));
                for (int k = 0; k < 6; k++)
                {
                    float a = (k * 60f + 25f) * Mathf.Deg2Rad; int side = k % 2 == 0 ? 1 : -1;
                    var lc = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (0.30f + side * 0.10f);
                    float la = a + side * 0.9f; var dir = new Vector2(Mathf.Cos(la), Mathf.Sin(la)); var perp = new Vector2(-dir.y, dir.x);
                    var q = new Vector2(u, v) - lc; float along = Vector2.Dot(q, dir), across = Vector2.Dot(q, perp);
                    float e = Mathf.Sqrt(along * along / (0.078f * 0.078f) + across * across / (0.034f * 0.034f));
                    Shape((e - 1f) * 0.034f, new Color(0.42f, 0.86f, 0.46f));
                }
                if (u > 0.1f && v > 0.1f)   // tendril: a thin spiral curling out of the top right
                {
                    float best = 9f;
                    for (int i = 0; i <= 40; i++)
                    {
                        float t = i / 40f, phi = 2f * Mathf.PI * 1.25f * t + 0.6f, rho = 0.085f * (1f - t * 0.85f);
                        var pt = new Vector2(0.34f, 0.36f) + rho * new Vector2(Mathf.Cos(phi), Mathf.Sin(phi));
                        best = Mathf.Min(best, Vector2.Distance(new Vector2(u, v), pt));
                    }
                    Shape(best - 0.011f, new Color(0.66f, 0.93f, 0.48f));
                }
                px[y * S + x] = c;
            }
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels(px); tex.Apply();
            _vineO = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100);
            _vineO.hideFlags = HideFlags.HideAndDontSave;
            return _vineO;
        }

        static Color Over(Color dst, Color src, float a)
        {
            float oa = a + dst.a * (1f - a);
            if (oa <= 0f) return new Color(0, 0, 0, 0);
            var rgb = (new Vector3(src.r, src.g, src.b) * a + new Vector3(dst.r, dst.g, dst.b) * dst.a * (1f - a)) / oa;
            return new Color(rgb.x, rgb.y, rgb.z, oa);
        }


        GameObject Screen(string name, RectTransform root, float dim)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform; rt.SetParent(root, false);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var im = go.AddComponent<Image>(); im.color = new Color(0.04f, 0.09f, 0.14f, dim); im.raycastTarget = false;
            return go;
        }

        Transform Panel(Transform parent, Vector2 pos, Vector2 size, Color c)
        {
            var rt = Rect("Panel", parent, size, pos);
            var im = rt.gameObject.AddComponent<Image>(); im.sprite = Rounded(); im.type = Image.Type.Sliced; im.color = c; im.raycastTarget = false;
            return rt;
        }

        RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform; rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = pos;
            return rt;
        }

        Text Label(Transform parent, string text, int size, Vector2 pos, float width, Color color)
        {
            var rt = Rect("Label", parent, new Vector2(width, UiLayout.Font(size, true) * 1.4f), pos);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font; t.text = text; UiLayout.Fit(t, size, true); t.alignment = TextAnchor.MiddleCenter; t.color = color; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var sh = rt.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.6f); sh.effectDistance = new Vector2(2, -2);
            return t;
        }

        Text Button(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction click, float width = 360f, int fontSize = 38, float height = 70f)
        {
            if (TouchInput.Active) { width *= 1.15f; height = Mathf.Max(height * 1.25f, 64f); }   // phones: bigger touch targets
            var rt = Rect(label + "Button", parent, new Vector2(width, height), pos);
            var im = rt.gameObject.AddComponent<Image>(); im.sprite = Pill(); im.type = Image.Type.Sliced; im.color = new Color(0.17f, 0.37f, 0.56f, 0.95f);
            var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = im; b.onClick.AddListener(click);
            var cb = b.colors; cb.highlightedColor = new Color(0.49f, 0.95f, 0.9f, 1f); cb.pressedColor = Palette.SkyBlue; b.colors = cb;
            var t = Label(rt, label, fontSize, Vector2.zero, width, Palette.TextOrange);
            if (height < 70f || TouchInput.Active) ((RectTransform)t.transform).sizeDelta = new Vector2(width, height);   // compact button: the label box follows it
            return t;
        }

        /// <summary>The main call to action: a purple button with white text, 20 percent bigger (Play on the home screen).</summary>
        Text Accent(Text label)
        {
            var im = label.transform.parent.GetComponent<Image>();
            im.color = new Color(0.46f, 0.30f, 0.84f, 0.97f);
            var b = im.GetComponent<UnityEngine.UI.Button>(); var cb = b.colors; cb.highlightedColor = new Color(0.68f, 0.52f, 1f, 1f); cb.pressedColor = new Color(0.33f, 0.2f, 0.62f, 1f); b.colors = cb;
            label.color = Color.white;
            im.rectTransform.localScale = Vector3.one * 1.2f;   // a little bigger than the other buttons
            return label;
        }

        /// <summary>Quit: a muted red button, a bit smaller than the rest, so it reads as "leaves the game".</summary>
        public static Text Danger(Text label)
        {
            var im = label.transform.parent.GetComponent<Image>();
            im.color = new Color(0.62f, 0.2f, 0.24f, 0.97f);
            var b = im.GetComponent<UnityEngine.UI.Button>(); var cb = b.colors; cb.highlightedColor = new Color(0.95f, 0.4f, 0.42f, 1f); cb.pressedColor = new Color(0.4f, 0.12f, 0.16f, 1f); b.colors = cb;
            label.color = Color.white;
            im.rectTransform.localScale = Vector3.one * 0.85f;   // a little smaller than the other buttons
            return label;
        }

        InputField InputBox(Transform parent, Vector2 pos, Vector2 size)
        {
            var rt = Rect("CodeField", parent, size, pos);
            var im = rt.gameObject.AddComponent<Image>(); im.sprite = Rounded(); im.type = Image.Type.Sliced; im.color = new Color(0.02f, 0.05f, 0.09f, 0.95f);
            var text = Label(rt, "", 32, Vector2.zero, size.x - 24, Color.white);
            text.raycastTarget = true;
            var ph = Label(rt, "word-word-word-word-word", 32, Vector2.zero, size.x - 24, new Color(1, 1, 1, 0.35f));
            var f = rt.gameObject.AddComponent<InputField>();
            f.targetGraphic = im; f.textComponent = text; f.placeholder = ph; f.characterLimit = 80;
            f.lineType = InputField.LineType.SingleLine; f.keyboardType = TouchScreenKeyboardType.ASCIICapable; f.inputType = InputField.InputType.Standard;   // soft keyboard on phones
            return f;
        }

        Sprite Rounded() => _square != null ? _square : _square = RoundedSprite(32, 8);

        /// <summary>Buttons: strongly rounded corners (a pill on the usual 70 px height).</summary>
        Sprite Pill() => _pill != null ? _pill : _pill = RoundedSprite(64, 28);
        Sprite _pill;

        static Sprite RoundedSprite(int S, int R)
        {
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float dx = Mathf.Max(R - x, x - (S - 1 - R), 0), dy = Mathf.Max(R - y, y - (S - 1 - R), 0);
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(R + 0.5f - Mathf.Sqrt(dx * dx + dy * dy))));
            }
            tex.Apply();
            var sp = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(R, R, R, R));
            sp.hideFlags = HideFlags.HideAndDontSave;
            return sp;
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
