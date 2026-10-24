using System.Collections.Generic;
using UnityEngine;

namespace PlantGame.Core
{
    /// <summary>Marks an island's respawn point (child "Spawn" of the island). Registered while enabled.</summary>
    public class IslandSpawn : MonoBehaviour
    {
        public static readonly List<IslandSpawn> All = new List<IslandSpawn>();
        public float islandRadius = 8.5f;
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        public Vector3 Centre => transform.parent != null ? transform.parent.position : transform.position;
    }
}
