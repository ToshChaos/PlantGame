using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PlantGame.Core;

namespace PlantGame.World
{
    /// <summary>
    /// T16: drop-in sky for any scene (Prefabs/Environment/Atmosphere.prefab). Applies the skybox, ambient gradient, fog and sun
    /// direction, follows the camera with the cloud sea so it has no visible edge, and switches the post effects (bloom, distance blur, vignette, SMAA) with GameSettings.Bloom.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public class Atmosphere : MonoBehaviour
    {
        public Material skybox;
        public Color ambientSky = new Color(0.62f, 0.80f, 0.96f), ambientEquator = new Color(0.957f, 0.973f, 0.965f), ambientGround = new Color(0.373f, 0.62f, 0.55f);
        public Color fogColor = new Color(0.60f, 0.78f, 0.94f);
        public float fogStart = 60f, fogEnd = 420f;
        public Vector3 sunEuler = new Vector3(42f, -35f, 0f);
        public Color sunColor = new Color(1f, 0.93f, 0.8f);
        public float sunIntensity = 1.35f;
        [Tooltip("Follows the main camera on XZ (cloud sea, far islands).")]
        public Transform followRoot;
        public Volume volume;

        Camera _cam, _aaCam;
        Bloom _bloom;
        DepthOfField _blur;
        Vignette _vignette;

        void Awake()
        {
            if (skybox != null) RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky; RenderSettings.ambientEquatorColor = ambientEquator; RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = fogColor; RenderSettings.fogStartDistance = fogStart; RenderSettings.fogEndDistance = fogEnd;
            var sun = RenderSettings.sun;
            if (sun == null) foreach (var l in FindObjectsByType<Light>(FindObjectsInactive.Exclude)) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(sunEuler); sun.color = sunColor; sun.intensity = sunIntensity;
                RenderSettings.sun = sun;
            }
            if (volume != null && volume.profile != null)
            {
                var p = volume.profile;
                p.TryGet(out _bloom); p.TryGet(out _vignette);
                // Soft distance blur: far islands melt into the haze (Gaussian only, cheap; added here so the profile asset needs no rebuild).
                if (!p.TryGet(out _blur)) _blur = p.Add<DepthOfField>(true);
                _blur.mode.Override(DepthOfFieldMode.Gaussian);
                _blur.gaussianStart.Override(90f); _blur.gaussianEnd.Override(380f); _blur.gaussianMaxRadius.Override(1.2f);
                _blur.highQualitySampling.Override(false);
            }
            ApplySettings();
        }

        void OnEnable() { GameSettings.Changed += ApplySettings; }
        void OnDisable() { GameSettings.Changed -= ApplySettings; }
        void ApplySettings()
        {
            bool fx = GameSettings.Bloom;
            if (_bloom != null) _bloom.active = fx;
            if (_blur != null) _blur.active = fx;
            if (_vignette != null) _vignette.active = fx;
            ApplyAntiAliasing();
        }

        /// <summary>SMAA on the main camera with the effects on (skipped on phones: the setting defaults off there anyway).</summary>
        void ApplyAntiAliasing()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            _aaCam = _cam;
            var data = _cam.GetUniversalAdditionalCameraData();
            data.antialiasing = GameSettings.Bloom ? AntialiasingMode.SubpixelMorphologicalAntiAliasing : AntialiasingMode.None;
            data.antialiasingQuality = AntialiasingQuality.High;
        }

        void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            if (_cam != _aaCam) ApplyAntiAliasing();   // the player camera appears after the world is built
            if (followRoot == null) return;
            var p = _cam.transform.position; var t = followRoot.position;
            followRoot.position = new Vector3(p.x, t.y, p.z);
        }
    }
}
