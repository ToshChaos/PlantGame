using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.World
            {
    /// <summary>
    /// Richer surfaces, on two independent switches.
    /// "Postprocessing" (GameSettings.Bloom) = filtering and shaders:
    ///  - 16x anisotropic + trilinear on the island, rock and water textures (sharper at grazing angles, which is how the camera sees an island top),
    ///    and the global anisotropic switch turned on when the tier has it off;
    ///  - the grass blade sprites (<c>Overgrowth.SetBladeFilter</c>): trilinear, 16x anisotropic and coverage-preserving mips, so distant tufts keep their blades instead of thinning out;
    ///  - water: the global <c>_PG_Fx</c> switches on the extra water shader work (rippled sun glints, sky reflection, foam streaks).
    /// The High quality tier (GameSettings.Quality 2) = the textures themselves:
    ///  - surface detail: a second, finer, generated detail layer (URP Lit detail albedo + detail normal) on the island tops and rocks:
    ///    blade streaks and clumps on grass, grain on mud, cracked stone and grit on rock, kept soft and matte (no extra sheen: only water is reflective);
    /// The kit materials are shared assets, so each one is snapshotted before it is changed and restored when the setting goes off or the game
    /// ends (the editor must not save a play-mode tweak). The detail keyword is carried into builds by <c>M_Keep_LitDetail</c> (MaterialBuilder).
    /// </summary>
    public sealed class SurfaceFx : MonoBehaviour
    {
        enum Look { Grass, Mud, Rock }

        struct Entry { public string name; public Look look; public float tile; public float smooth; }
        static readonly Entry[] Targets =
        {
            new Entry { name = "M_Terrain",       look = Look.Grass, tile = 7f },
            new Entry { name = "M_TerrainAutumn", look = Look.Grass, tile = 7f },
            new Entry { name = "M_TerrainMud",    look = Look.Mud,   tile = 6f },
            new Entry { name = "M_TerrainRock",   look = Look.Rock,  tile = 5f, smooth = 0.06f },
            new Entry { name = "M_Stone",         look = Look.Rock,  tile = 4f },
            new Entry { name = "M_RockLoose",     look = Look.Rock,  tile = 4f },
        };
        static readonly string[] TextureProps = { "_BaseMap", "_BumpMap", "_Ripple" };
        static readonly int WaterFx = Shader.PropertyToID("_PG_Fx");

        static SurfaceFx _inst;
        bool _filterOn, _detailOn;
        bool _forced;
        readonly List<(Material live, Material backup, bool hadDetail, bool specOff, bool envOff)> _mats = new List<(Material, Material, bool, bool, bool)>();
        readonly Dictionary<Texture, (int aniso, FilterMode filter)> _tex = new Dictionary<Texture, (int, FilterMode)>();
        readonly Dictionary<Look, (Texture2D albedo, Texture2D normal)> _detail = new Dictionary<Look, (Texture2D, Texture2D)>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            Shader.SetGlobalFloat(WaterFx, 0f);   // statics survive when domain reload is off
            // No HideFlags.DontSave: such an object outlives play mode, so every session would add one more copy, each snapshotting the
            // materials the others had already changed. Clear any copy left by an older run, then make one that dies with the player.
            foreach (var old in Resources.FindObjectsOfTypeAll<SurfaceFx>()) if (old != null) { old.OnDisable(); if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject); }
            var go = new GameObject("SurfaceFx");
            DontDestroyOnLoad(go);
            _inst = go.AddComponent<SurfaceFx>();
        }

        void OnEnable() { GameSettings.Changed += Apply; Apply(); }
        void OnDisable() { GameSettings.Changed -= Apply; RevertFilter(); RevertDetail(); }
        void OnApplicationQuit() { RevertFilter(); RevertDetail(); }
        void OnDestroy() { RevertFilter(); RevertDetail(); foreach (var d in _detail) { if (d.Value.albedo != null) Kill(d.Value.albedo); if (d.Value.normal != null) Kill(d.Value.normal); } _detail.Clear(); }

        /// <summary>Postprocessing switches filtering and shaders; the High quality tier switches the surface textures.</summary>
        void Apply()
        {
            bool filter = GameSettings.Bloom, detail = GameSettings.Quality >= 2;
            if (filter != _filterOn) { if (filter) EngageFilter(); else RevertFilter(); }
            if (detail != _detailOn) { if (detail) EngageDetail(); else RevertDetail(); }
        }

        void EngageFilter()
        {
            _filterOn = true;
            _forced = QualitySettings.anisotropicFiltering == AnisotropicFiltering.Disable;   // per-texture levels only count when the global switch is not Disable
            if (_forced) QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
            foreach (var t in Targets)
            {
                var m = Kit.Mat(t.name);
                if (m == null) continue;
                foreach (var p in TextureProps) Sharpen(m, p);
            }
            var kit = Kit.Asset;
            if (kit != null && kit.materials != null)
                foreach (var m in kit.materials) if (m != null && m.shader != null && m.shader.name == "PlantGame/OvergrowthWater") Sharpen(m, "_Ripple");
            Shader.SetGlobalFloat(WaterFx, 1f);
            Overgrowth.SetBladeFilter(true);   // the grass blade sprites
        }

        void RevertFilter()
        {
            if (!_filterOn) return;
            _filterOn = false;
            Shader.SetGlobalFloat(WaterFx, 0f);
            Overgrowth.SetBladeFilter(false);
            if (_forced && QualitySettings.anisotropicFiltering == AnisotropicFiltering.Enable) QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            _forced = false;
            foreach (var kv in _tex) if (kv.Key != null) { kv.Key.anisoLevel = kv.Value.aniso; kv.Key.filterMode = kv.Value.filter; }
            _tex.Clear();
        }

        void EngageDetail()
        {
            _detailOn = true;
            foreach (var t in Targets)
            {
                var m = Kit.Mat(t.name);
                if (m == null || !m.HasProperty("_DetailAlbedoMap")) continue;
                bool had = m.IsKeywordEnabled("_DETAIL_MULX2");
                _mats.Add((m, new Material(m) { hideFlags = HideFlags.DontSave }, had, m.IsKeywordEnabled("_SPECULARHIGHLIGHTS_OFF"), m.IsKeywordEnabled("_ENVIRONMENTREFLECTIONS_OFF")));
                var (alb, nrm) = Detail(t.look);
                m.SetTexture("_DetailAlbedoMap", alb); m.SetTexture("_DetailNormalMap", nrm);
                m.SetTextureScale("_DetailAlbedoMap", new Vector2(t.tile, t.tile)); m.SetTextureScale("_DetailNormalMap", new Vector2(t.tile, t.tile));
                m.SetFloat("_DetailAlbedoMapScale", t.look == Look.Grass ? 0.4f : 0.35f);
                m.SetFloat("_DetailNormalMapScale", t.look == Look.Rock ? 0.4f : 0.3f);
                m.EnableKeyword("_DETAIL_MULX2"); m.EnableKeyword("_NORMALMAP");
                if (t.smooth > 0f)   // the grey rock island is lightly reflective on High (the asset is matte; about half its old shine)
                {
                    m.SetFloat("_Smoothness", t.smooth);
                    m.SetFloat("_SpecularHighlights", 1f); m.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
                    m.SetFloat("_EnvironmentReflections", 1f); m.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                }
            }
        }

        void RevertDetail()
        {
            if (!_detailOn) return;
            _detailOn = false;
            foreach (var (live, backup, had, specOff, envOff) in _mats)
            {
                if (live != null)
                {
                    live.CopyPropertiesFromMaterial(backup);
                    if (had) live.EnableKeyword("_DETAIL_MULX2"); else live.DisableKeyword("_DETAIL_MULX2");
                    if (specOff) live.EnableKeyword("_SPECULARHIGHLIGHTS_OFF"); else live.DisableKeyword("_SPECULARHIGHLIGHTS_OFF");
                    if (envOff) live.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF"); else live.DisableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                }
                if (backup != null) Kill(backup);
            }
            _mats.Clear();
        }

        static void Kill(Object o) { if (Application.isPlaying) Destroy(o); else DestroyImmediate(o); }   // a revert can also run while the editor leaves play mode

        void Sharpen(Material m, string prop)
        {
            if (m == null || !m.HasProperty(prop)) return;
            var t = m.GetTexture(prop);
            if (t == null || _tex.ContainsKey(t)) return;
            _tex[t] = (t.anisoLevel, t.filterMode);
            t.anisoLevel = 16;
            if (t.filterMode == FilterMode.Bilinear) t.filterMode = FilterMode.Trilinear;
        }

        // ---- generated detail layers (tileable, mean grey so the "x2" multiply leaves the base colour alone) ----

        (Texture2D, Texture2D) Detail(Look look)
        {
            if (_detail.TryGetValue(look, out var d) && d.albedo != null) return d;
            const int N = 128;
            var h = new float[N * N];
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float u = x / (float)N, v = y / (float)N, val;
                    switch (look)
                    {
                        case Look.Grass:   // vertical blade streaks, broken into clumps, with bright tips
                            val = 0.55f * Fbm(u, v, 40, 5, 2, 11) + 0.25f * Fbm(u, v, 9, 9, 3, 12) + 0.20f * Fbm(u, v, 96, 24, 1, 13);
                            break;
                        case Look.Mud:     // soft cracked clay with grit
                            val = 0.6f * Fbm(u, v, 6, 6, 4, 21) + 0.25f * Crack(u, v, 7, 22) + 0.15f * Fbm(u, v, 64, 64, 1, 23);
                            break;
                        default:           // cracked stone with grain
                            val = 0.55f * Crack(u, v, 6, 31) + 0.30f * Fbm(u, v, 10, 10, 4, 32) + 0.15f * Fbm(u, v, 64, 64, 1, 33);
                            break;
                    }
                    h[y * N + x] = val;
                }
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var f in h) { if (f < lo) lo = f; if (f > hi) hi = f; }
            float mean = 0; foreach (var f in h) mean += (f - lo) / Mathf.Max(1e-4f, hi - lo); mean /= h.Length;   // centre on 0.5 so the x2 multiply keeps the base brightness
            float contrast = look == Look.Grass ? 0.62f : 0.7f;
            var alb = new Texture2D(N, N, TextureFormat.RGBA32, true, false) { name = "SurfaceFx_Albedo_" + look, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8, hideFlags = HideFlags.DontSave };
            var nrm = new Texture2D(N, N, TextureFormat.RGBA32, true, true) { name = "SurfaceFx_Normal_" + look, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 8, hideFlags = HideFlags.DontSave };
            var ac = new Color32[N * N]; var nc = new Color32[N * N];
            float slope = look == Look.Grass ? 5f : 7f;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float n = (h[y * N + x] - lo) / Mathf.Max(1e-4f, hi - lo);
                    float g = Mathf.Clamp01(0.5f + (n - mean) * contrast);
                    float r = look == Look.Grass ? g * 0.96f : g, b = look == Look.Grass ? g * 0.88f : look == Look.Mud ? g * 0.94f : g * 1.02f;
                    ac[y * N + x] = new Color(r, Mathf.Min(1f, g * (look == Look.Grass ? 1.04f : 1f)), Mathf.Clamp01(b), 1f);
                    float dx = h[y * N + (x + 1) % N] - h[y * N + (x + N - 1) % N], dy = h[((y + 1) % N) * N + x] - h[((y + N - 1) % N) * N + x];
                    var nv = new Vector3(-dx * slope, -dy * slope, 1f).normalized;
                    nc[y * N + x] = new Color(nv.x * 0.5f + 0.5f, nv.y * 0.5f + 0.5f, nv.z * 0.5f + 0.5f, 1f);   // a = 1: both the DXT5nm and the plain unpack read the same x
                }
            alb.SetPixels32(ac); alb.Apply(true); nrm.SetPixels32(nc); nrm.Apply(true);
            return _detail[look] = (alb, nrm);
        }

        static int Mod(int a, int p) { int r = a % p; return r < 0 ? r + p : r; }
        static float Hash(int x, int y, int s)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + s * 1274126177);
                h = (h ^ (h >> 13)) * 1274126177u; h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
        /// <summary>Value noise that repeats every px x py lattice cells.</summary>
        static float VNoise(float u, float v, int px, int py, int s)
        {
            float x = u * px, y = v * py; int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0; fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            int X0 = Mod(x0, px), X1 = Mod(x0 + 1, px), Y0 = Mod(y0, py), Y1 = Mod(y0 + 1, py);
            return Mathf.Lerp(Mathf.Lerp(Hash(X0, Y0, s), Hash(X1, Y0, s), fx), Mathf.Lerp(Hash(X0, Y1, s), Hash(X1, Y1, s), fx), fy);
        }
        static float Fbm(float u, float v, int px, int py, int octaves, int s)
        {
            float sum = 0, amp = 0.5f, norm = 0;
            for (int o = 0; o < octaves; o++) { sum += amp * VNoise(u, v, px << o, py << o, s + o * 7); norm += amp; amp *= 0.5f; }
            return sum / norm;
        }
        /// <summary>Cell edges of a tileable Worley pattern: 0 on the cracks, rising to ~1 inside the cells.</summary>
        static float Crack(float u, float v, int cells, int s)
        {
            float x = u * cells, y = v * cells; int cx = Mathf.FloorToInt(x), cy = Mathf.FloorToInt(y);
            float f1 = 9f, f2 = 9f;
            for (int j = -1; j <= 1; j++)
                for (int i = -1; i <= 1; i++)
                {
                    int gx = cx + i, gy = cy + j; int wx = Mod(gx, cells), wy = Mod(gy, cells);
                    float px = gx + Hash(wx, wy, s), py = gy + Hash(wx, wy, s + 99);
                    float d = Mathf.Sqrt((px - x) * (px - x) + (py - y) * (py - y));
                    if (d < f1) { f2 = f1; f1 = d; } else if (d < f2) f2 = d;
                }
            return Mathf.Clamp01((f2 - f1) * 2.2f);
        }
    }
}
