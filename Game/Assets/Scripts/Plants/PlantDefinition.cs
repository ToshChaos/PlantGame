using UnityEngine;

namespace PlantGame.Plants
{
    /// <summary>Glider is a gap crossed with the one-use parachute leaf (no plant). It is only used by curated level sequences, never in seed codes (4-bit mask).</summary>
    public enum PlantKind { Bridge, Ladder, Platform, Collector, Glider }

    /// <summary>STUB from T00 (contract 5.5). Owner A2 extends this in T11.</summary>
    [CreateAssetMenu(menuName = "PlantGame/Plant Definition")]
    public class PlantDefinition : ScriptableObject
    {
        public string id;
        public PlantKind kind;
        public bool needsWater = true;          // false for Collector
        public GameObject[] variants;           // baked Blender variants (prefabs)
        public float growSeconds;
        public AnimationCurve growCurve = AnimationCurve.Linear(0, 0, 1, 1);
    }
}
