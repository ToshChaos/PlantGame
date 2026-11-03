using UnityEngine;

namespace PlantGame.Inventory
{
    /// <summary>
    /// Seeds spin slowly. Each one picks its own direction (clockwise or not) and a speed between 0.2 and `degreesPerSecond` deg/s,
    /// so a patch of seeds never turns in unison. (Colour comes from the per-prefab material, not a property block.)
    /// </summary>
    public class ItemBob : MonoBehaviour
    {
        /// <summary>Upper bound of the random spin speed (the old fixed speed).</summary>
        public float degreesPerSecond = 60f;
        public const float MinDegreesPerSecond = 0.2f;
        float _speed;

        void Awake() { _speed = Random.Range(MinDegreesPerSecond, Mathf.Max(MinDegreesPerSecond, degreesPerSecond)) * (Random.value < 0.5f ? -1f : 1f); }

        void Update() { transform.Rotate(0f, _speed * Time.deltaTime, 0f, Space.World); }
    }
}
