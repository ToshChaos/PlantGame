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
    /// T22: PlantGame > Smoke Test Levels. Plays each curated level in the Main scene for a few seconds
    /// and checks that a player, a goal and a run exist and the console stays free of errors. Result lines go to Logs/smoke.txt.
    /// State lives in EditorPrefs so it survives the domain reloads of entering play mode (SessionState was lost mid-run).
    /// </summary>
    [InitializeOnLoad]
    public static class LevelSmokeTest
    {
        const string K = "PlantGame.Smoke.Index", ReportPath = "Logs/smoke.txt";
        static int _errors, _idle, _f0; static bool _reported; static float _t0 = -1; static string _firstError = "";

        static LevelSmokeTest()
        {
            if (EditorPrefs.GetInt(K, -1) < 0) return;
            Application.logMessageReceived += OnLog;
            EditorApplication.playModeStateChanged += OnState;
            EditorApplication.update += Tick;
            if (EditorApplication.isPlayingOrWillChangePlaymode) Arm();
        }

        [MenuItem("PlantGame/Smoke Test Levels", false, 102)]
        static void Start()
        {
            File.WriteAllText(ReportPath, "");
            EditorPrefs.SetInt(K, 0);
            EditorPrefs.SetBool("PlantGame.PlayFromTitle", false);   // PlayFromTitle would reset the start scene after every reload; restored when the run ends
            EditorSceneManager.playModeStartScene = null;            // the pref alone does not clear it: the first level would start in Title
            EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");   // with PlayFromTitle off, play mode starts in the open scene
            EditorApplication.isPlaying = true;
        }

        static void Arm()
        {
            int i = EditorPrefs.GetInt(K, -1); if (i < 0) return;
            var d = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"Assets/ScriptableObjects/World/level_{i + 1}.asset");
            GameBootstrap.PendingRun = d.ToConfig();
            PlantGame.UI.PlayerHud.SkipMenuOnce();   // otherwise Main bounces to the title screen
            _errors = 0; _reported = false; _t0 = -1; _firstError = "";
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
            if (_reported || !Application.isPlaying || Time.timeSinceLevelLoad < 4f) return;
            if (_t0 < 0) { _t0 = Time.realtimeSinceStartup; _f0 = Time.frameCount; }
            if (Time.realtimeSinceStartup - _t0 < 4f) return;   // measure 4 s of frames after the load settles   // isPlaying flips before the reload: wait for the real scene
            int i = EditorPrefs.GetInt(K, -1);
            var player = Object.FindAnyObjectByType<PlayerController>();
            var run = Object.FindAnyObjectByType<RunSession>();
            string lvl = $"level {i + 1}";
            float ms = (Time.realtimeSinceStartup - _t0) * 1000f / Mathf.Max(1, Time.frameCount - _f0);
            string perf = $" avg {ms:0.0} ms/frame, {UnityStats.drawCalls} draw calls, {UnityStats.setPassCalls} setpass, {UnityStats.triangles / 1000} k tris, {UnityStats.shadowCasters} shadow casters";
            _reported = true;
            bool ok = player != null && run != null && _errors == 0;
            File.AppendAllText(ReportPath, $"{(ok ? "OK  " : "FAIL")} {lvl}: player={(player != null)} run={(run != null)} state={(run != null ? run.Config.levelId : "-")} errors={_errors} {_firstError}{perf}\n");
            EditorApplication.isPlaying = false;
        }

        static void Next()
        {
            int i = EditorPrefs.GetInt(K, -1); if (i < 0) return;
            if (i >= 9) { EditorPrefs.SetInt(K, -1); EditorPrefs.SetBool("PlantGame.PlayFromTitle", true); EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>("Assets/Scenes/Title.unity"); File.AppendAllText(ReportPath, "DONE\n"); return; }
            EditorPrefs.SetInt(K, i + 1);
            EditorApplication.delayCall += () => EditorApplication.isPlaying = true;   // lost if a reload comes first: Tick below restarts it
        }
    }
}
