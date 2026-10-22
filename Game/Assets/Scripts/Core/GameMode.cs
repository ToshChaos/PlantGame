using PlantGame.Plants;

namespace PlantGame.Core
{
    public enum GameMode { Exploration, Challenge }

    /// <summary>What the menu hands to the game scene (AGENTS.md 5.5). levelId is null for random/code levels.</summary>
    public struct RunConfig
    {
        public GameMode mode;
        public int seed;
        public int islandCount;
        public PlantKind[] allowedKinds;
        public float timeLimit;
        public string levelId;
    }
}
