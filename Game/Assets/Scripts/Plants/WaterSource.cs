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
        class Patch { public Transform t; public float r; public bool frozen; public GameObject ice; }
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
            p.frozen = false; if (p.ice != null) Destroy(p.ice);
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
        /// <summary>Ices over every patch (call once the water surface exists).</summary>
        public void Freeze()
        {
            Init(); frozen = true;
            float y = surface != null ? surface.bounds.max.y + 0.03f : transform.position.y;
            foreach (var p in _patches)
            {
                p.frozen = true;
                if (p.ice != null) continue;
                p.ice = new GameObject("Ice"); p.ice.transform.SetParent(transform, false);
                p.ice.transform.position = new Vector3(p.t.position.x, y, p.t.position.z);
                p.ice.AddComponent<MeshFilter>().sharedMesh = DiscMesh(p.r);
                var r = p.ice.AddComponent<MeshRenderer>(); r.sharedMaterial = IceMat(); r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static Mesh DiscMesh(float radius)
        {
            const int N = 28;
            var v = new Vector3[N + 1]; var nrm = new Vector3[N + 1]; var uv = new Vector2[N + 1]; var t = new int[N * 3];
            nrm[0] = Vector3.up; uv[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < N; i++)
            {
                float a = i * Mathf.PI * 2f / N; var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v[i + 1] = d * radius; nrm[i + 1] = Vector3.up; uv[i + 1] = new Vector2(d.x, d.z) * 0.5f + new Vector2(0.5f, 0.5f);
                t[i * 3] = 0; t[i * 3 + 1] = (i + 1) % N + 1; t[i * 3 + 2] = i + 1;
            }
            var m = new Mesh { name = "IceDisc", hideFlags = HideFlags.DontSave, vertices = v, normals = nrm, uv = uv, triangles = t };
            m.RecalculateBounds();
            return m;
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
