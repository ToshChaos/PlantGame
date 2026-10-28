using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Player
{
    /// <summary>
    /// Scenery (trees, ruins, rocks, shrubs, props) standing between the camera and the player fades to see-through. Candidates are the renderers under
    /// the islands' "Scenery" roots (collected when a world appears); a bounds test against the camera-to-head segment runs ten times a second and each
    /// hit renderer swaps to a transparent clone of its materials, fading its alpha in and out smoothly. Materials without a URP surface switch stay solid.
    /// </summary>
    public class CameraOcclusionFade : MonoBehaviour
    {
        public Transform target;                  // the player's head / chest
        public float fadedAlpha = 0.22f, fadeSpeed = 6f, checkInterval = 0.1f, margin = 0.4f, maxBoundsSize = 22f;

        class State { public Material[] original; public Material[] clones; public float alpha = 1f; public bool wanted; }
        readonly Dictionary<Renderer, State> _active = new Dictionary<Renderer, State>();
        static readonly Dictionary<Material, Material> Clones = new Dictionary<Material, Material>();
        readonly List<Renderer> _candidates = new List<Renderer>();
        readonly List<Renderer> _drop = new List<Renderer>();
        float _next, _nextRefresh;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), Color_ = Shader.PropertyToID("_Color"), Surface = Shader.PropertyToID("_Surface");

        void OnEnable() { GameEvents.WorldGenerated += OnWorld; }
        void OnDisable() { GameEvents.WorldGenerated -= OnWorld; RestoreAll(); }
        void OnWorld(int seed) { _nextRefresh = Time.time + 0.5f; }   // the scenery objects exist a moment after the event

        /// <summary>Collects the fadeable renderers (also called by tests).</summary>
        public void Refresh()
        {
            RestoreAll(); _candidates.Clear();
            foreach (var r in FindObjectsByType<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer || !r.enabled) continue;
                if (r.GetComponentInParent<PlayerController>() != null) continue;
                bool scenery = false;
                for (var t = r.transform.parent; t != null; t = t.parent) if (t.name == "Scenery" || t.name == "SideDecor") { scenery = true; break; }
                if (!scenery) continue;
                var b = r.bounds; if (Mathf.Max(b.size.x, b.size.y, b.size.z) > maxBoundsSize) continue;
                _candidates.Add(r);
            }
        }

        void LateUpdate() { Tick(); }

        public void Tick()
        {
            if (target == null) return;
            if (_nextRefresh > 0f && Time.time >= _nextRefresh) { _nextRefresh = 0f; Refresh(); }
            if (_candidates.Count == 0 && _active.Count == 0 && _nextRefresh == 0f && Time.frameCount % 120 == 1) Refresh();   // late joiners, e.g. a world built before this component
            if (Time.time >= _next)
            {
                _next = Time.time + checkInterval;
                foreach (var kv in _active) kv.Value.wanted = false;
                var from = transform.position; var to = target.position; var dir = to - from; float len = dir.magnitude;
                if (len > 0.05f)
                {
                    var ray = new Ray(from, dir / len);
                    foreach (var r in _candidates)
                    {
                        if (r == null) continue;
                        var b = r.bounds; b.Expand(margin);
                        // only things on the camera's side of the player: the ray must enter the bounds well before the player, and the middle of the object must lie nearer the camera than the player (anything beside or beyond the player stays solid)
                        float centreAlong = Vector3.Dot(b.center - from, ray.direction);
                        bool camInside = b.Contains(from);   // the camera sits inside the crown / ruin: always see-through, whatever side its middle is on
                        if (!camInside && (!b.IntersectRay(ray, out float d) || d > len - 0.3f || centreAlong > len - 0.2f)) { if (_active.TryGetValue(r, out var st0)) st0.wanted = false; continue; }
                        if (!_active.TryGetValue(r, out var st)) { st = Begin(r); if (st == null) continue; _active[r] = st; }
                        st.wanted = true;
                    }
                }
            }
            _drop.Clear();
            foreach (var kv in _active)
            {
                var st = kv.Value; var r = kv.Key;
                if (r == null) { Release(st); _drop.Add(r); continue; }
                st.alpha = Mathf.MoveTowards(st.alpha, st.wanted ? fadedAlpha : 1f, fadeSpeed * Time.deltaTime);
                foreach (var m in st.clones) if (m != null) SetAlpha(m, st.alpha);
                if (!st.wanted && st.alpha >= 0.999f) { r.sharedMaterials = st.original; Release(st); _drop.Add(r); }
            }
            foreach (var r in _drop) _active.Remove(r);
        }

        State Begin(Renderer r)
        {
            var orig = r.sharedMaterials; var cl = new Material[orig.Length];
            bool any = false;
            for (int i = 0; i < orig.Length; i++)
            {
                if (orig[i] == null || !orig[i].HasProperty(Surface)) { cl[i] = orig[i]; continue; }
                if (!Clones.TryGetValue(orig[i], out var c) || c == null) { c = MakeTransparent(orig[i]); Clones[orig[i]] = c; }
                cl[i] = new Material(c) { hideFlags = HideFlags.HideAndDontSave };   // own copy per renderer: alpha is set per instance
                any = true;
            }
            if (!any) return null;
            r.sharedMaterials = cl;
            return new State { original = orig, clones = cl };
        }

        static Material MakeTransparent(Material src)
        {
            var m = new Material(src) { name = src.name + "_Fade", hideFlags = HideFlags.HideAndDontSave };
            RuntimeMats.SetTransparent(m);   // build variants: MaterialBuilder's M_Keep_LitFade* materials
            m.SetFloat("_AlphaClip", 0f); m.DisableKeyword("_ALPHATEST_ON");
            m.SetFloat("_AlphaToMask", 0f);   // leaf cards carry alpha-to-coverage: on Metal without MSAA it dithers into a noisy pattern under alpha blending
            m.SetShaderPassEnabled("ShadowCaster", true);
            return m;
        }

        static void SetAlpha(Material m, float a)
        {
            if (m.HasProperty(BaseColor)) { var c = m.GetColor(BaseColor); c.a = a; m.SetColor(BaseColor, c); }
            else if (m.HasProperty(Color_)) { var c = m.GetColor(Color_); c.a = a; m.SetColor(Color_, c); }
        }

        void RestoreAll()
        {
            foreach (var kv in _active) { if (kv.Key != null) kv.Key.sharedMaterials = kv.Value.original; Release(kv.Value); }
            _active.Clear();
        }

        /// <summary>Destroys a renderer's own fade copies once it is solid again (the shared transparent templates in Clones stay).</summary>
        static void Release(State st)
        {
            for (int i = 0; i < st.clones.Length; i++)
                if (st.clones[i] != null && st.clones[i] != st.original[i]) { if (Application.isPlaying) Destroy(st.clones[i]); else DestroyImmediate(st.clones[i]); }
        }
    }
}
