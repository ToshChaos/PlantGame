using UnityEngine;

namespace PlantGame.Core
{
    /// <summary>
    /// Waves a raised flag: every mesh under this transform (pennant, hems, emblems, tassels) is bent sideways by a travelling wave
    /// that grows towards the free end (local +x), so the parts move together. Fades in over a second; runs on unscaled time.
    /// </summary>
    public class FlagWave : MonoBehaviour
    {
        public float amplitude = 0.32f, wavesPerLength = 1.1f, speed = 3.2f, length = 3.4f;

        const float NormalsInterval = 1f / 30f;   // the cloth moves every frame, its shading normals follow at most this often (the waves are slow)

        struct Part { public MeshFilter filter; public Renderer renderer; public Mesh mesh; public Vector3[] rest, work; public Matrix4x4 toFlag, fromFlag; public float normalsAt; }
        Part[] _parts;
        float _start;

        void Start()
        {
            _start = Time.unscaledTime;
            var filters = GetComponentsInChildren<MeshFilter>();
            _parts = new Part[filters.Length];
            for (int i = 0; i < filters.Length; i++)
            {
                var f = filters[i]; var m = Instantiate(f.sharedMesh); m.name = f.sharedMesh.name; f.sharedMesh = m;   // own copy: the tassels share one mesh
                m.MarkDynamic();
                var rest = m.vertices;
                _parts[i] = new Part { filter = f, renderer = f.GetComponent<Renderer>(), mesh = m, rest = rest, work = new Vector3[rest.Length], normalsAt = _start - i * NormalsInterval / filters.Length,   // out of step: the parts do not all refresh in one frame
                    toFlag = transform.worldToLocalMatrix * f.transform.localToWorldMatrix, fromFlag = f.transform.worldToLocalMatrix * transform.localToWorldMatrix };
            }
        }

        void Update()
        {
            float t = Time.unscaledTime, fade = Mathf.Clamp01((t - _start) / 1.2f), k = wavesPerLength * 2f * Mathf.PI / length;
            for (int i = 0; i < _parts.Length; i++)
            {
                ref var p = ref _parts[i];
                if (p.filter == null || (p.renderer != null && !p.renderer.isVisible)) continue;   // off-screen: the wave is a pure function of time, so skipping a part never puts it out of step
                for (int v = 0; v < p.rest.Length; v++)
                {
                    var q = p.toFlag.MultiplyPoint3x4(p.rest[v]);
                    float x = Mathf.Max(q.x, 0f) / length;
                    // a main wave plus a faster ripple, bigger towards the free end; the far edge also dips a little
                    q.z += fade * amplitude * x * (Mathf.Sin(k * q.x - speed * t + q.y * 0.4f) + 0.3f * Mathf.Sin(2.3f * k * q.x - 1.7f * speed * t));
                    q.x -= fade * 0.06f * x * x * (1f + Mathf.Sin(speed * t * 0.5f));
                    p.work[v] = p.fromFlag.MultiplyPoint3x4(q);
                }
                p.mesh.vertices = p.work;
                if (t - p.normalsAt >= NormalsInterval) { p.mesh.RecalculateNormals(); p.normalsAt = t; }
                p.mesh.RecalculateBounds();
                var b = p.mesh.bounds; b.Expand(2f * amplitude); p.mesh.bounds = b;   // room for the whole swing, so culling never acts on a stale, narrower frame
            }
        }
    }
}
