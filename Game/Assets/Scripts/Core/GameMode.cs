using PlantGame.Plants;

namespace PlantGame.Core
{
    public enum GameMode { Exploration, Challenge }

    /// <summary>Weather / season of a level: sky, light, music and effects. Random levels derive it from the seed (WorldTheme.Resolve).</summary>
    public enum LevelTheme { Sunny, Rain, Autumn, Snow, Fog, Sunset, Sunrise }

    /// <summary>What you reach at the end of a level (all raise the same win): a flag, a golden fruit, a gate or a campfire.</summary>
    public enum GoalKind { Flag, Fruit, Gate, Fire }

    public static class WorldTheme
    {
        /// <summary>Curated levels carry their theme; random / code levels derive one from the seed (about 31 % sunny, 12 % sunset, 6 % sunrise, 19 % autumn, 12 % rain, 12 % snow, 6 % fog).</summary>
        public static LevelTheme Resolve(RunConfig c)
        {
            if (!string.IsNullOrEmpty(c.levelId)) return c.theme;
            int h = unchecked((int)((uint)c.seed * 2654435761u) >> 4) & 15;
            return h < 4 ? LevelTheme.Sunny : h < 6 ? LevelTheme.Sunset : h < 7 ? LevelTheme.Sunrise : h < 10 ? LevelTheme.Autumn : h < 12 ? LevelTheme.Rain : h < 14 ? LevelTheme.Snow : h < 15 ? LevelTheme.Fog : LevelTheme.Sunny;
        }

        /// <summary>T50: wind gusts are an extra on top of any theme: set per curated level, about one random level in five.</summary>
        public static bool ResolveWind(RunConfig c)
        {
            if (!string.IsNullOrEmpty(c.levelId)) return c.wind;
            return (unchecked((int)((uint)c.seed * 2246822519u) >> 9) & 7) < 2;
        }

        public static GoalKind ResolveGoal(RunConfig c)
        {
            if (Resolve(c) == LevelTheme.Snow) return GoalKind.Fire;   // snowy levels always end at the campfire
            if (!string.IsNullOrEmpty(c.levelId)) return c.goal;
            return GoalKind.Gate;   // the default goal: the temple gate to the next level
        }
    }

    /// <summary>Sources: lakes / ponds on some islands (the start island always). CollectorOnly: no water source at all, a Water Collector plant is the only water.</summary>
    public enum WaterMode { Sources, CollectorOnly }

    /// <summary>What the menu hands to the game scene (AGENTS.md 5.5). levelId is null for random/code levels.</summary>
    [System.Serializable]
    public struct RunConfig
    {
        public GameMode mode;
        public int seed;
        public int islandCount;
        public PlantKind[] allowedKinds;
        public float timeLimit;
        public string levelId;
        public WaterMode waterMode;
        public LevelTheme theme;
        /* T50: gusts of wind push the player (stronger higher up). */ public bool wind;
        public GoalKind goal;
        /* Round trip: the goal is back on the start island and needs the Heart Seed from the farthest island. */ public bool roundTrip;
        /* T46: the islands wind round in a spiral (each heading turns a fixed 50 degrees) instead of a random walk. */ public bool spiral;
        /* Extra optional dead-end islands (curated levels; random levels add their own from the island count). */ public int sideIslands;
        /* Fixed order of gaps for curated levels (null = generated). Includes PlantKind.Glider. */ public PlantKind[] sequence;
    }
}
