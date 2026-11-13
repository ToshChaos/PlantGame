using System.Collections.Generic;
using UnityEngine;

namespace PlantGame.World
{
    /// <summary>A tree that starts without leaves. F next to it (no water needed) swaps in the leafy mesh with a small pop. Purely for fun.</summary>
    public class LeaflessTree : MonoBehaviour
    {
        public static readonly List<LeaflessTree> All = new List<LeaflessTree>();
        public const float Reach = 3.5f;

        MeshFilter[] _filters; Mesh[] _leafy; Transform _model; Vector3 _scale; float _pop = -1f;
        public bool Grown { get; private set; }

        /// <summary>Remembers the leafy meshes and swaps in copies without the leaf submeshes. Returns false (tree stays leafy) when the meshes cannot be read.</summary>
        public bool Setup(GameObject model)
        {
            var fs = new List<MeshFilter>(); var leafy = new List<Mesh>(); var bare = new List<Mesh>();
            foreach (var mf in model.GetComponentsInChildren<MeshFilter>())
            {
                var src = mf.sharedMesh; var mr = mf.GetComponent<MeshRenderer>();
                if (src == null || mr == null) continue;
                var mats = mr.sharedMaterials; bool hasLeaf = false;
                for (int i = 0; i < mats.Length; i++) if (mats[i] != null && mats[i].name.Contains("Leaves")) hasLeaf = true;
                if (!hasLeaf) continue;
                if (!src.isReadable) return false;
                var m = new Mesh { name = src.name + "_Bare", indexFormat = src.indexFormat };
                m.vertices = src.vertices; m.normals = src.normals; m.uv = src.uv;   // as Scenery.SimplifyTree: no tangents, no vertex colours
                m.subMeshCount = src.subMeshCount;
                for (int i = 0; i < src.subMeshCount; i++)
                {
                    bool leaf = i < mats.Length && mats[i] != null && mats[i].name.Contains("Leaves");
                    m.SetIndices(leaf ? System.Array.Empty<int>() : src.GetIndices(i), MeshTopology.Triangles, i);
                }
                m.RecalculateBounds();
                fs.Add(mf); leafy.Add(src); bare.Add(m);
            }
            if (fs.Count == 0) return false;
            _filters = fs.ToArray(); _leafy = leafy.ToArray();
            for (int i = 0; i < _filters.Length; i++) _filters[i].sharedMesh = bare[i];
            _model = model.transform; _scale = _model.localScale;
            return true;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }
        void OnDestroy() { if (_filters != null && !Grown) foreach (var mf in _filters) if (mf != null && mf.sharedMesh != null) Destroy(mf.sharedMesh); }

        /// <summary>The nearest tree still without leaves within Reach of `p`, or null.</summary>
        public static LeaflessTree Nearest(Vector3 p)
        {
            LeaflessTree best = null; float bestD = Reach * Reach;
            foreach (var t in All)
            {
                if (t == null || t.Grown || t._model == null) continue;
                var c = t.transform.position; float d = new Vector2(c.x - p.x, c.z - p.z).sqrMagnitude;   // measured to the trunk, flat
                if (d < bestD) { bestD = d; best = t; }
            }
            return best;
        }

        public void GrowLeaves()
        {
            if (Grown) return;
            Grown = true;
            for (int i = 0; i < _filters.Length; i++)
            {
                if (_filters[i] == null) continue;
                var bare = _filters[i].sharedMesh; _filters[i].sharedMesh = _leafy[i]; if (bare != null) Destroy(bare);
            }
            _pop = 0f;
        }

        void Update()
        {
            if (_pop < 0f || _model == null) return;
            _pop += Time.unscaledDeltaTime / 0.5f;
            if (_pop >= 1f) { _model.localScale = _scale; _pop = -1f; return; }
            _model.localScale = _scale * (1f + 0.06f * Mathf.Sin(_pop * Mathf.PI));
        }
    }
}
