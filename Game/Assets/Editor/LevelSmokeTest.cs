using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PlantGame.Core;
using PlantGame.Player;

namespace PlantGame.Editor
{
    /// <summary>
    /// T22: PlantGame > Smoke Test Levels. Plays each curated level (Exploration, then Challenge for level 1) in the Main scene for a few seconds
    /// and checks that a player, a goal and a run exist and the console stays free of errors. Result lines go to Logs/smoke.txt.
    /// State lives in EditorPrefs so it survives the domain reloads of entering play mode (SessionState was lost mid-run).
    /// </summary>
    [InitializeOnLoad]
    public static class LevelSmokeTest
    {
        const string K = "PlantGame.Smoke.Index", ReportPath = "Logs/smoke.txt";
        static int _frames, _errors, _idle; static string _firstError = "";

        static LevelSmokeTest()
        {
            if (EditorPrefs.GetInt(K, -1) < 0) return;
            Application.logMessageReceived += OnLog;
            EditorApplication.playModeStateChanged += OnState;
            EditorApplication.update += Tick;
            if (EditorApplication.isPlayingOrWillChangePlaymode) Arm();
        }

        [MenuItem("PlantGame/Smoke Test Levels")]
        static void Start()
        {
            File.WriteAllText(ReportPath, "");
            EditorPrefs.SetInt(K, 0);
            EditorPrefs.SetBool("PlantGame.PlayFromTitle", false);   // PlayFromTitle would reset the start scene after every reload; restored when the run ends
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");   // with PlayFromTitle off, play mode starts in the open scene
            EditorApplication.isPlaying = true;
        }

        static GameMode ModeOf(int i) => i == 10 ? GameMode.Challenge : GameMode.Exploration;   // index 10 = level 1 again, Challenge

        static void Arm()
        {
            int i = EditorPrefs.GetInt(K, -1); if (i < 0) return;
            var d = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"Assets/ScriptableObjects/World/level_{(i == 10 ? 1 : i + 1)}.asset");
            GameBootstrap.PendingRun = d.ToConfig(ModeOf(i));
            PlantGame.UI.PlayerHud.SkipMenuOnce();   // otherwise Main bounces to the title screen
            _frames = 0; _errors = 0; _firstError = "";
        }

        static void OnLog(string msg, string stack, LogType t) { if (t == LogType.Error || t == LogType.Exception || t == LogType.Assert) { _errors++; if (_firstError == "") _firstError = msg; } }

        static void OnState(PlayModeStateChange s)
        {
            if (s == PlayModeStateChange.ExitingEditMode) Arm();
            if (s == PlayModeStateChange.EnteredEditMode) Next();
        }

        static void Tick()
        {
            if (EditorPrefs.GetInt(K, -1) >= 0 && !EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling && ++_idle > 120) { _idle = 0; EditorApplication.isPlaying = true; return; }
            if (!EditorApplication.isPlaying || EditorPrefs.GetInt(K, -1) < 0) return;
            _idle = 0;
            Application.runInBackground = true;
            if (!Application.isPlaying || Time.timeSinceLevelLoad < 4f) return;   // isPlaying flips before the reload: wait for the real scene
            int i = EditorPrefs.GetInt(K, -1);
            var player = Object.FindAnyObjectByType<PlayerController>();
            var run = Object.FindAnyObjectByType<RunSession>();
            string lvl = $"level {(i == 10 ? 1 : i + 1)} {ModeOf(i)}";
            bool ok = player != null && run != null && _errors == 0;
            File.AppendAllText(ReportPath, $"{(ok ? "OK  " : "FAIL")} {lvl}: player={(player != null)} run={(run != null)} state={(run != null ? run.Config.levelId : "-")} errors={_errors} {_firstError}\n");
            _frames = int.MinValue / 2;
            EditorApplication.isPlaying = false;
        }

        static void Next()
        {
            int i = EditorPrefs.GetInt(K, -1); if (i < 0) return;
            if (i >= 10) { EditorPrefs.SetInt(K, -1); EditorPrefs.SetBool("PlantGame.PlayFromTitle", true); EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Title.unity"); File.AppendAllText(ReportPath, "DONE\n"); return; }
            EditorPrefs.SetInt(K, i + 1);
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;   // lost if a reload comes first: Tick below restarts it
        }
    }
}
