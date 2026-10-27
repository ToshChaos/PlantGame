using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Plants
{
    public enum WaterKind { Lake, Waterfall }

    /// <summary>
    /// Contract 5.5: unlimited water (lake, waterfall, sandbox puddle). F (Water) fills the watering can instantly.
    /// Minimal version for the sandbox; T27/T29 add the kit pieces and feedback. Layer Water, trigger collider.
    /// </summary>
    public class WaterSource : MonoBehaviour, IInteractable
    {
        public WaterKind kind;
        [Tooltip("T45/T50: frozen water cannot refill the can and gives no refill hint until a Heat Seed melts it. A Heat Seed melts one patch (one disc of the pond or stream) only.")] public bool frozen;
        [System.NonSerialized] public Renderer surface; [System.NonSerialized] public Material liquid;

        /// <summary>One disc of the pond / stream (a child BoxCollider): frozen patches wear an ice cap.</summary>
        class Patch { public Transform t; public float r; public bool frozen; public MeshRenderer piece; }
        System.Collections.Generic.List<Patch> _patches;
        const float Reach = 2.0f;

        void Init()
        {
            if (_patches != null) return;
            _patches = new System.Collections.Generic.List<Patch>();
            foreach (var b in GetComponentsInChildren<BoxCollider>()) _patches.Add(new Patch { t = b.transform, r = b.size.x * 0.5f });
        }

        static float Dist(Patch p, Vector3 pos) { var d = p.t.position - pos; d.y = 0f; return d.magnitude - p.r; }

        Patch Nearest(Vector3 pos, bool wantFrozen)
        {
            Init(); Patch best = null; float bd = Reach;
            foreach (var p in _patches) { if (p.frozen != wantFrozen) continue; float d = Dist(p, pos); if (d <= bd) { bd = d; best = p; } }
            return best;
        }

        /// <summary>Some still-frozen patch within reach of pos.</summary>
        public bool FrozenNear(Vector3 pos) => Nearest(pos, true) != null;
        /// <summary>Some open (unfrozen) water within reach of pos: the can can be refilled here.</summary>
        public bool WaterNear(Vector3 pos) => Nearest(pos, false) != null;
        /// <summary>Melts the frozen patch nearest to pos (only that one).</summary>
        public bool ThawNear(Vector3 pos)
        {
            var p = Nearest(pos, true); if (p == null) return false;
            p.frozen = false; if (p.piece != null && liquid != null) p.piece.sharedMaterial = liquid;
            Init(); frozen = _patches.Exists(q => q.frozen);
            return true;
        }
        static Material _ice;
        public static Material IceMat()
        {
            if (_ice != null) return _ice;
            _ice = RuntimeMats.NewLit("M_Ice", new Color(0.80f, 0.92f, 1f));
            if (_ice.HasProperty("_Smoothness")) _ice.SetFloat("_Smoothness", 0.95f);
            _ice.EnableKeyword("_EMISSION"); _ice.SetColor("_EmissionColor", new Color(0.45f, 0.75f, 1f) * 0.45f);   // a cold glow marks frozen water from a distance
            return _ice;
        }
        /// <summary>Ices over every patch (call once the water surface exists). The surface mesh is split into one piece per patch
        /// (each triangle goes to the nearest patch); a frozen piece wears the ice material, a melted one the water again.</summary>
        public void Freeze()
        {
            Init(); frozen = true;
            if (surface != null && _patches.Count > 0 && _patches[0].piece == null) SplitSurface();
            foreach (var p in _patches) { p.frozen = true; if (p.piece != null) p.piece.sharedMaterial = IceMat(); }
            if (_patches.Count == 0 && surface != null) surface.sharedMaterial = IceMat();
        }

        void SplitSurface()
        {
            var mf = surface.GetComponent<MeshFilter>(); var src = mf != null ? mf.sharedMesh : null; if (src == null) return;
            var verts = src.vertices; var tris = src.triangles;
            var lists = new System.Collections.Generic.List<int>[_patches.Count];
            for (int i = 0; i < lists.Length; i++) lists[i] = new System.Collections.Generic.List<int>();
            for (int i = 0; i + 2 < tris.Length; i += 3)
            {
                var c = surface.transform.TransformPoint((verts[tris[i]] + verts[tris[i + 1]] + verts[tris[i + 2]]) / 3f);
                int best = 0; float bd = float.MaxValue;
                for (int k = 0; k < _patches.Count; k++) { float d = Dist(_patches[k], c); if (d < bd) { bd = d; best = k; } }
                lists[best].Add(tris[i]); lists[best].Add(tris[i + 1]); lists[best].Add(tris[i + 2]);
            }
            for (int k = 0; k < _patches.Count; k++)
            {
                var go = new GameObject("WaterPiece"); go.layer = surface.gameObject.layer;
                go.transform.SetParent(surface.transform, false);
                var m = Instantiate(src); m.name = "WaterPiece"; m.hideFlags = HideFlags.DontSave; m.triangles = lists[k].ToArray();
                go.AddComponent<MeshFilter>().sharedMesh = m;
                var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = surface.sharedMaterial;
                r.shadowCastingMode = surface.shadowCastingMode; r.receiveShadows = surface.receiveShadows;
                _patches[k].piece = r;
            }
            surface.enabled = false;
        }

        public InteractAction Action => InteractAction.Use;
        public string Prompt => "Refill watering can";

        public bool CanInteract(GameObject actor)
        {
            var can = actor != null ? actor.GetComponent<WateringCan>() : null;
            return can != null && !can.IsFull && WaterNear(actor.transform.position + Vector3.up);
        }

        public void Interact(GameObject actor)
        {
            var can = actor != null ? actor.GetComponent<WateringCan>() : null;
            if (can != null) can.Refill(WateringCan.MaxCharges);
        }
    }
}
