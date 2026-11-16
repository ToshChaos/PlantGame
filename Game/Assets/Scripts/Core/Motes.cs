using UnityEngine;

namespace PlantGame.Core
{
    /// <summary>
    /// Soft dots: the particle materials every effect shares (additive `Mat` for the GrowthVfx bursts, alpha-blended `Soft` for the
    /// motes round seeds, seed sources and grown plants) and a builder for a small looping "ambient" emitter. A handful of particles each, no prefabs, no
    /// texture files. The material and its dot texture are built once per session (DontSave statics), so scene loads never leak a copy.
    /// </summary>
    public static class Motes
    {
        static Material _mat, _soft;
        static Texture2D _dot, _firmDot;

        /// <summary>White additive soft dot (URP Particles/Unlit, kept in builds by M_Keep_Particles); tint it with the particle colour.</summary>
        public static Material Mat
        {
            get
            {
                if (_mat != null) return _mat;
                _mat = RuntimeMats.NewParticles("M_VfxDot", Color.white, additive: true);
                if (_dot == null) _dot = MakeDot();
                _mat.mainTexture = _dot;
                if (_mat.HasProperty("_BaseMap")) _mat.SetTexture("_BaseMap", _dot);
                return _mat;
            }
        }

        /// <summary>A dot with a solid core, alpha blended: coloured motes that still read against a bright sky (the additive glow dot washes out there).</summary>
        public static Material Soft
        {
            get
            {
                if (_soft != null) return _soft;
                _soft = RuntimeMats.NewParticles("M_VfxDotSoft", Color.white);
                if (_firmDot == null) _firmDot = MakeDot(0.3f);
                _soft.mainTexture = _firmDot;
                if (_soft.HasProperty("_BaseMap")) _soft.SetTexture("_BaseMap", _firmDot);
                return _soft;
            }
        }

        /// <summary>core 0: a glow that falls off from the centre. Otherwise opaque out to `core` of the radius, then a soft edge.</summary>
        static Texture2D MakeDot(float core = 0f)
        {
            const int S = 32; var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { name = core > 0f ? "T_VfxDotFirm" : "T_VfxDot", hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(S / 2f, S / 2f)) / (S / 2f);
                float a = Mathf.Clamp01(1f - d); a *= a;
                if (core > 0f) a = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(core, 1f, d));
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            t.Apply(); return t;
        }

        /// <summary>
        /// A looping emitter of slow, twinkling dots inside a box (`box` in metres, in the emitter's own axes), a child of `parent` at
        /// `worldPos`. They drift up at `rise` m/s and wander a little; the colour of each is picked between `a` and `b`. The dots live in
        /// world space, so an emitter riding a bobbing island or a falling seed leaves them behind. Stops simulating while off screen.
        /// </summary>
        public static ParticleSystem Ambient(string name, Transform parent, Vector3 worldPos, Vector3 box, Color a, Color b, float size, float life, float rise, float rate, int max = 24)
        {
            var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = worldPos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.duration = 4f; main.playOnAwake = false; main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.65f, life);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Shape;   // a stretched parent (a bridge) widens the box, not the dots
            main.maxParticles = max;
            main.cullingMode = ParticleSystemCullingMode.Pause;
            var em = ps.emission; em.rateOverTime = rate;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box;
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f); vel.y = new ParticleSystem.MinMaxCurve(rise * 0.5f, rise); vel.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.12f; noise.frequency = 0.45f; noise.scrollSpeed = 0.2f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();   // fade in, dim, flare again, fade out: a slow twinkle
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.18f), new GradientAlphaKey(0.35f, 0.5f), new GradientAlphaKey(0.9f, 0.75f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = Soft; r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            ps.Play();
            return ps;
        }
    }
}
