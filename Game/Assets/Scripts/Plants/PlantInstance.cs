using System.Collections.Generic;
using UnityEngine;

namespace PlantGame.Plants
{
    /// <summary>
    /// Minimal runtime plant (T11 will extend): shows a sprout, then grows by sweeping the shader's _Growth
    /// (M_PlantGrow, vertex colour R = growth_t) and enables the COL_* collision boxes base-to-tip as the growth passes them.
    /// Built from the baked Blender variants by `PlantGame > 7 Build Bridge Prefabs`.
    /// </summary>
    public class PlantInstance : MonoBehaviour
    {
        static readonly int GrowthId = Shader.PropertyToID("_Growth");

        public float sproutGrowth = 0.08f;
        public float growSeconds = 6f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>Static hooks for audio / VFX (T18, T20): a plant was sprouted, started growing, finished growing.</summary>
        public static event System.Action<PlantInstance> Sprouted, GrowthStarted, Grown;

        public PlantKind kind;
        public float Growth { get; private set; }
        public bool IsGrown { get; private set; }
        public bool IsGrowing { get; private set; }

        Renderer[] _renderers;
        readonly List<Collider> _cols = new List<Collider>();
        PlantGame.Player.ClimbableVolume[] _climbVolumes;   // ladders: only climbable once fully grown
        MaterialPropertyBlock _mpb;
        float _t;

        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _mpb = new MaterialPropertyBlock();
            _climbVolumes = GetComponentsInChildren<PlantGame.Player.ClimbableVolume>(true);
            var named = new List<Collider>();
            foreach (var c in GetComponentsInChildren<Collider>(true)) if (c.name.StartsWith("COL_")) named.Add(c);
            named.Sort((a, b) => string.CompareOrdinal(a.name, b.name) == 0 ? 0 : IndexOf(a).CompareTo(IndexOf(b)));
            _cols.AddRange(named);
            SetGrowth(sproutGrowth);
        }

        static int IndexOf(Collider c) { var p = c.name.Split('_'); return p.Length > 2 && int.TryParse(p[2], out int i) ? i : 0; }

        public void ShowSprout() { IsGrowing = false; IsGrown = false; _t = 0; SetGrowth(sproutGrowth); Sprouted?.Invoke(this); }

        public void StartGrowing() { if (!IsGrown && !IsGrowing) { IsGrowing = true; GrowthStarted?.Invoke(this); } }

        void Update()
        {
            if (!IsGrowing) return;
            _t += Time.deltaTime / Mathf.Max(0.01f, growSeconds);
            float k = Mathf.Clamp01(_t);
            SetGrowth(Mathf.Lerp(sproutGrowth, 1f, curve.Evaluate(k)));
            if (k >= 1f) { IsGrowing = false; IsGrown = true; Grown?.Invoke(this); }
        }

        void SetGrowth(float g)
        {
            Growth = g;
            foreach (var r in _renderers)
            {
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(GrowthId, g);
                r.SetPropertyBlock(_mpb);
            }
            for (int i = 0; i < _cols.Count; i++)
                _cols[i].enabled = g >= 0.999f || (i + 1f) / _cols.Count <= g - 0.02f;
            foreach (var cv in _climbVolumes) cv.enabled = g >= 0.999f;
            if (g <= sproutGrowth + 0.0001f) foreach (var c in _cols) c.enabled = false;
        }
    }
}
