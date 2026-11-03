using UnityEngine;
using UnityEngine.Rendering;

namespace PlantGame.Core
{
    /// <summary>
    /// Materials made in code at runtime (effects, tints, generated props). A player build only carries a shader, and only the keyword variants,
    /// that some material asset in the build uses: the M_Keep_* materials (MaterialBuilder, listed in Resources/WorldKit) keep URP Particles/Unlit
    /// and the transparent Lit variants alive, so Shader.Find never comes back null in a build. Add a keeper there when a new runtime keyword combo appears.
    /// </summary>
    public static class RuntimeMats
    {
        public const string LitShader = "Universal Render Pipeline/Lit", UnlitShader = "Universal Render Pipeline/Unlit", ParticlesShader = "Universal Render Pipeline/Particles/Unlit";
        static Shader _lit, _unlit, _particles;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor"), Color_ = Shader.PropertyToID("_Color"), Surface = Shader.PropertyToID("_Surface");

        public static Shader Lit => _lit != null ? _lit : _lit = Find(LitShader);
        public static Shader Unlit => _unlit != null ? _unlit : _unlit = Find(UnlitShader);
        public static Shader Particles => _particles != null ? _particles : _particles = Find(ParticlesShader);

        static Shader Find(string name)
        {
            var s = Shader.Find(name);
            if (s == null) { Debug.LogError($"[PlantGame] Shader '{name}' is missing from the build (rebuild materials and the World Kit)."); s = Shader.Find("Hidden/InternalErrorShader"); }
            return s;
        }

        /// <summary>A new, unsaved material on the given shader, tinted (both _BaseColor and _Color, so it also reads on a fallback shader).</summary>
        public static Material New(Shader shader, string name, Color tint)
        {
            var m = new Material(shader) { name = name, hideFlags = HideFlags.DontSave };
            m.SetColor(BaseColor, tint); m.SetColor(Color_, tint);
            return m;
        }

        public static Material NewLit(string name, Color tint) => New(Lit, name, tint);

        /// <summary>URP surface switch to transparent: alpha blending, or additive (SrcAlpha, One). No-op on shaders without a _Surface property.</summary>
        public static void SetTransparent(Material m, bool additive = false)
        {
            if (!m.HasProperty(Surface)) return;
            m.SetFloat(Surface, 1f); m.SetFloat("_Blend", additive ? 2f : 0f); m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)(additive ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            m.SetOverrideTag("RenderType", "Transparent"); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = (int)RenderQueue.Transparent;
        }

        /// <summary>A double-sided transparent particle material (URP Particles/Unlit).</summary>
        public static Material NewParticles(string name, Color tint, bool additive = false)
        {
            var m = New(Particles, name, tint);
            SetTransparent(m, additive); m.SetFloat("_Cull", 0f);
            return m;
        }
    }
}
