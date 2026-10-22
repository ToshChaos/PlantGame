using UnityEngine;

namespace PlantGame.Plants
{
    /// <summary>
    /// T01 helper: sweeps the shader property _Growth 0 -> 1 -> 0 on all child renderers using a
    /// MaterialPropertyBlock (the same mechanism T11 uses). Also usable as a manual slider in the Inspector.
    /// </summary>
    [ExecuteAlways]
    public class GrowSweep : MonoBehaviour
    {
        static readonly int GrowthId = Shader.PropertyToID("_Growth");

        [Range(0f, 1f)] public float growth = 0f;
        public bool autoSweep = true;
        [Tooltip("Seconds for one 0 -> 1 pass")] public float seconds = 4f;
        public float holdAtEnds = 0.6f;

        Renderer[] _renderers;
        MaterialPropertyBlock _mpb;

        void OnEnable() { _renderers = GetComponentsInChildren<Renderer>(); _mpb = new MaterialPropertyBlock(); }

        void Update()
        {
            if (autoSweep && Application.isPlaying)
            {
                // triangle wave with small holds at both ends
                float cycle = seconds + holdAtEnds;
                float t = Mathf.Repeat(Time.time, cycle * 2f);
                float u = t < cycle ? Mathf.Clamp01(t / seconds) : 1f - Mathf.Clamp01((t - cycle) / seconds);
                growth = Mathf.SmoothStep(0f, 1f, u);
            }
            Apply();
        }

        void OnValidate() { if (isActiveAndEnabled) { OnEnable(); Apply(); } }

        public void Apply()
        {
            if (_renderers == null) return;
            foreach (var r in _renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(GrowthId, growth);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
