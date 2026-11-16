using UnityEngine;

namespace PlantGame.Inventory
{
    /// <summary>
    /// A seed source (a tree or a patch) that drops its seeds when the level starts and, with regrowSeconds, grows new ones.
    /// Drop(seed) is deterministic: System.Random(seed ^ salt) picks the seed types and the spots around the trunk.
    /// It drops once in Start with dropSeed (the generator gives every source its own); a restart reloads the scene, so the seeds come back.
    /// </summary>
    public class SeedTree : MonoBehaviour
    {
        [Tooltip("Seed types this tree may drop (picked with the seeded random).")]
        public ItemDefinition[] possibleSeeds;
        public int seedsToDrop = 1;
        public float dropRadius = 1.6f;
        public float dropHeight = 2.2f;
        public int salt = 0x5EED;
        [Tooltip("Seed of the first drop: the generator numbers the sources of a level, so each one drops its own pattern.")]
        public int dropSeed = 1;
        /// <summary>The usual time a source takes to grow another seed.</summary>
        public const float DefaultRegrowSeconds = 60f;

        [Tooltip("0 = drops once (and again only when its seed is lost off the islands). Otherwise a new seed drops this often while fewer than maxAlive of its seeds lie around.")]
        public float regrowSeconds = 0f;
        public int maxAlive = 2;

        /// <summary>A regrown seed appeared at this world position (not raised for the seeds dropped when the level starts).</summary>
        public static event System.Action<Vector3> SeedRegrown;

        public bool HasDropped { get; private set; }
        readonly System.Collections.Generic.List<GameObject> _dropped = new System.Collections.Generic.List<GameObject>();

        System.Random _rng;
        float _regrowTimer;

        /// <summary>A source that drops once (the Heart Seed) owes this many more: one for each of its seeds lost off the islands.</summary>
        int _owed;

        void OnEnable() { WorldItem.Lost += OnLost; }
        void OnDisable() { WorldItem.Lost -= OnLost; }

        /// <summary>
        /// A pickup fell off the islands and disappeared. A regrowing source needs nothing: its own lost seed no longer lies around, and a
        /// seed the player carried off was replaced when it was picked up. A source that drops once would never give another, so it grows
        /// one back at the usual rate.
        /// </summary>
        void OnLost(ItemDefinition lost)
        {
            if (regrowSeconds > 0f || !HasDropped || possibleSeeds == null || System.Array.IndexOf(possibleSeeds, lost) < 0) return;
            _owed++;
        }

        // the next seed shows as a bud swelling on the source for the last seconds before it drops
        const float BudSeconds = 12f, BudScale = 0.75f;
        Transform _bud; MeshFilter _budMesh; MeshRenderer _budRenderer;
        float _nextAngle; ItemDefinition _nextDef; bool _nextPicked;

        void Start() { Drop(dropSeed); }

        void Update()
        {
            float every = regrowSeconds > 0f ? regrowSeconds : _owed > 0 ? DefaultRegrowSeconds : 0f;
            if (every <= 0f || !HasDropped || _rng == null) return;
            _dropped.RemoveAll(g => g == null);   // picked up, or lost off the islands (WorldItem destroys those)
            if (_dropped.Count >= maxAlive) { _regrowTimer = 0f; ShowBud(0f); return; }
            _regrowTimer += Time.deltaTime;
            float left = every - _regrowTimer;
            if (left <= BudSeconds && !_nextPicked)
            {
                // the same two draws, in the same order, that the drop itself used to make: where it falls, then which seed
                _nextAngle = (float)_rng.NextDouble() * Mathf.PI * 2f; _nextDef = possibleSeeds[_rng.Next(possibleSeeds.Length)]; _nextPicked = true;
            }
            if (_nextPicked) ShowBud(1f - Mathf.Clamp01(left / BudSeconds));
            if (_regrowTimer < every) return;
            _regrowTimer = 0f; _nextPicked = false; ShowBud(0f);
            if (_owed > 0) _owed--;
            var go = SpawnOne(_nextAngle, _nextDef, true);
            if (go != null) SeedRegrown?.Invoke(go.transform.position);
        }

        /// <summary>Groves and poles carry the bud where the seed hangs; a low patch grows it on the ground and the seed hops out of it.</summary>
        bool BudOnGround => dropHeight < 1.5f;
        Vector3 BudPoint(float angle) => BudOnGround ? transform.position + new Vector3(Mathf.Cos(angle) * dropRadius * 0.4f, 0.25f, Mathf.Sin(angle) * dropRadius * 0.4f) : DropPoint(angle);

        Vector3 DropPoint(float angle) => transform.position + new Vector3(Mathf.Cos(angle) * dropRadius * 0.4f, dropHeight, Mathf.Sin(angle) * dropRadius * 0.4f);

        /// <summary>The bud: the coming seed's own mesh at the spot it will drop from, growing from nothing to most of its size (0 hides it).</summary>
        void ShowBud(float k)
        {
            if (k <= 0f || _nextDef == null || _nextDef.worldPrefab == null) { if (_bud != null && _bud.gameObject.activeSelf) _bud.gameObject.SetActive(false); return; }
            if (_bud == null)
            {
                var go = new GameObject("Bud"); go.transform.SetParent(transform, false); _bud = go.transform;
                _budMesh = go.AddComponent<MeshFilter>(); _budRenderer = go.AddComponent<MeshRenderer>();
                go.SetActive(false);   // switched on below, where it gets the coming seed's mesh and its place
            }
            if (!_bud.gameObject.activeSelf)
            {
                var mf = _nextDef.worldPrefab.GetComponentInChildren<MeshFilter>(); var mr = _nextDef.worldPrefab.GetComponentInChildren<MeshRenderer>();
                if (mf == null || mr == null) return;
                _budMesh.sharedMesh = mf.sharedMesh; _budRenderer.sharedMaterial = mr.sharedMaterial;
                _bud.position = BudPoint(_nextAngle); _bud.rotation = Quaternion.Euler(0f, _nextAngle * Mathf.Rad2Deg, 0f);
                _bud.gameObject.SetActive(true);
            }
            float s = k * k * (3f - 2f * k);
            _bud.localScale = Vector3.one * (BudScale * s * (1f + 0.06f * Mathf.Sin(Time.time * 5f)));   // it quivers a little as it swells
        }

        GameObject SpawnOne(float angle, ItemDefinition def = null, bool fromBud = false)
        {
            if (def == null) def = possibleSeeds[_rng.Next(possibleSeeds.Length)];
            if (def == null || def.worldPrefab == null) return null;
            bool hop = fromBud && BudOnGround;
            var pos = hop ? BudPoint(angle) + Vector3.up * 0.15f : DropPoint(angle);
            var go = Instantiate(def.worldPrefab, pos, Quaternion.identity);
            var wi = go.GetComponent<WorldItem>(); if (wi != null) wi.item = def;
            var rb = go.GetComponent<Rigidbody>();
            if (rb != null) rb.AddForce(new Vector3(Mathf.Cos(angle), hop ? 1.6f : 0.2f, Mathf.Sin(angle)) * 2f, ForceMode.VelocityChange);
            _dropped.Add(go);
            return go;
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
            _dropped.Clear(); HasDropped = false; _regrowTimer = 0f; _nextPicked = false; _owed = 0; ShowBud(0f);
        }
    }
}
