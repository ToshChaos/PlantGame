using UnityEngine;
using PlantGame.Core;
using PlantGame.Player;

namespace PlantGame.World
{
    /// <summary>
    /// Weather / season of a level: re-colours the skybox (a runtime copy, the asset is never touched), fog, ambient light, sun and the
    /// cloud layers, switches the golden butterflies off, and runs the particle effects: rain streaks (Rain) or leaves blown by the wind
    /// (Autumn). The particle boxes follow the player. Created by the WorldGenerator; music and rain ambience come from AudioManager.
    /// </summary>
    [DefaultExecutionOrder(-30)]
    public class WeatherController : MonoBehaviour
    {
        LevelTheme _theme;
        bool _started;
        Transform _fxRoot, _player;
        Material _particleMat;

        bool _wind;
        /// <summary>The blown-leaves particle system of windy levels (WindGusts steers it), null otherwise.</summary>
        public ParticleSystem WindLeaves { get; private set; }
        public void Apply(LevelTheme t, bool wind = false) { _theme = t; _wind = wind; if (_started) Setup(); }
        void Start() { _started = true; Setup(); }

        ParticleSystem _rain;
        bool _showering; float _intensity, _switchAt;
        const float RainSeconds = 30f, DrySeconds = 30f;   // the cycle always starts with rain

