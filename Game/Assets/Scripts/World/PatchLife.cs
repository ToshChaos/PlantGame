using System.Collections.Generic;
using UnityEngine;

namespace PlantGame.World
{
    /// <summary>
    /// Life for a seed source (bamboo grove, bean poles, puffball log, pumpkin patch): its hanging fruit and leaves sway as if in a
    /// light breeze, its puffballs breathe, its pumpkin faces flicker like candles. It finds the pieces by the names the patch builders
    /// in Scenery give them ("Fruit*", "Bean", "Leaf", "Puffball*", "Pumpkin*"), each on its own rhythm, and only works while the
    /// camera is near. High tier only (WorldGenerator.SeedSource): on Medium / Low the pieces are merged into one static mesh
    /// (WorldOptimizer), so nothing here may move them.
    /// </summary>
    public class PatchLife : MonoBehaviour
    {
        [Tooltip("Sway of hanging fruit and leaves, degrees (0 = none).")] public float sway = 5f;
        [Tooltip("Puffballs swell and shrink by this fraction (0 = none).")] public float breathe = 0.03f;
        [Tooltip("Pumpkin faces flicker by this fraction of their glow (0 = none).")] public float flicker = 0.3f;
        public float activeDistance = 32f;

        enum Kind : byte { Sway, Breathe, Flicker }
        struct Piece { public Transform t; public Kind kind; public Vector3 pos, scale; public Quaternion rot; public float phase; public Renderer renderer; public Color emission; }

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        readonly List<Piece> _pieces = new List<Piece>();
        MaterialPropertyBlock _mpb; Transform _cam; bool _rested = true;

        void Start()
        {
            int n = 0;
            foreach (Transform c in transform)
            {
                string name = c.name; Kind kind;
                if (sway > 0f && (name.StartsWith("Fruit") || name == "Bean" || name == "Leaf")) kind = Kind.Sway;
                else if (breathe > 0f && name.StartsWith("Puffball")) kind = Kind.Breathe;
                else if (flicker > 0f && name.StartsWith("Pumpkin")) kind = Kind.Flicker;
                else continue;
                var p = new Piece { t = c, kind = kind, pos = c.localPosition, rot = c.localRotation, scale = c.localScale, phase = Scenery.Hash01(n++, 977) * 6.2832f };
                if (kind == Kind.Flicker)
                {
                    p.renderer = c.GetComponent<Renderer>(); var m = p.renderer != null ? p.renderer.sharedMaterial : null;
                    if (m == null || !m.HasProperty(EmissionId) || !m.IsKeywordEnabled("_EMISSION")) continue;
                    p.emission = m.GetColor(EmissionId);
                }
                _pieces.Add(p);
            }
            if (_pieces.Count == 0) { enabled = false; return; }
            _mpb = new MaterialPropertyBlock();
        }

        void Update()
        {
            if (_cam == null) { var cam = Camera.main; if (cam == null) return; _cam = cam.transform; }
            bool near = (_cam.position - transform.position).sqrMagnitude < activeDistance * activeDistance;
            if (!near)
            {
                if (!_rested) { Rest(); _rested = true; }   // left alone in its built pose
                return;
            }
            _rested = false;
            float time = Time.time, gust = 0.65f + 0.35f * Mathf.Sin(time * 0.31f + transform.position.x);   // the breeze comes and goes
            for (int i = 0; i < _pieces.Count; i++)
            {
                var p = _pieces[i]; if (p.t == null) continue;
                switch (p.kind)
                {
                    case Kind.Sway:
                    {
                        float a = Mathf.Sin(time * 1.15f + p.phase) * gust, b = Mathf.Sin(time * 0.83f + p.phase * 1.7f) * gust;
                        p.t.localRotation = p.rot * Quaternion.Euler(a * sway, 0f, b * sway);
                        p.t.localPosition = p.pos + new Vector3(a, 0f, b) * (sway * 0.003f);   // hanging things swing a little as well as turn
                        break;
                    }
                    case Kind.Breathe:
                        p.t.localScale = p.scale * (1f + breathe * Mathf.Sin(time * 0.9f + p.phase));
                        break;
                    case Kind.Flicker:
                    {
                        float n = Mathf.PerlinNoise(time * 2.6f + p.phase, p.phase);   // 0..1, wandering like a candle flame
                        p.renderer.GetPropertyBlock(_mpb);
                        _mpb.SetColor(EmissionId, p.emission * (1f + flicker * (n * 2f - 1f)));
                        p.renderer.SetPropertyBlock(_mpb);
                        break;
                    }
                }
            }
        }

        void Rest()
        {
            foreach (var p in _pieces)
            {
                if (p.t == null) continue;
                if (p.kind == Kind.Flicker) { p.renderer.SetPropertyBlock(null); continue; }
                p.t.localPosition = p.pos; p.t.localRotation = p.rot; p.t.localScale = p.scale;
            }
        }
    }
}
