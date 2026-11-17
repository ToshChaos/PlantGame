using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Plants
{
    /// <summary>
    /// A planted plant: shows a sprout, then grows by sweeping the shader's _Growth
    /// (M_PlantGrow, vertex colour R = growth_t) and enables the COL_* collision boxes base-to-tip as the growth passes them.
    /// The prefabs are built from the baked Blender variants by the Bridge / Ladder / Platform / Collector prefab builders.
    /// </summary>
    public class PlantInstance : MonoBehaviour
    {
        static readonly int GrowthId = Shader.PropertyToID("_Growth"), FlowerColorId = Shader.PropertyToID("_FlowerColor"), SquashId = Shader.PropertyToID("_Squash");

        public float sproutGrowth = 0.08f;
        public float growSeconds = 6f;
        public AnimationCurve curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        /// <summary>Static hooks for audio / VFX: a plant was sprouted, started growing, finished growing.</summary>
        public static event System.Action<PlantInstance> Sprouted, GrowthStarted, Grown;

        public PlantKind kind;
        public float Growth { get; private set; }
        public bool IsGrown { get; private set; }
        public bool IsGrowing { get; private set; }

        static readonly List<PlantInstance> _all = new List<PlantInstance>();
        /// <summary>All active plants (touch: grown bridges get a circle at each end).</summary>
        public static IReadOnlyList<PlantInstance> All => _all;
        void OnEnable() { _all.Add(this); GameEvents.PlayerLanded += OnPlayerLanded; }
        void OnDisable() { _all.Remove(this); GameEvents.PlayerLanded -= OnPlayerLanded; }

        /// <summary>This plant's own flower colour (the shader's yellow-flower parts), kept in the property block next to the growth.</summary>
        public void SetFlowerColor(Color c)
        {
            foreach (var r in _renderers) { if (r == null) continue; r.GetPropertyBlock(_mpb); _mpb.SetColor(FlowerColorId, c); r.SetPropertyBlock(_mpb); }
        }

        // a mushroom cap gives under the player's landing and springs back (the shader dips the cap round the point; colliders stay put)
        const float SquashDepth = 0.1f, SquashSeconds = 0.9f;
        Vector3 _squashAt; float _squashT = -1f;
        static readonly List<Vector3> _capTops = new List<Vector3>(); static readonly List<float> _capRadii = new List<float>();

        void OnPlayerLanded()
        {
            if (kind != PlantKind.Platform || !IsGrown) return;
            var pc = PlantGame.Player.PlayerController.Instance; if (pc == null) return;
            Vector3 p = pc.transform.position;
            CapTops(_capTops, _capRadii);
            for (int i = 0; i < _capTops.Count; i++)
            {
                var d = p - _capTops[i];
                if (new Vector2(d.x, d.z).magnitude > _capRadii[i] + 0.4f || Mathf.Abs(d.y) > 0.8f) continue;
                _squashAt = transform.InverseTransformPoint(new Vector3(p.x, _capTops[i].y, p.z)); _squashT = 0f;
                return;
            }
        }

        void Squash()
        {
            _squashT += Time.deltaTime / SquashSeconds;
            float depth = _squashT >= 1f ? 0f : SquashDepth * 1.6f * Mathf.Exp(-4.5f * _squashT) * Mathf.Sin(_squashT * 11f);   // eases down (deepest after a tenth of a second), then a couple of rebounds
            if (_squashT >= 1f) _squashT = -1f;
            var v = new Vector4(_squashAt.x, _squashAt.y, _squashAt.z, depth);
            foreach (var r in _renderers) { if (r == null) continue; r.GetPropertyBlock(_mpb); _mpb.SetVector(SquashId, v); r.SetPropertyBlock(_mpb); }
            SinkPlayer(depth);
        }

        // the player standing on the dipping cap goes down (and up) with it: the shader's own falloff, sampled under the player's feet
        void SinkPlayer(float depth)
        {
            var pc = PlantGame.Player.PlayerController.Instance;
            if (pc == null || depth == 0f || pc.IsClimbing) return;
            Vector3 p = transform.InverseTransformPoint(pc.transform.position);
            if (Mathf.Abs(p.y - _squashAt.y) > 0.3f) return;   // feet on the cap (not the grounded state: it drops while time is stopped, so a pause would pop the model up)
            float t = Mathf.Clamp01(1f - new Vector2(p.x - _squashAt.x, p.z - _squashAt.z).magnitude / 1.6f);   // smoothstep(1.6, 0, d) in PlantGrow.shader
            pc.SinkModel(depth * t * t * (3f - 2f * t) * transform.lossyScale.y);
        }

        Transform _anchor, _tip;
        /// <summary>Bridge ends (world space) once looked up: Anchor at the planting island, Tip at the far one.</summary>
        public bool TryGetEnds(out Vector3 anchor, out Vector3 tip)
        {
            if (_anchor == null || _tip == null)
                foreach (var t in GetComponentsInChildren<Transform>(true)) { if (t.name == "Anchor") _anchor = t; else if (t.name == "Tip") _tip = t; }
            anchor = _anchor != null ? _anchor.position : default; tip = _tip != null ? _tip.position : default;
            return _anchor != null && _tip != null;
        }

        /// <summary>Platform plants: the standing point on top of each walkable cap, base to tip (world space), once fully grown.</summary>
        public int CapTops(List<Vector3> into, List<float> radii = null)
        {
            into.Clear(); radii?.Clear();
            if (kind != PlantKind.Platform || !IsGrown) return 0;
            foreach (var c in _cols)
            {
                var b = c.bounds;
                into.Add(new Vector3(b.center.x, b.max.y, b.center.z));
                radii?.Add(Mathf.Max(b.extents.x, b.extents.z));
            }
            return into.Count;
        }

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

        public void ShowSprout() { IsGrowing = false; IsGrown = false; _t = 0; _sprout = true; _baseScale = transform.localScale; SetGrowth(sproutGrowth); Sprouted?.Invoke(this); }

        /// <summary>Skip the sprout and growth: the plant stands fully grown (level-authored plants).</summary>
        public void GrowInstantly() { EndSprout(); IsGrowing = false; IsGrown = true; _t = 1f; SetGrowth(1f); }

        public void StartGrowing() { if (!IsGrown && !IsGrowing) { EndSprout(); IsGrowing = true; GrowthStarted?.Invoke(this); } }

        // a sprout waiting for water bounces twice every few seconds, so it reads as alive and asking
        bool _sprout; Vector3 _baseScale; float _sproutPhase;
        const float SproutEvery = 2.8f, SproutBounce = 0.16f;

        void EndSprout() { if (_sprout) { _sprout = false; transform.localScale = _baseScale; } }

        void Update()
        {
            if (_sprout && !IsGrowing && !IsGrown)
            {
                if (_renderers.Length > 0 && _renderers[0] != null && !_renderers[0].isVisible) return;
                _sproutPhase = Mathf.Repeat(_sproutPhase + Time.deltaTime / SproutEvery, 1f);
                float q = _sproutPhase / 0.3f;   // the two bounces take the first 30 % of the cycle, then it stands still
                float bump = q < 1f ? Mathf.Abs(Mathf.Sin(q * Mathf.PI * 2f)) * (1f - q) : 0f;
                transform.localScale = _baseScale * (1f + SproutBounce * bump);
                return;
            }
            if (_squashT >= 0f) Squash();
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
