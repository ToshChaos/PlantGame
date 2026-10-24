using System.Collections.Generic;
using UnityEngine;

namespace PlantGame.World
{
    /// <summary>
    /// A flowering bush that keeps a few butterflies around it: they flutter in loose loops over the bush, wings beating, and now and then
    /// one drifts a little further out and comes back. Purely cosmetic (no colliders, shared wing mesh, a handful of colours).
    /// </summary>
    public class ButterflyBush : MonoBehaviour
    {
        class Fly { public Transform body, left, right; public float phase, speed, radius, height, wing, drift; public Vector3 offset; }

        public int count = 4;
        readonly List<Fly> _flies = new List<Fly>();
        static Mesh _wing; static readonly Dictionary<int, Material> Mats = new Dictionary<int, Material>();
        static readonly Color[] Palette = { new Color(1f, 0.62f, 0.15f), new Color(0.35f, 0.65f, 1f), new Color(1f, 0.9f, 0.3f), new Color(0.96f, 0.96f, 1f), new Color(0.85f, 0.4f, 0.9f) };

        static Mesh Wing()
        {
            if (_wing != null) return _wing;
            var m = new Mesh { name = "ButterflyWing" };
            // a rounded wing hinged at the body (x = 0), spreading along +X; both sides drawn
            var v = new List<Vector3> { new Vector3(0, 0, 0), new Vector3(0.05f, 0, 0.08f), new Vector3(0.12f, 0, 0.07f), new Vector3(0.14f, 0, 0), new Vector3(0.1f, 0, -0.07f), new Vector3(0.04f, 0, -0.06f) };
            var t = new List<int>();
            for (int i = 1; i < v.Count - 1; i++) { t.Add(0); t.Add(i); t.Add(i + 1); t.Add(0); t.Add(i + 1); t.Add(i); }
            m.SetVertices(v); m.SetTriangles(t, 0);
            var nrm = new List<Vector3>(); foreach (var _ in v) nrm.Add(Vector3.up); m.SetNormals(nrm);   // both sides are drawn from the same vertices: recalculated normals would cancel to zero (black wings)
            m.RecalculateBounds();
            return _wing = m;
        }

        static Material ColourMat(int k)
        {
            if (Mats.TryGetValue(k, out var m) && m != null) return m;
            var c = Palette[k % Palette.Length];
            m = Core.RuntimeMats.NewLit("M_Butterfly" + k, c);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.2f);
            m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", c * 0.35f);
            return Mats[k] = m;
        }

        /// <summary>The bush itself (an imported flowering bush or a simple stand-in) with its butterflies, standing at `pos`.</summary>
        public static GameObject Build(Transform parent, Vector3 pos, GameObject bush, System.Random rnd)
        {
            var go = new GameObject("ButterflyBush"); go.transform.SetParent(parent, true); go.transform.position = pos;
            if (bush != null) { bush.transform.SetParent(go.transform, true); }
            var bb = go.AddComponent<ButterflyBush>(); bb.count = 3 + rnd.Next(3);
            bb.Spawn(rnd);
            return go;
        }

        void Spawn(System.Random rnd)
        {
            for (int i = 0; i < count; i++)
            {
                var f = new Fly { phase = (float)rnd.NextDouble() * 6.28f, speed = 0.5f + (float)rnd.NextDouble() * 0.5f, radius = 0.8f + (float)rnd.NextDouble() * 1.4f, height = 0.9f + (float)rnd.NextDouble() * 1.1f, wing = 10f + (float)rnd.NextDouble() * 6f, drift = (float)rnd.NextDouble() * 6.28f };
                var b = new GameObject("Butterfly"); b.transform.SetParent(transform, false);
                var mat = ColourMat(rnd.Next(Palette.Length));
                f.left = Make("WingL", b.transform, mat, 1f); f.right = Make("WingR", b.transform, mat, -1f);
                f.body = b.transform; _flies.Add(f);
            }
        }

        static Transform Make(string n, Transform parent, Material mat, float side)
        {
            var w = new GameObject(n); w.transform.SetParent(parent, false);
            w.transform.localScale = new Vector3(side, 1, 1);
            w.AddComponent<MeshFilter>().sharedMesh = Wing();
            var r = w.AddComponent<MeshRenderer>(); r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return w.transform;
        }

        void Update()
        {
            float t = Time.time;
            foreach (var f in _flies)
            {
                float a = t * f.speed + f.phase;
                float far = 1f + 0.8f * Mathf.Max(0f, Mathf.Sin(t * 0.13f + f.drift));   // occasionally ranges wider
                var p = new Vector3(Mathf.Cos(a) * f.radius * far, f.height + 0.35f * Mathf.Sin(a * 2.3f + f.phase), Mathf.Sin(a * 1.3f) * f.radius * far);
                var next = new Vector3(Mathf.Cos(a + 0.05f) * f.radius * far, p.y, Mathf.Sin((a + 0.05f) * 1.3f) * f.radius * far);
                f.body.localPosition = p;
                var dir = next - p; dir.y = 0f; if (dir.sqrMagnitude > 1e-6f) f.body.localRotation = Quaternion.Slerp(f.body.localRotation, Quaternion.LookRotation(dir.normalized), 4f * Time.deltaTime);
                float flap = Mathf.Sin(t * f.wing + f.phase) * 55f;
                f.left.localRotation = Quaternion.Euler(0, 0, flap); f.right.localRotation = Quaternion.Euler(0, 0, flap);
            }
        }
    }
}
