using UnityEngine;

namespace PlantGame.Core
{
    /// <summary>
    /// Whether it is raining right now. Only rain levels have showers that stop and restart (WeatherController); everywhere else
    /// `Raining` is false and `RainLevel` false, so dew plants always fill and mushrooms always need water.
    /// </summary>
    public static class WeatherState
    {
        public static bool RainLevel { get; private set; }
        public static bool Raining { get; private set; }
        /// <summary>0..1 rain strength (fades in and out), for the ambience.</summary>
        public static float Intensity { get; private set; }

        public static void Set(bool rainLevel, bool raining, float intensity)
        {
            RainLevel = rainLevel; Raining = rainLevel && raining; Intensity = rainLevel ? intensity : 0f;
        }

        /// <summary>Dew plants only collect while it rains on a rain level.</summary>
        public static bool DewFalls => !RainLevel || Raining;
        /// <summary>Mushrooms (platform plants) grow on their own in the rain.</summary>
        public static bool RainWaters => RainLevel && Raining;
    }
}
