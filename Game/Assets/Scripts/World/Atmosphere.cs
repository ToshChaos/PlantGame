using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PlantGame.Core;

namespace PlantGame.World
{
    /// <summary>
    /// T16: drop-in sky for any scene (Prefabs/Environment/Atmosphere.prefab). Applies the skybox, ambient gradient, fog and sun
    /// direction, follows the camera with the cloud sea so it has no visible edge, and switches Bloom with GameSettings.Bloom.
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

        Camera _cam;
        Bloom _bloom;

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
            if (volume != null && volume.profile != null) volume.profile.TryGet(out _bloom);
            ApplySettings();
        }

        void OnEnable() { GameSettings.Changed += ApplySettings; }
        void OnDisable() { GameSettings.Changed -= ApplySettings; }
        void ApplySettings() { if (_bloom != null) _bloom.active = GameSettings.Bloom; }

        void LateUpdate()
        {
            if (followRoot == null) return;
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            var p = _cam.transform.position; var t = followRoot.position;
            followRoot.position = new Vector3(p.x, t.y, p.z);
        }
    }
}
