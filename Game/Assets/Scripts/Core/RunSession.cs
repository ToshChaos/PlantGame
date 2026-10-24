using UnityEngine;

namespace PlantGame.Core
{
    public enum RunState { Idle, Running, Won, Failed }

    /// <summary>
    /// Contract 5.5: owns the run timer and state, raises events through GameEvents.
    /// Exploration counts up and never fails; Challenge counts down from timeLimit and fails at zero.
    /// The clock uses unscaled time but stops while Time.timeScale is 0 (pause menu, end panel).
    /// Sandbox: starts itself in Start (startOnAwake); T15/T26 call StartRun with the menu's RunConfig instead.
    /// </summary>
    public class RunSession : MonoBehaviour
    {
        public static RunSession Instance { get; private set; }

        [Header("Sandbox defaults (used when startOnAwake)")]
        public bool startOnAwake = true;
        public GameMode mode = GameMode.Exploration;
        public float timeLimit = 0f;
        public string levelId = "sandbox";

        public RunConfig Config;
        public RunState State { get; private set; } = RunState.Idle;
        public float Elapsed { get; private set; }
        /// <summary>timeLimit - Elapsed, Challenge only (0 otherwise).</summary>
        public float Remaining => Config.mode == GameMode.Challenge ? Mathf.Max(0f, Config.timeLimit + BonusTime - Elapsed) : 0f;
        /// <summary>T52: seconds added to the Challenge countdown by time fruits this run.</summary>
        public float BonusTime { get; private set; }
        /// <summary>Best-time key: the curated levelId, or the seed code for random / code levels (so records follow the seed).</summary>
        public string SaveKey => !string.IsNullOrEmpty(Config.levelId) ? Config.levelId : Config.seed != 0 ? SeedCode.Encode(Config) : "sandbox";

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void Start() { if (startOnAwake) StartRun(new RunConfig { mode = mode, timeLimit = timeLimit, levelId = levelId }); }

        public void StartRun(RunConfig c)
        {
            Config = c; Elapsed = 0f; BonusTime = 0f; State = RunState.Running;
            GameEvents.RaiseRunStarted(c.mode);
        }

        void Update()
        {
            if (State != RunState.Running || Time.timeScale <= 0f) return;
            Elapsed += Time.unscaledDeltaTime;
            if (Config.mode == GameMode.Challenge && Config.timeLimit > 0f && Elapsed >= Config.timeLimit + BonusTime) Fail();
        }

        /// <summary>T52: extends the Challenge countdown (the best time stays the plain elapsed time).</summary>
        public void AddTime(float seconds)
        {
            if (State != RunState.Running || Config.mode != GameMode.Challenge || seconds <= 0f) return;
            BonusTime += seconds;
            GameEvents.RaiseTimeAdded(seconds);
        }

        public void Win()
        {
            if (State != RunState.Running) return;
            State = RunState.Won;
            bool best = SaveService.TrySubmitTime(SaveKey, Config.mode, Elapsed);
            GameEvents.RaiseGameWon();
            GameEvents.RaiseRunFinished(Elapsed, best);
        }

        public void Fail()
        {
            if (State != RunState.Running) return;
            State = RunState.Failed;
            GameEvents.RaiseGameFailed();
        }

        public void Restart() { StartRun(Config); }
    }
}
