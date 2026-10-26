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
    /// a Random Level block (difficulty, its seed code is shown before you start and can be copied), an Enter Seed Code field with live
    /// validation, and the Exploration / Challenge toggle. Start puts the RunConfig in GameBootstrap.PendingRun and loads Main.
    /// </summary>
    public class TitleMenu : MonoBehaviour
    {
        public string gameScene = "Main";

        Font _font;
        WorldGenerator _showcase;
        GameObject _home, _levels, _options, _generate, _seed;
        Text _musicToggle, _soundToggle, _hintsToggle, _fpsToggle, _followToggle, _glowToggle;
        readonly System.Collections.Generic.List<Text> _modeLabels = new System.Collections.Generic.List<Text>();
        Text _diffLabel, _randomCode, _codeStatus, _resetNote;
        InputField _codeField;
        Text[] _cardInfo;
        GameMode _mode = GameMode.Exploration;
        int _difficulty = 1;   // 0 easy (4 islands), 1 normal (6), 2 hard (8)
        int _randomSeed;
        RunConfig _codeConfig; bool _codeValid;
        Sprite _square;

        static readonly (string label, string scene)[] TestScenes =
        {
            ("Movement course", "Sandbox_Gameplay"),
        };
        static readonly string[] DifficultyNames = { "Easy", "Normal", "Hard" };
        static readonly int[] DifficultyIslands = { 4, 6, 8 };

        void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (FindAnyObjectByType<EventSystem>() == null) { var es = new GameObject("EventSystem"); es.AddComponent<EventSystem>(); es.AddComponent<InputSystemUIInputModule>(); }
            _mode = GameBootstrap.Current.mode;
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
            _followToggle.text = "Camera follows: " + (GameSettings.CameraFollow ? "On" : "Off");
            _glowToggle.text = "Glow: " + (GameSettings.Bloom ? "On" : "Off");
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
                // slow orbit around all showcase islands, far enough out that the whole chain stays in frame
                var isles = _showcase.Layout.isles; var mid = Vector3.zero;
                foreach (var i in isles) mid += i.centre;
                mid /= isles.Count;
                float spread = 0f; foreach (var i in isles) spread = Mathf.Max(spread, Vector3.Distance(i.centre, mid));
                var centre = mid + Vector3.up * 2.5f;
                float ang = Time.unscaledTime * 0.06f, dist = spread + 22f;
                cam.transform.position = centre + new Vector3(Mathf.Cos(ang) * dist, 9f, Mathf.Sin(ang) * dist);
                cam.transform.LookAt(centre);
            }
            else cam.transform.rotation = Quaternion.Euler(8f, Time.unscaledTime * 3f, 0f);
        }

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
            foreach (var g in new[] { _home, _levels, _options, _generate, _seed }) g.SetActive(g == screen);
            RefreshLevels();
            if (screen == _seed) OnCodeChanged(_codeField.text);
        }

        void ToggleMode() { _mode = _mode == GameMode.Exploration ? GameMode.Challenge : GameMode.Exploration; RefreshLevels(); }

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
            c.mode = _mode;
            if (string.IsNullOrEmpty(c.levelId)) c.timeLimit = c.islandCount * 55f + 60f;
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
            foreach (var m in _modeLabels)
            {
                bool ch = _mode == GameMode.Challenge;
                m.text = ch ? "Mode: Time challenge" : "Mode: Exploration";
                m.color = ch ? Palette.DeepBlue : Palette.TextOrange;                                              // orange on green / deep blue on orange
                m.transform.parent.GetComponent<Image>().color = ch ? Palette.TextOrange : new Color(0.18f, 0.52f, 0.32f, 0.97f);
            }
            var levels = Kit.Asset != null ? Kit.Asset.levels : null;
            if (levels == null) return;
            for (int i = 0; i < _cardInfo.Length && i < levels.Length; i++)
            {
                var l = levels[i];
                _cardInfo[i].text = l.description + "\n\n" + l.islandCount + " islands" + (l.theme != LevelTheme.Sunny ? "  -  " + (l.theme == LevelTheme.Rain ? "Rain" : l.theme == LevelTheme.Snow ? "Snow" : l.theme == LevelTheme.Fog ? "Dense fog" : l.theme == LevelTheme.Sunset ? "Sunset" : l.theme == LevelTheme.Sunrise ? "Sunrise" : "Autumn") : "") + (l.wind ? "  -  Windy" : "") + "\nGoal: " + GoalName(l.goal) + "\n" +
                    (_mode == GameMode.Challenge ? "Time limit " + Fmt(l.challengeTimeLimitSeconds) + "\n" : "") +
                    BestLines(l.id);
            }
            RefreshRandom();
        }

        /// <summary>Both records of a level, each its own: relaxed (Exploration) and countdown (Challenge); the selected mode is marked.</summary>
        string BestLines(string key)
        {
            string Line(GameMode m, string label) { var b = SaveService.BestTime(key, m); return (m == _mode ? "> " : "  ") + label + "  " + (b.HasValue ? Fmt(b.Value) : "--"); }
            return Line(GameMode.Exploration, "Exploration best") + "\n" + Line(GameMode.Challenge, "Challenge best");
        }

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
            if (TouchInput.Active)
            {
                // phones are wide and short: the title and mode switch on the left, the six buttons in two columns on the right
                Title(_home.transform, new Vector2(-470, 150), 0.8f);
                Label(_home.transform, "Grow plants to cross the floating islands.", 30, new Vector2(-470, 20), 880, Color.white);
                ModeButton(_home.transform, new Vector2(-470, -110));
                Accent(Button(_home.transform, "Play", new Vector2(150, 140), () => { if (levels.Length > 0) StartRun(levels[0].ToConfig(_mode)); }, 380));
                Button(_home.transform, "Select level", new Vector2(150, 20), () => Show(_levels), 380);
                Button(_home.transform, "Generate level", new Vector2(740, 140), () => Show(_generate), 380);
                Button(_home.transform, "Input seed", new Vector2(740, 20), () => Show(_seed), 380);
                Button(_home.transform, "Options", new Vector2(150, -100), () => Show(_options), 380);
                Danger(Button(_home.transform, "Quit", new Vector2(740, -100), Quit, 380));
            }
            else
            {
            Title(_home.transform, new Vector2(0, 340));
            Label(_home.transform, "Grow plants to cross the floating islands.", 36, new Vector2(0, 230), 1500, Color.white);
            ModeButton(_home.transform, new Vector2(0, 130));
            Accent(Button(_home.transform, "Play", new Vector2(0, 30), () => { if (levels.Length > 0) StartRun(levels[0].ToConfig(_mode)); }));
            Button(_home.transform, "Select level", new Vector2(0, -65), () => Show(_levels));
            Button(_home.transform, "Generate level", new Vector2(0, -160), () => Show(_generate));
            Button(_home.transform, "Input seed", new Vector2(0, -255), () => Show(_seed));
            Button(_home.transform, "Options", new Vector2(0, -350), () => Show(_options));
            Danger(Button(_home.transform, "Quit", new Vector2(0, -445), Quit));
            }

            _options = Screen("Options", root, 0.85f);
            Label(_options.transform, "Options", 80, new Vector2(0, 380), 1000, Palette.TextOrange);
            // two columns: toggles on the left, sliders on the right; camera follow and glow sit below the sliders
            var o = _options.transform;
            _musicToggle = Button(o, "Music", new Vector2(-330, 260), () => { GameSettings.Music = !GameSettings.Music; RefreshOptions(); }, 420, 34);
            UiKit.Slider(o, _font, "Music volume", new Vector2(330, 260), 420, GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
            _soundToggle = Button(o, "Sound", new Vector2(-330, 140), () => { GameSettings.Sound = !GameSettings.Sound; RefreshOptions(); }, 420, 34);
            UiKit.Slider(o, _font, "Sound volume", new Vector2(330, 140), 420, GameSettings.SoundVolume, v => { GameSettings.SoundVolume = v; Audio.AudioManager.PlaySfx("ui_click"); });
            _fpsToggle = Button(o, "FPS", new Vector2(-330, 20), () => { GameSettings.Fps = !GameSettings.Fps; RefreshOptions(); }, 420, 34);
            _followToggle = Button(o, "Camera", new Vector2(330, 20), () => { GameSettings.CameraFollow = !GameSettings.CameraFollow; RefreshOptions(); }, 420, 34);
            _hintsToggle = Button(o, "Hints", new Vector2(-330, -100), () => { GameSettings.Hints = !GameSettings.Hints; RefreshOptions(); }, 420, 34);
            _glowToggle = Button(o, "Glow", new Vector2(330, -100), () => { GameSettings.Bloom = !GameSettings.Bloom; RefreshOptions(); }, 420, 34);
            Button(o, "Reset all best times", new Vector2(0, -215), () => { SaveService.ResetTimes(); RefreshLevels(); _resetNote.text = "All best times cleared."; }, 520, 30);
            _resetNote = Label(o, "", 26, new Vector2(0, -280), 800, Palette.TextOrange);
            Button(o, "Back", new Vector2(0, -360), () => { _resetNote.text = ""; Show(_home); }, 300, 34);
            RefreshOptions();

            // select level: the curated levels with best times, plus the test scenes
            _levels = Screen("Levels", root, 0.82f);
            Label(_levels.transform, "Select level", 70, new Vector2(0, 450), 1200, Palette.TextOrange);
            ModeButton(_levels.transform, new Vector2(0, 360));
            _cardInfo = new Text[levels.Length];
            int perRow = levels.Length > 6 ? (levels.Length + 1) / 2 : Mathf.Max(1, levels.Length);   // T50: ten levels go in two rows of five
            bool two = levels.Length > 6;
            for (int i = 0; i < levels.Length; i++)
            {
                var l = levels[i];
                int row = i / perRow, col = i % perRow, inRow = Mathf.Min(perRow, levels.Length - row * perRow);
                float x = (col - (inRow - 1) * 0.5f) * 314f, y = two ? (row == 0 ? 135f : -195f) : 110f, h = two ? 310f : 370f;
                var card = Panel(_levels.transform, new Vector2(x, y), new Vector2(304, h), new Color(0.07f, 0.16f, 0.25f, 0.9f));
                Label(card, l.title, two ? 21 : 23, new Vector2(0, h / 2 - 35), 290, Palette.TextOrange);
                _cardInfo[i] = Label(card, "", two ? 16 : 19, new Vector2(0, two ? 5 : 10), 280, Color.white);
                _cardInfo[i].alignment = TextAnchor.MiddleCenter;
                ((RectTransform)_cardInfo[i].transform).sizeDelta = new Vector2(280, h - 90);
                Button(card, "Play", new Vector2(0, -h / 2 + 41), () => StartRun(l.ToConfig(_mode)), 200, 30);
            }
            // asset / mechanics test scenes (not runs: no timer, no best times)
            float testY = two ? -385f : -170f;
            Label(_levels.transform, "Test scenes", 30, new Vector2(0, testY), 400, Palette.TextOrange);
            for (int i = 0; i < TestScenes.Length; i++)
            {
                string scene = TestScenes[i].scene;
                if (!Application.CanStreamedLevelBeLoaded(scene)) continue;
                Button(_levels.transform, TestScenes[i].label, new Vector2(i * 360, testY - 75f), () => { Time.timeScale = 1f; SceneManager.LoadScene(scene); }, 340, 26);
            }
            Button(_levels.transform, "Back", new Vector2(two ? 760 : 0, two ? 440 : -420), () => Show(_home), 300, 34);

            // generate level: random seed, difficulty, shows the code before starting
            _generate = Screen("Generate", root, 0.82f);
            Label(_generate.transform, "Generate level", 70, new Vector2(0, 400), 1200, Palette.TextOrange);
            ModeButton(_generate.transform, new Vector2(0, 310));
            var rnd = Panel(_generate.transform, new Vector2(0, 60), new Vector2(900, 330), new Color(0.07f, 0.16f, 0.25f, 0.9f));
            Label(rnd, "Your seed", 34, new Vector2(0, 130), 800, Palette.TextOrange);
            _randomCode = Label(rnd, "", 40, new Vector2(0, 65), 860, Color.white);
            _diffLabel = Button(rnd, "Difficulty", new Vector2(-215, -25), () => { _difficulty = (_difficulty + 1) % 3; RefreshRandom(); }, 420, 26);
            Button(rnd, "New seed", new Vector2(215, -25), NewRandomSeed, 420, 30);
            Button(rnd, "Play", new Vector2(-215, -115), () => StartRun(RandomConfig()), 420, 34);
            Button(rnd, "Copy code", new Vector2(215, -115), () => GUIUtility.systemCopyBuffer = SeedCode.Encode(RandomConfig()), 420, 30);
            Button(_generate.transform, "Back", new Vector2(0, -250), () => Show(_home), 300, 34);

            // input seed
            _seed = Screen("Seed", root, 0.82f);
            Label(_seed.transform, "Input seed", 70, new Vector2(0, 400), 1200, Palette.TextOrange);
            ModeButton(_seed.transform, new Vector2(0, 310));
            var code = Panel(_seed.transform, new Vector2(0, 60), new Vector2(1000, 330), new Color(0.07f, 0.16f, 0.25f, 0.9f));
            Label(code, "Type or paste a seed code", 34, new Vector2(0, 130), 900, Palette.TextOrange);
            _codeField = InputBox(code, new Vector2(-170, 55), new Vector2(600, 64));
            Button(code, "Paste", new Vector2(280, 55), () => { _codeField.text = GUIUtility.systemCopyBuffer; }, 220, 28);
            _codeStatus = Label(code, "", 26, new Vector2(0, -35), 940, Color.white);
            ((RectTransform)_codeStatus.transform).sizeDelta = new Vector2(940, 80);
            Button(code, "Play code", new Vector2(0, -120), () => { if (_codeValid) StartRun(_codeConfig); }, 420, 34);
            _codeField.onValueChanged.AddListener(OnCodeChanged);
            OnCodeChanged("");
            Button(_seed.transform, "Back", new Vector2(0, -250), () => Show(_home), 300, 34);
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

        void ModeButton(Transform parent, Vector2 pos) { _modeLabels.Add(Button(parent, "Mode", pos, ToggleMode, 620, 32)); }

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
            var rt = Rect("Label", parent, new Vector2(width, size * 1.4f), pos);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = _font; t.text = text; t.fontSize = size; t.alignment = TextAnchor.MiddleCenter; t.color = color; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var sh = rt.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.6f); sh.effectDistance = new Vector2(2, -2);
            return t;
        }

        Text Button(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction click, float width = 360f, int fontSize = 38)
        {
            var rt = Rect(label + "Button", parent, new Vector2(width, 70), pos);
            var im = rt.gameObject.AddComponent<Image>(); im.sprite = Rounded(); im.type = Image.Type.Sliced; im.color = new Color(0.17f, 0.37f, 0.56f, 0.95f);
            var b = rt.gameObject.AddComponent<UnityEngine.UI.Button>(); b.targetGraphic = im; b.onClick.AddListener(click);
            var cb = b.colors; cb.highlightedColor = new Color(0.49f, 0.95f, 0.9f, 1f); cb.pressedColor = Palette.SkyBlue; b.colors = cb;
            var t = Label(rt, label, fontSize, Vector2.zero, width, Palette.TextOrange);
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

        /// <summary>Quit: a muted red button so it reads as "leaves the game".</summary>
        public static Text Danger(Text label)
        {
            var im = label.transform.parent.GetComponent<Image>();
            im.color = new Color(0.62f, 0.2f, 0.24f, 0.97f);
            var b = im.GetComponent<UnityEngine.UI.Button>(); var cb = b.colors; cb.highlightedColor = new Color(0.95f, 0.4f, 0.42f, 1f); cb.pressedColor = new Color(0.4f, 0.12f, 0.16f, 1f); b.colors = cb;
            label.color = Color.white;
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

        Sprite Rounded()
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
