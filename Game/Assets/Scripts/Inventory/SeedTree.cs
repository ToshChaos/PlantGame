using System;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Inventory
{
    /// <summary>
    /// A tree on the start island that drops its seeds when the level starts (user request, T12/T14).
    /// Drop(seed) is deterministic: System.Random(seed ^ salt) picks the seed types and the spots around the trunk.
    /// With dropOnStart the tree drops once in Start (sandbox / no RunSession yet); T15 turns that off and calls Drop(worldSeed)
    /// from RunStarted so a restarted run gets its seeds again (call Clear first to remove leftovers).
    /// </summary>
    public class SeedTree : MonoBehaviour
    {
        [Tooltip("Seed types this tree may drop (picked with the seeded random).")]
        public ItemDefinition[] possibleSeeds;
        public int seedsToDrop = 1;
        public float dropRadius = 1.6f;
        public float dropHeight = 2.2f;
        public int salt = 0x5EED;
        public bool dropOnStart = true;
        public int sandboxSeed = 1;
        [Tooltip("0 = drops once. Otherwise a new seed drops this often while fewer than maxAlive of its seeds lie around.")]
        public float regrowSeconds = 0f;
        public int maxAlive = 2;

        public bool HasDropped { get; private set; }
        readonly System.Collections.Generic.List<GameObject> _dropped = new System.Collections.Generic.List<GameObject>();

        System.Random _rng;
        float _regrowTimer;

        void Start() { if (dropOnStart) Drop(sandboxSeed); }

        void Update()
        {
            if (regrowSeconds <= 0f || !HasDropped || _rng == null) return;
            _dropped.RemoveAll(g => g == null);   // picked up (or destroyed)
            if (_dropped.Count >= maxAlive) { _regrowTimer = 0f; return; }
            _regrowTimer += Time.deltaTime;
            if (_regrowTimer < regrowSeconds) return;
            _regrowTimer = 0f;
            SpawnOne((float)_rng.NextDouble() * Mathf.PI * 2f);
        }

        void SpawnOne(float angle)
        {
            var def = possibleSeeds[_rng.Next(possibleSeeds.Length)];
            if (def == null || def.worldPrefab == null) return;
            var pos = transform.position + new Vector3(Mathf.Cos(angle) * dropRadius * 0.4f, dropHeight, Mathf.Sin(angle) * dropRadius * 0.4f);
            var go = Instantiate(def.worldPrefab, pos, Quaternion.identity);
            var wi = go.GetComponent<WorldItem>(); if (wi != null) wi.item = def;
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(new Vector3(Mathf.Cos(angle), 0.2f, Mathf.Sin(angle)) * 2f, ForceMode.VelocityChange);
            _dropped.Add(go);
        }

        public void Drop(int worldSeed)
        {
            if (possibleSeeds == null || possibleSeeds.Length == 0) return;
            Clear();
            _rng = new System.Random(worldSeed ^ salt ^ (int)(transform.position.x * 73f) ^ (int)(transform.position.z * 151f));
            float a0 = (float)_rng.NextDouble() * Mathf.PI * 2f;
            for (int i = 0; i < seedsToDrop; i++) SpawnOne(a0 + i * Mathf.PI * 2f / Mathf.Max(1, seedsToDrop));
            HasDropped = true;
        }

        /// <summary>Removes seeds this tree dropped that nobody has picked up yet.</summary>
        public void Clear()
        {
            foreach (var g in _dropped) if (g != null) Destroy(g);
            _dropped.Clear(); HasDropped = false;
        }
    }
}
