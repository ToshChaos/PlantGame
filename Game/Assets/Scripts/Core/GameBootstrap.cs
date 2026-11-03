using System.Collections.Generic;
using UnityEngine;
using PlantGame.Player;

namespace PlantGame.Core
{
    /// <summary>
    /// T15: glue for Main. Picks the RunConfig (static PendingRun from the menu, else Level 1 Exploration), hands it to the RunSession
    /// (which then waits for the HUD's Start), sets the kill plane under the lowest island, keeps the player's respawn point on the
    /// last island they stood on, and turns a held R into a run restart.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameBootstrap : MonoBehaviour
    {
        public const string Level1Id = "level_1";
        /// <summary>The hand-authored seven-island puzzle (LevelPlanner.HandPlan).</summary>
        public const string HandLevelId = "level_5";

        /// <summary>Set by the menu (T26) before loading Main; kept across reloads so Restart sticks.</summary>
        public static RunConfig? PendingRun;

        public static RunConfig DefaultConfig() => new RunConfig
        {
            seed = 1, islandCount = 5, levelId = Level1Id,
            allowedKinds = new[] { Plants.PlantKind.Bridge, Plants.PlantKind.Ladder, Plants.PlantKind.Platform },
        };

        public static RunConfig Current => PendingRun ?? DefaultConfig();


        public float killDepth = 30f;
        PlayerController _player;
        float _nextCheck;

        void Awake()
        {
            var rs = FindAnyObjectByType<RunSession>();
            if (rs != null) { rs.startOnAwake = false; rs.Config = Current; }
        }

        void Start()
        {
            _player = PlayerController.Instance;
            if (_player == null) return;
            float lowest = float.MaxValue;
            foreach (var s in IslandSpawn.All) lowest = Mathf.Min(lowest, s.Centre.y);
            if (lowest < float.MaxValue) _player.killPlaneY = lowest - killDepth;
            _player.RestartRequested += OnRestart;
            GameEvents.RaiseWorldGenerated(Current.seed);
        }

        void OnDestroy() { if (_player != null) _player.RestartRequested -= OnRestart; }

        void OnRestart() { var hud = FindAnyObjectByType<UI.PlayerHud>(); if (hud != null) hud.RestartRun(); }

        void Update()
        {
            if (_player == null || Time.unscaledTime < _nextCheck) return;
            _nextCheck = Time.unscaledTime + 0.2f;
            if (_player.CurrentState != PlayerController.State.Grounded) return;
            Vector3 p = _player.transform.position;
            IslandSpawn best = null; float bestD = float.MaxValue;
            foreach (var s in IslandSpawn.All)
            {
                var c = s.Centre;
                float d = new Vector2(p.x - c.x, p.z - c.z).magnitude;
                if (d > s.islandRadius || Mathf.Abs(p.y - c.y) > 3f || d >= bestD) continue;
                best = s; bestD = d;
            }
            if (best != null) _player.respawnPoint = best.transform;
        }
    }
}
