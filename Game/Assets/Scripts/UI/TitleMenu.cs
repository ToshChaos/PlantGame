using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PlantGame.Core;
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
        Text _musicToggle, _soundToggle, _hintsToggle;
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
            _hintsToggle.text = "Key hints: " + (GameSettings.Hints ? "On" : "Off");
        }

        void Update()
        {
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
            foreach (var m in _modeLabels) m.text = "Mode: " + _mode + (_mode == GameMode.Challenge ? " (countdown)" : " (relaxed)");
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

        static string Fmt(float t) { int m = (int)(t / 60f); return m.ToString("00") + ":" + (t - m * 60f).ToString("00.0", System.Globalization.CultureInfo.InvariantCulture); }

        // ------------------------------------------------------------------ build
        void Build()
        {
            var cgo = new GameObject("TitleUI", typeof(RectTransform));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)cgo.transform;

            var levels = Kit.Asset != null ? Kit.Asset.levels : new LevelDefinition[0];
            _home = Screen("Home", root, 0.15f);
            Label(_home.transform, "Overgrown", 130, new Vector2(0, 340), 1400, Palette.TextOrange);
            Label(_home.transform, "Grow plants to cross the floating islands.", 36, new Vector2(0, 230), 1500, Color.white);
            ModeButton(_home.transform, new Vector2(0, 130));
            Button(_home.transform, "Play", new Vector2(0, 30), () => { if (levels.Length > 0) StartRun(levels[0].ToConfig(_mode)); });
            Button(_home.transform, "Select level", new Vector2(0, -65), () => Show(_levels));
            Button(_home.transform, "Generate level", new Vector2(0, -160), () => Show(_generate));
            Button(_home.transform, "Input seed", new Vector2(0, -255), () => Show(_seed));
            Button(_home.transform, "Options", new Vector2(0, -350), () => Show(_options));
            Button(_home.transform, "Quit", new Vector2(0, -445), Quit);

            _options = Screen("Options", root, 0.85f);
            Label(_options.transform, "Options", 80, new Vector2(0, 380), 1000, Palette.TextOrange);
            _musicToggle = Button(_options.transform, "Music", new Vector2(-330, 200), () => { GameSettings.Music = !GameSettings.Music; RefreshOptions(); }, 420, 34);
            UiKit.Slider(_options.transform, _font, "Music volume", new Vector2(330, 200), 420, GameSettings.MusicVolume, v => GameSettings.MusicVolume = v);
            _soundToggle = Button(_options.transform, "Sound", new Vector2(-330, 60), () => { GameSettings.Sound = !GameSettings.Sound; RefreshOptions(); }, 420, 34);
            UiKit.Slider(_options.transform, _font, "Sound volume", new Vector2(330, 60), 420, GameSettings.SoundVolume, v => { GameSettings.SoundVolume = v; Audio.AudioManager.PlaySfx("ui_click"); });
            _hintsToggle = Button(_options.transform, "Hints", new Vector2(0, -90), () => { GameSettings.Hints = !GameSettings.Hints; RefreshOptions(); }, 420, 34);
            Button(_options.transform, "Reset all best times", new Vector2(0, -190), () => { SaveService.ResetTimes(); RefreshLevels(); _resetNote.text = "All best times cleared."; }, 520, 30);
            _resetNote = Label(_options.transform, "", 26, new Vector2(0, -255), 800, Palette.TextOrange);
            Button(_options.transform, "Back", new Vector2(0, -340), () => { _resetNote.text = ""; Show(_home); }, 300, 34);
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
                float x = (col - (inRow - 1) * 0.5f) * 314f, y = two ? (row == 0 ? 180f : -150f) : 110f, h = two ? 310f : 370f;
                var card = Panel(_levels.transform, new Vector2(x, y), new Vector2(304, h), new Color(0.07f, 0.16f, 0.25f, 0.9f));
                Label(card, l.title, two ? 21 : 23, new Vector2(0, h / 2 - 35), 290, Palette.TextOrange);
                _cardInfo[i] = Label(card, "", two ? 16 : 19, new Vector2(0, two ? 5 : 10), 280, Color.white);
                _cardInfo[i].alignment = TextAnchor.MiddleCenter;
                ((RectTransform)_cardInfo[i].transform).sizeDelta = new Vector2(280, h - 90);
                Button(card, "Play", new Vector2(0, -h / 2 + 41), () => StartRun(l.ToConfig(_mode)), 200, 30);
            }
            // asset / mechanics test scenes (not runs: no timer, no best times)
            float testY = two ? -340f : -170f;
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

        InputField InputBox(Transform parent, Vector2 pos, Vector2 size)
        {
            var rt = Rect("CodeField", parent, size, pos);
            var im = rt.gameObject.AddComponent<Image>(); im.sprite = Rounded(); im.type = Image.Type.Sliced; im.color = new Color(0.02f, 0.05f, 0.09f, 0.95f);
            var text = Label(rt, "", 32, Vector2.zero, size.x - 24, Color.white);
            text.raycastTarget = true;
            var ph = Label(rt, "word-word-word-word-word", 32, Vector2.zero, size.x - 24, new Color(1, 1, 1, 0.35f));
            var f = rt.gameObject.AddComponent<InputField>();
            f.targetGraphic = im; f.textComponent = text; f.placeholder = ph; f.characterLimit = 80;
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
