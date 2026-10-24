using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.World
{
    /// <summary>
    /// Decorative things that drop off an island's rim: small rock chips from a crumbling edge, or an occasional leaf from a tree near the
    /// edge. A tiny pool of non-physical bits (no colliders, shared mesh) that spawn on a slow random schedule, fall away below the island
    /// and are recycled. Purely cosmetic and not part of world generation (UnityEngine.Random).
    /// </summary>
    public class FallingBits : MonoBehaviour
    {
        public enum Kind { Rocks, Leaves }

        class Bit { public Transform t; public Vector3 vel, spin; public float age, life, phase; public bool live; }

        public Kind kind;
        /// <summary>Rocks: the crumbling rim point. Leaves: unused.</summary>
        public Vector3 origin, outward;
        /// <summary>Leaves: x, y, z of each leaf tree's canopy and w the canopy height above y; only trees near the rim are used.</summary>
        public List<Vector4> trees = new List<Vector4>();
        public Vector3 islandCentre;
        public Color colour = new Color(0.5f, 0.46f, 0.42f);
        public float minInterval = 1.2f, maxInterval = 3.5f;

        Bit[] _pool; Material _mat; Mesh _mesh; float _next;

        static Mesh _rockMesh, _leafMesh;

        void Start()
        {
            _mat = RuntimeMats.NewLit("M_FallingBit", colour); _mat.hideFlags = HideFlags.None;
            if (_mat.HasProperty("_Smoothness")) _mat.SetFloat("_Smoothness", 0.05f);
            if (_mat.HasProperty("_Cull")) _mat.SetFloat("_Cull", 0f);
            _mesh = kind == Kind.Rocks ? RockMesh() : LeafMesh();
            _pool = new Bit[kind == Kind.Rocks ? 8 : 3];
            for (int i = 0; i < _pool.Length; i++)
            {
                var go = new GameObject(kind + "Bit"); go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = _mesh;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = _mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                go.SetActive(false);
                _pool[i] = new Bit { t = go.transform };
            }
            _next = Time.time + Random.Range(0f, maxInterval);
        }

        void Update()
        {
            if (_pool == null) return;
            float dt = Time.deltaTime;
            foreach (var b in _pool)
            {
                if (!b.live) continue;
                b.age += dt;
                if (kind == Kind.Rocks) b.vel += Vector3.down * 7f * dt;
                else
                {
                    // a leaf flutters: slow fall, sideways sway, steady drift away from the island
                    var sway = Vector3.Cross(Vector3.up, outward) * Mathf.Sin(b.age * 2.4f + b.phase) * 0.9f;
                    b.t.position += (new Vector3(outward.x, 0, outward.z) * 1.1f + sway + Vector3.down * 0.85f) * dt;
                }
                if (kind == Kind.Rocks) b.t.position += b.vel * dt;
                b.t.Rotate(b.spin * dt, Space.Self);
                if (b.age > b.life) { b.live = false; b.t.gameObject.SetActive(false); }
            }
            if (Time.time < _next) return;
            _next = Time.time + Random.Range(minInterval, maxInterval);
            Spawn();
        }

        void Spawn()
        {
            Bit b = null; foreach (var p in _pool) if (!p.live) { b = p; break; }
            if (b == null) return;
            if (kind == Kind.Rocks)
            {
                b.t.position = origin + Vector3.Cross(Vector3.up, outward) * Random.Range(-1.2f, 1.2f) + Vector3.up * Random.Range(-0.1f, 0.2f);
                b.t.localScale = Vector3.one * Random.Range(0.08f, 0.22f);
                b.vel = outward * Random.Range(0.3f, 1.2f) + Vector3.up * Random.Range(0f, 0.8f);
                b.life = 5f; b.spin = new Vector3(Random.Range(-240f, 240f), Random.Range(-240f, 240f), Random.Range(-240f, 240f));
            }
            else
            {
                if (trees.Count == 0) return;
                var tr = trees[Random.Range(0, trees.Count)];
                var dir = new Vector3(tr.x - islandCentre.x, 0, tr.z - islandCentre.z).normalized;
                outward = dir;
                b.t.position = new Vector3(tr.x, tr.y + tr.w * Random.Range(0.55f, 0.9f), tr.z) + new Vector3(Random.Range(-0.8f, 0.8f), 0, Random.Range(-0.8f, 0.8f));
                b.t.localScale = Vector3.one * Random.Range(0.9f, 1.4f);
                b.life = 14f; b.spin = new Vector3(Random.Range(-60f, 60f), Random.Range(-90f, 90f), Random.Range(-60f, 60f)); b.phase = Random.Range(0f, 6.28f);
            }
            b.t.rotation = Random.rotation; b.age = 0f; b.live = true; b.t.gameObject.SetActive(true);
        }

        static Mesh RockMesh()
        {
            if (_rockMesh != null) return _rockMesh;
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube); var m = Object.Instantiate(tmp.GetComponent<MeshFilter>().sharedMesh); m.name = "RockChip";
            Object.Destroy(tmp);
            var v = m.vertices; var rnd = new System.Random(5);   // squash and jitter the cube into a chip
            for (int i = 0; i < v.Length; i++) v[i] = new Vector3(v[i].x * 1.1f, v[i].y * 0.7f, v[i].z) * (0.85f + (float)rnd.NextDouble() * 0.3f);
            m.vertices = v; m.RecalculateNormals(); m.RecalculateBounds();
            return _rockMesh = m;
        }

        static Mesh LeafMesh()
        {
            if (_leafMesh != null) return _leafMesh;
            var m = new Mesh { name = "FallingLeaf" };
            m.SetVertices(new[] { new Vector3(0, 0, -0.09f), new Vector3(0.05f, 0, 0), new Vector3(0, 0, 0.09f), new Vector3(-0.05f, 0, 0) });
            m.SetNormals(new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            m.SetTriangles(new[] { 0, 1, 2, 0, 2, 3, 2, 1, 0, 3, 2, 0 }, 0); m.RecalculateBounds();
            return _leafMesh = m;
        }

        /// <summary>Crumbling edge: chips fall from the rim at `origin` (world), pointing away from the island along `outward`.</summary>
        public static FallingBits AddRocks(Transform island, Vector3 origin, Vector3 outward, Color colour)
        {
            var go = new GameObject("CrumblingEdge"); go.transform.SetParent(island, true);
            var f = go.AddComponent<FallingBits>(); f.kind = Kind.Rocks; f.origin = origin; f.outward = new Vector3(outward.x, 0, outward.z).normalized; f.colour = colour;
            return f;
        }

        /// <summary>Rare leaves that drift away from leaf trees close to the rim and fall off the island.</summary>
        public static FallingBits AddLeaves(Transform island, Vector3 centre, List<Vector4> trees, Color colour)
        {
            var go = new GameObject("FallingLeaves"); go.transform.SetParent(island, true);
            var f = go.AddComponent<FallingBits>(); f.kind = Kind.Leaves; f.islandCentre = centre; f.trees = trees; f.colour = colour; f.minInterval = 6f; f.maxInterval = 14f;
            return f;
        }
    }
}
