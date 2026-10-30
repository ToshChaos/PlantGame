using UnityEngine;

namespace PlantGame.World
{
    /// <summary>A gem that glints: its emission pulses and flares briefly now and then, each gem on its own rhythm. Cheap (one property block, no particles).</summary>
    public class GemSparkle : MonoBehaviour
    {
        public Color glow = new Color(0.49f, 0.95f, 0.9f);
        public float baseLevel = 0.22f, flare = 1.4f;   // the shards are dark glass: a soft glow, a clear glint
        Renderer[] _rs; MaterialPropertyBlock _mb; float _phase, _period;
        static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        void Start()
        {
            _rs = GetComponentsInChildren<Renderer>(); _mb = new MaterialPropertyBlock();
            var p = transform.position; _phase = (p.x * 12.9898f + p.z * 78.233f) % 6.2832f; _period = 2.5f + Mathf.Abs(Mathf.Sin(p.x * 3.1f + p.z)) * 3f;
        }
        void Update()
        {
            if (_rs == null || _mb == null) Start();   // survives a script reload while playing
            float t = Time.time + _phase * 3f;
            float k = baseLevel + 0.25f * Mathf.Sin(t * 1.3f);
            float f = Mathf.Repeat(t, _period); if (f < 0.35f) k += flare * Mathf.Sin(f / 0.35f * Mathf.PI);   // a quick glint
            _mb.SetColor(Emission, glow * k);
            foreach (var r in _rs) if (r != null) r.SetPropertyBlock(_mb);
        }
    }
}
