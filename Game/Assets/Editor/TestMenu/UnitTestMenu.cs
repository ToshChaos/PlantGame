using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace PlantGame.Editor
{
    /// <summary>Menu PlantGame > Test > Run Unit Tests: runs the EditMode suite (Assets/Tests, editor-only, never in a player build) and logs a summary.</summary>
    public static class UnitTestMenu
    {
        sealed class Report : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { if (!result.HasChildren && result.TestStatus == TestStatus.Failed) Debug.LogError($"[Tests] FAILED {result.Test.FullName}\n{result.Message}"); }
            public void RunFinished(ITestResultAdaptor r)
            {
                var msg = $"[Tests] {r.PassCount} passed, {r.FailCount} failed, {r.SkipCount} skipped ({r.Duration:0.0} s)";
                if (r.FailCount > 0) Debug.LogError(msg); else Debug.Log(msg);
            }
        }

        [MenuItem("PlantGame/Test/Run Unit Tests", false, 38)]
        public static void Run()
        {
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Report());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }));
        }

        [MenuItem("PlantGame/Test/Open Test Runner", false, 39)]
        static void Open() => EditorApplication.ExecuteMenuItem("Window/General/Test Runner");
    }
}