        void Setup()
        {
            _rain = null; WeatherState.Set(false, false, 0f);
            if (_fxRoot != null) Destroy(_fxRoot.gameObject);
            var atm = FindAnyObjectByType<Atmosphere>();
            var bf = FindAnyObjectByType<GoldenButterflies>();
            if (bf != null) bf.gameObject.SetActive(_theme == LevelTheme.Sunny);   // no butterflies in rain or autumn
            WindLeaves = null;
            if (_theme == LevelTheme.Sunny)
            {
                if (_wind) { _fxRoot = new GameObject("WeatherFx").transform; _fxRoot.SetParent(transform, false); Leaves(); }   // T50: blown leaves show the wind
                return;
            }

            bool rain = _theme == LevelTheme.Rain, snow = _theme == LevelTheme.Snow, fog = _theme == LevelTheme.Fog, sunset = _theme == LevelTheme.Sunset, sunrise = _theme == LevelTheme.Sunrise;
            // sky, fog, ambient, sun
            Color horizon, mid, sky, zenith, sun; float sunSize, sunGlow, sunIntensity, fogEnd;
            Color ambSky, ambEq, ambGround, cloud, shade;
            if (sunset)
            {
                horizon = C(1.0f, 0.55f, 0.30f); mid = C(0.93f, 0.40f, 0.38f); sky = C(0.55f, 0.32f, 0.55f); zenith = C(0.22f, 0.20f, 0.40f);
                sun = C(1f, 0.55f, 0.25f); sunSize = 0.06f; sunGlow = 1.0f; sunIntensity = 1.1f; fogEnd = 340f;
                ambSky = C(0.75f, 0.5f, 0.55f); ambEq = C(0.9f, 0.62f, 0.5f); ambGround = C(0.4f, 0.3f, 0.35f);
                cloud = C(1f, 0.7f, 0.6f); shade = C(0.7f, 0.4f, 0.5f);
            }
            else if (sunrise)
            {
                horizon = C(1.0f, 0.82f, 0.70f); mid = C(0.98f, 0.72f, 0.72f); sky = C(0.72f, 0.78f, 0.95f); zenith = C(0.45f, 0.58f, 0.85f);
                sun = C(1f, 0.86f, 0.65f); sunSize = 0.05f; sunGlow = 0.8f; sunIntensity = 1.1f; fogEnd = 330f;
                ambSky = C(0.78f, 0.8f, 0.95f); ambEq = C(0.95f, 0.85f, 0.82f); ambGround = C(0.45f, 0.42f, 0.45f);
                cloud = C(1f, 0.88f, 0.85f); shade = C(0.8f, 0.7f, 0.8f);
            }
            else if (snow)
            {
                horizon = C(0.90f, 0.94f, 0.98f); mid = C(0.80f, 0.88f, 0.96f); sky = C(0.62f, 0.76f, 0.92f); zenith = C(0.40f, 0.55f, 0.78f);
                sun = C(0.95f, 0.97f, 1f); sunSize = 0.02f; sunGlow = 0.35f; sunIntensity = 0.95f; fogEnd = 260f;
                ambSky = C(0.72f, 0.82f, 0.95f); ambEq = C(0.85f, 0.9f, 0.97f); ambGround = C(0.55f, 0.63f, 0.75f);
                cloud = C(0.96f, 0.98f, 1f); shade = C(0.7f, 0.78f, 0.9f);
            }
            else if (fog)
            {
                horizon = C(0.78f, 0.81f, 0.82f); mid = C(0.74f, 0.77f, 0.79f); sky = C(0.66f, 0.70f, 0.73f); zenith = C(0.55f, 0.60f, 0.64f);
                sun = C(0.85f, 0.87f, 0.88f); sunSize = 0.002f; sunGlow = 0.02f; sunIntensity = 0.55f; fogEnd = 40f;
                ambSky = C(0.62f, 0.66f, 0.69f); ambEq = C(0.68f, 0.71f, 0.73f); ambGround = C(0.4f, 0.43f, 0.45f);
                cloud = C(0.78f, 0.8f, 0.82f); shade = C(0.55f, 0.6f, 0.64f);
            }
            else if (rain)
            {
                horizon = C(0.66f, 0.71f, 0.75f); mid = C(0.52f, 0.59f, 0.66f); sky = C(0.40f, 0.47f, 0.55f); zenith = C(0.25f, 0.31f, 0.39f);
                sun = C(0.78f, 0.84f, 0.92f); sunSize = 0.004f; sunGlow = 0.05f; sunIntensity = 0.65f; fogEnd = 230f;
                ambSky = C(0.5f, 0.57f, 0.64f); ambEq = C(0.6f, 0.65f, 0.7f); ambGround = C(0.3f, 0.36f, 0.4f);
                cloud = C(0.72f, 0.75f, 0.8f); shade = C(0.35f, 0.4f, 0.47f);
            }
            else
            {
                horizon = C(1.0f, 0.89f, 0.70f); mid = C(0.97f, 0.74f, 0.50f); sky = C(0.80f, 0.58f, 0.45f); zenith = C(0.40f, 0.33f, 0.45f);
                sun = C(1f, 0.74f, 0.42f); sunSize = 0.045f; sunGlow = 0.9f; sunIntensity = 1.25f; fogEnd = 380f;
                ambSky = C(0.85f, 0.7f, 0.55f); ambEq = C(0.95f, 0.85f, 0.7f); ambGround = C(0.45f, 0.38f, 0.3f);
                cloud = C(1f, 0.9f, 0.78f); shade = C(0.85f, 0.6f, 0.5f);
            }
            if (RenderSettings.skybox != null)
            {
                var m = new Material(RenderSettings.skybox) { name = "Sky_" + _theme, hideFlags = HideFlags.DontSave };
                m.SetColor("_HorizonColor", horizon); m.SetColor("_MidColor", mid); m.SetColor("_SkyColor", sky); m.SetColor("_ZenithColor", zenith);
                m.SetColor("_GroundColor", mid); m.SetColor("_SunColor", sun); m.SetFloat("_SunSize", sunSize); m.SetFloat("_SunGlow", sunGlow);
                RenderSettings.skybox = m;
            }
            RenderSettings.fogColor = mid; RenderSettings.fogEndDistance = fogEnd;
            if (rain) RenderSettings.fogStartDistance = 30f;
            if (fog) { RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 1f; }   // T50: dense fog, visibility about 40 m
            RenderSettings.ambientSkyColor = ambSky; RenderSettings.ambientEquatorColor = ambEq; RenderSettings.ambientGroundColor = ambGround;
            var light = RenderSettings.sun;
            if (light != null)
            {
                light.color = sun; light.intensity = sunIntensity;
                if (sunset) light.transform.rotation = Quaternion.Euler(9f, -35f, 0f);        // low sun, long shadows
                else if (sunrise) light.transform.rotation = Quaternion.Euler(12f, 130f, 0f);
                else if (!rain && !fog && !snow) light.transform.rotation = Quaternion.Euler(26f, -35f, 0f);
            }
            if (atm != null)
                foreach (var r in atm.GetComponentsInChildren<Renderer>())
                {
                    var sm = r.sharedMaterial;
                    if (sm == null || sm.shader == null || sm.shader.name != "PlantGame/CloudLayer") continue;
                    var inst = r.material;   // per-renderer copy
                    inst.SetColor("_Color", cloud); inst.SetColor("_Shade", shade);
                }

            _fxRoot = new GameObject("WeatherFx").transform;
            _fxRoot.SetParent(transform, false);
            if (rain) { Rain(); _showering = true; _intensity = 1f; _switchAt = Time.time + RainSeconds; WeatherState.Set(true, true, 1f); } else if (snow) Snow(); else if (fog || sunset || sunrise) { } else Leaves();
            if (_wind && WindLeaves == null) Leaves();
        }

