using UnityEngine;

namespace PlantGame.Plants
{
    public enum PlantKind { Bridge, Ladder, Platform, Collector }

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
