using UnityEngine;

namespace PlantGame.Core
{
    /// <summary>
    /// T04: glow-cyan pulse on the focused object (base colour tint + emission via MaterialPropertyBlock, so shared
    /// materials are never touched). Added on demand by InteractionSensor; disabled when focus leaves.
    /// </summary>
    public class FocusHighlight : MonoBehaviour
    {
        public float tint = 0.45f;
        public float emission = 1.2f;
        public float pulseHz = 1.5f;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        Renderer[] _renderers;
        Color[] _base;
        MaterialPropertyBlock _mpb;

        public static void Set(GameObject go, bool on)
        {
            if (go == null) return;
            var h = go.GetComponent<FocusHighlight>();
            if (h == null) { if (!on) return; h = go.AddComponent<FocusHighlight>(); }
            h.enabled = on;
        }

        void OnEnable()
        {
            _mpb ??= new MaterialPropertyBlock();
            _renderers = GetComponentsInChildren<Renderer>();
            _base = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var m = _renderers[i].sharedMaterial;
                _base[i] = m != null && m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : Color.white;
            }
        }

        void OnDisable()
        {
            if (_renderers == null) return;
            foreach (var r in _renderers) if (r != null) r.SetPropertyBlock(null);
        }

        void Update()
        {
            float k = 0.5f + 0.5f * Mathf.Sin(Time.time * pulseHz * Mathf.PI * 2f);
            for (int i = 0; i < _renderers.Length; i++)
            {
                var r = _renderers[i];
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetColor(BaseColorId, Color.Lerp(_base[i], Palette.TextPurple, tint * (0.5f + 0.5f * k)));
                _mpb.SetColor(EmissionId, Palette.TextPurple * (emission * k));
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