        void OnDestroy() { WeatherState.Set(false, false, 0f); }

        void Update()
        {
            if (_rain == null) return;
            if (Time.time >= _switchAt) { _showering = !_showering; _switchAt = Time.time + (_showering ? RainSeconds : DrySeconds); }
            _intensity = Mathf.MoveTowards(_intensity, _showering ? 1f : 0f, Time.deltaTime / 3f);
            var em = _rain.emission; em.rateOverTime = 1800f * _intensity;
            WeatherState.Set(true, _showering, _intensity);
        }

        void LateUpdate()
        {
            if (_fxRoot == null) return;
            if (_player == null) { var pc = PlayerController.Instance; if (pc != null) _player = pc.transform; else return; }
            _fxRoot.position = _player.position;
        }

        static Color C(float r, float g, float b) => new Color(r, g, b, 1f);

        // ------------------------------------------------------------------ rain
        void Rain()
        {
            var ps = NewSystem("Rain", new Vector3(0f, 16f, 0f), ParticleSystemRenderMode.Stretch, ParticleMaterial(null));
            var main = ps.main;
            main.loop = true; main.startLifetime = 0.9f; main.startSpeed = 0f; main.startSize = 0.05f;
            main.startColor = new Color(0.78f, 0.88f, 1f, 0.5f); main.maxParticles = 4000; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 1800f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(44f, 0.1f, 44f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = 2.5f; vel.y = -32f; vel.z = 0f;
            var r = ps.GetComponent<ParticleSystemRenderer>(); r.lengthScale = 1f; r.velocityScale = 0.03f;
            ps.Play();
            _rain = ps;
        }

        // ------------------------------------------------------------------ snow
        void Snow()
        {
            var ps = NewSystem("Snow", new Vector3(0f, 12f, 0f), ParticleSystemRenderMode.Billboard, ParticleMaterial(null));
            var main = ps.main;
            main.loop = true; main.startLifetime = 9f; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
            main.startColor = new Color(1f, 1f, 1f, 0.9f); main.maxParticles = 2500; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 260f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(48f, 4f, 48f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = 0.9f; vel.y = -1.4f; vel.z = 0.3f;
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.9f; noise.frequency = 0.3f; noise.scrollSpeed = 0.3f;
            ps.Play();
        }

        // ------------------------------------------------------------------ autumn leaves
        void Leaves()
        {
            var ps = NewSystem("Leaves", new Vector3(-8f, 6f, 0f), ParticleSystemRenderMode.Billboard, ParticleMaterial(LeafTexture()));
            WindLeaves = ps;
            var main = ps.main;
            main.loop = true; main.startLifetime = 11f; main.startSpeed = 0.2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.42f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = _theme == LevelTheme.Autumn ? new ParticleSystem.MinMaxGradient(new Color(0.96f, 0.6f, 0.14f), new Color(0.78f, 0.2f, 0.1f)) : new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.72f, 0.28f), new Color(0.85f, 0.8f, 0.3f));
            main.maxParticles = 500; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 24f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(50f, 9f, 50f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = 2.8f; vel.y = -0.9f; vel.z = 1.1f;   // the wind
            var noise = ps.noise; noise.enabled = true; noise.strength = 1.6f; noise.frequency = 0.35f; noise.scrollSpeed = 0.4f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            ps.Play();
        }

        // ------------------------------------------------------------------ helpers
        ParticleSystem NewSystem(string name, Vector3 offset, ParticleSystemRenderMode mode, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_fxRoot, false); go.transform.localPosition = offset;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = mode; r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            return ps;
        }

        Material ParticleMaterial(Texture2D tex)
        {
            var m = RuntimeMats.NewParticles("M_WeatherFx", Color.white);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            return m;
        }

        /// <summary>Pointed oval with a centre vein: a leaf.</summary>
        Texture2D LeafTexture()
        {
            const int S = 32;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                float u = (x + 0.5f) / S * 2f - 1f, v = (y + 0.5f) / S * 2f - 1f;   // v along the leaf
                float half = 0.62f * Mathf.Sqrt(Mathf.Max(0f, 1f - v * v)) * (1f - 0.25f * v);
                float a = Mathf.Clamp01((half - Mathf.Abs(u)) * 14f);
                float vein = Mathf.Abs(u) < 0.05f ? 0.75f : 1f;
                t.SetPixel(x, y, new Color(vein, vein, vein, a));
            }
            t.Apply();
            return t;
        }
    }
}
