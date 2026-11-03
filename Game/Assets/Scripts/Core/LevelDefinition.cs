using UnityEngine;
using PlantGame.Plants;

namespace PlantGame.Core
{
    /// <summary>Curated level (AGENTS.md 5.5). Assets live in ScriptableObjects/World and are listed in the WorldKit. The id level_5 is the hand-authored island puzzle; the others run the procedural generator (with a fixed `sequence` of gaps where given).</summary>
    [CreateAssetMenu(menuName = "PlantGame/Level")]
    public class LevelDefinition : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea] public string description;
        public int seed;
        [Range(4, 8)] public int islandCount = 5;
        public PlantKind[] allowedKinds = { PlantKind.Bridge, PlantKind.Ladder, PlantKind.Platform };
        public WaterMode waterMode;
        public LevelTheme theme;
        [Tooltip("Gusts of wind that push the player (stronger higher up).")] public bool wind;
        public GoalKind goal;
        [Tooltip("Go out to the last island, pick up the Heart Seed and bring it back to the goal on the start island.")]
        public bool roundTrip;
        [Tooltip("T46: the islands curl round in a spiral ascent.")] public bool spiral;
        [Tooltip("Fixed order of gaps (empty = generated from the seed). Glider = a leaf glide down to a lower island.")]
        public PlantKind[] sequence;
        [Tooltip("Optional dead-end islands off the main chain (not needed to finish, with a bonus). Random levels add their own.")]
        [Range(0, 3)] public int sideIslands;

        public RunConfig ToConfig() => new RunConfig
        {
            seed = seed, islandCount = islandCount, allowedKinds = allowedKinds, levelId = id,
            waterMode = waterMode, theme = theme, wind = wind, goal = goal, roundTrip = roundTrip, spiral = spiral, sideIslands = sideIslands, sequence = sequence != null && sequence.Length > 0 ? sequence : null,
        };
    }
}
