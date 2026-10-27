using UnityEngine;

namespace PlantGame.Core
{
    public enum RunState { Idle, Running, Won }

    /// <summary>
    /// Contract 5.5: owns the run timer and state, raises events through GameEvents.
    /// The clock counts up and the run never fails (falling just respawns).
    /// The clock uses unscaled time but stops while Time.timeScale is 0 (pause menu, end panel).
    /// Sandbox: starts itself in Start (startOnAwake); T15/T26 call StartRun with the menu's RunConfig instead.
    /// </summary>
    public class RunSession : MonoBehaviour
    {
        public static RunSession Instance { get; private set; }

        [Header("Sandbox defaults (used when startOnAwake)")]
        public bool startOnAwake = true;
        public string levelId = "sandbox";

        public RunConfig Config;
        public RunState State { get; private set; } = RunState.Idle;
        public float Elapsed { get; private set; }
        /// <summary>Best-time key: the curated levelId, or the seed code for random / code levels (so records follow the seed).</summary>
        public string SaveKey => !string.IsNullOrEmpty(Config.levelId) ? Config.levelId : Config.seed != 0 ? SeedCode.Encode(Config) : "sandbox";

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void Start() { if (startOnAwake) StartRun(new RunConfig { levelId = levelId }); }

        public void StartRun(RunConfig c)
        {
            Config = c; Elapsed = 0f; State = RunState.Running;
            GameEvents.RaiseRunStarted();
        }

        void Update()
        {
            if (State != RunState.Running || Time.timeScale <= 0f) return;
            Elapsed += Time.unscaledDeltaTime;
        }

        public void Win()
        {
            if (State != RunState.Running) return;
            State = RunState.Won;
            bool best = SaveService.TrySubmitTime(SaveKey, Elapsed);
            GameEvents.RaiseGameWon();
            GameEvents.RaiseRunFinished(Elapsed, best);
        }

        public void Restart() { StartRun(Config); }
    }
}
