using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PlantGame.Audio;
using PlantGame.Core;
using PlantGame.Plants;
using PlantGame.World;

namespace PlantGame.Editor
{
    /// <summary>
    /// T15/T16/T17/T18/T20 builder. Atmosphere prefab (sky, cloud sea, cloud cards, far islands, post volume), audio import settings,
    /// and the systems that live in Main (bootstrap, AudioManager, SfxBinder, GrowthVfx).
    /// Menus: PlantGame > 11 Build Atmosphere, PlantGame > 12 Build Main.
    /// </summary>
    public static class MainSceneBuilder
    {
        const string AtmosphereDir = "Assets/Prefabs/Environment", AtmospherePrefab = AtmosphereDir + "/Atmosphere.prefab";
        const string ProfilePath = "Assets/Art/VFX/PostFX_Overgrowth.asset";

        [MenuItem("PlantGame/11 Build Atmosphere", false, 11)]
        public static void BuildAtmosphere()
        {
            Directory.CreateDirectory(AtmosphereDir); Directory.CreateDirectory("Assets/Art/VFX");
            var noise = EnsureCloudTexture();
            var sky = Mat("M_Sky", "PlantGame/OvergrowthSky", m =>
            {
                m.SetColor("_HorizonColor", new Color(0.58f, 0.80f, 0.96f)); m.SetColor("_MidColor", new Color(0.42f, 0.69f, 0.95f));
                m.SetColor("_SkyColor", new Color(0.18f, 0.50f, 0.91f)); m.SetColor("_ZenithColor", new Color(0.08f, 0.28f, 0.70f));
            });
            var sea = Mat("M_CloudSea", "PlantGame/CloudLayer", m =>
            {
                m.SetTexture("_MainTex", noise); m.SetColor("_Color", Color.Lerp(Palette.DeepBlue, Palette.MistBlue, 0.72f)); m.SetColor("_Shade", Color.Lerp(Palette.DeepBlue, Color.black, 0.85f)); m.SetFloat("_FogAmount", 0.3f);   // lower sea: darker, bluer
                m.SetFloat("_Opacity", 0.95f); m.SetFloat("_Tiling", 8f); m.SetFloat("_Coverage", 0.5f);
                m.SetVector("_Scroll", new Vector4(0.006f, 0.002f, 0, 0)); m.SetVector("_Radius", new Vector4(0.3f, 0.5f, 0, 0));
            });
            var sea2 = Mat("M_CloudSeaHigh", "PlantGame/CloudLayer", m =>
            {
                m.SetTexture("_MainTex", noise); m.SetColor("_Color", Palette.CloudWhite); m.SetColor("_Shade", Palette.MistBlue);
                m.SetFloat("_Opacity", 0.55f); m.SetFloat("_Tiling", 14f); m.SetFloat("_Coverage", 0.5f);
                m.SetVector("_Scroll", new Vector4(-0.009f, 0.004f, 0, 0)); m.SetVector("_Radius", new Vector4(0.3f, 0.5f, 0, 0));
            });
            var dark = Mat("M_CloudCardDark", "PlantGame/CloudLayer", m =>
            {
                m.SetTexture("_MainTex", noise); m.SetColor("_Color", Color.Lerp(Palette.DeepBlue, Palette.MistBlue, 0.35f)); m.SetColor("_Shade", Color.Lerp(Palette.DeepBlue, Color.black, 0.8f)); m.SetFloat("_FogAmount", 0.25f);
                m.SetFloat("_Opacity", 1f); m.SetFloat("_Tiling", 2.4f); m.SetFloat("_Coverage", 0.38f);
                m.SetVector("_Scroll", new Vector4(0.0015f, 0f, 0, 0)); m.SetVector("_Radius", new Vector4(0.15f, 0.5f, 0, 0));
            });
            var card = Mat("M_CloudCard", "PlantGame/CloudLayer", m =>
            {
                m.SetTexture("_MainTex", noise); m.SetColor("_Color", Palette.CloudWhite); m.SetColor("_Shade", Palette.MistBlue);
                m.SetFloat("_Opacity", 0.8f); m.SetFloat("_Tiling", 1.3f); m.SetFloat("_Coverage", 0.8f);
                m.SetVector("_Scroll", new Vector4(0.002f, 0f, 0, 0)); m.SetVector("_Radius", new Vector4(0.12f, 0.5f, 0, 0));
            });

            var root = new GameObject("Atmosphere");
            var atm = root.AddComponent<Atmosphere>();
            atm.skybox = sky;

            // cloud sea: two big translucent discs below the islands; they follow the camera on XZ so there is never an edge
            var follow = new GameObject("CloudSea"); follow.transform.SetParent(root.transform, false);
            atm.followRoot = follow.transform;
            Quad("Sea_Low", follow.transform, new Vector3(0, -62f, 0), 1800f, sea, true);
            Quad("Sea_High", follow.transform, new Vector3(0, -44f, 0), 1400f, sea2, true);
            // abyss: translucent deep-blue sheets stacked far below, so gaps in the clouds open onto darkness instead of more sky
            var abyss = Mat("M_Abyss", "Universal Render Pipeline/Unlit", m =>
            {
                m.SetColor("_BaseColor", new Color(0.05f, 0.1f, 0.22f, 0.38f));
                m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_SrcBlend", 5f); m.SetFloat("_DstBlend", 10f); m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent"); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); m.renderQueue = 2990;
            });
            float[] abyssY = { -80f, -100f, -125f, -155f, -190f };
            for (int i = 0; i < abyssY.Length; i++) Quad("Abyss" + i, follow.transform, new Vector3(0, abyssY[i], 0), 1800f, abyss, true);

            // drifting cloud cards around the play space
            var cards = new GameObject("CloudCards"); cards.transform.SetParent(root.transform, false);
            var rnd = new System.Random(16);
            for (int i = 0; i < 40; i++)
            {
                float a = (float)(rnd.NextDouble() * Mathf.PI * 2), r = 80f + (float)rnd.NextDouble() * 160f;
                float w = 80f + (float)rnd.NextDouble() * 110f, y = -25f + (float)rnd.NextDouble() * 150f;
                var q = Quad("Card" + i, cards.transform, new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r), w, card, false);
                q.transform.localScale = new Vector3(w, w * 0.38f, 1f);
                var d = q.AddComponent<CloudDrift>(); d.speed = 0.6f + (float)rnd.NextDouble() * 1.4f;
            }

            // big dark storm banks below the islands (they follow the camera with the sea so the underside is never empty)
            for (int i = 0; i < 26; i++)
            {
                float a = (float)(rnd.NextDouble() * Mathf.PI * 2), r = 20f + (float)rnd.NextDouble() * 170f;
                float w = 220f + (float)rnd.NextDouble() * 260f, y = -26f - (float)rnd.NextDouble() * 22f;
                var q = Quad("Bank" + i, follow.transform, new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r), w, dark, true);
                q.transform.localScale = new Vector3(w, w * 0.6f, 1f);
                var d = q.AddComponent<CloudDrift>(); d.speed = 0.4f + (float)rnd.NextDouble() * 0.8f;
            }

            // distant backdrop islands (kit-free for now: round islands, no colliders, fog tinted by distance)
            var far = new GameObject("FarIslands"); far.transform.SetParent(root.transform, false);
            var terrain = MaterialBuilder.Load("M_Terrain"); var stone = MaterialBuilder.Load("M_Stone");
            for (int i = 0; i < 9; i++)
            {
                float a = i * (Mathf.PI * 2 / 9f) + 0.3f, r = 240f + (float)rnd.NextDouble() * 120f;
                var isl = RoundIsland.Create("Far" + i, new Vector3(Mathf.Cos(a) * r, -10f + (float)rnd.NextDouble() * 70f, Mathf.Sin(a) * r), 14f + (float)rnd.NextDouble() * 18f, 12f, terrain, stone, 100 + i, 6f);
                foreach (var c in isl.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                isl.transform.SetParent(far.transform, true);
            }

            Scenery.SkyDebris(root.transform);
            var bf = new GameObject("GoldenButterflies"); bf.transform.SetParent(root.transform, false);
            bf.AddComponent<GoldenButterflies>();
            foreach (var t in far.GetComponentsInChildren<Transform>()) if (t.parent == far.transform) { var b = t.gameObject.AddComponent<FloatBob>(); b.amplitude = 1.2f; b.period = 9f; b.spin = 0.8f; }

            // post-processing volume
            var profile = BuildProfile();
            var vol = root.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 1; vol.sharedProfile = profile;
            atm.volume = vol;

            StoreGenerated(root, AtmosphereDir + "/Atmosphere_Generated.asset");
            PrefabUtility.SaveAsPrefabAsset(root, AtmospherePrefab);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] Atmosphere prefab built: " + AtmospherePrefab);
        }

        /// <summary>
        /// Code-made meshes and materials (sky rocks, tiles, islets, far islands) are not assets, and SaveAsPrefabAsset keeps only empty
        /// references to them: store every one under `root` in a single asset first (rebuilt from scratch each time).
        /// </summary>
        static void StoreGenerated(GameObject root, string path)
        {
            AssetDatabase.DeleteAsset(path);
            Object main = null; var seen = new HashSet<Object>(); int n = 0;
            void Store(Object o)
            {
                if (o == null || AssetDatabase.Contains(o) || !seen.Add(o)) return;
                o.hideFlags = HideFlags.None;
                if (main == null) { AssetDatabase.CreateAsset(o, path); main = o; } else AssetDatabase.AddObjectToAsset(o, main);
                n++;
            }
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) { mf.sharedMesh = Scenery.SourceOf(mf.sharedMesh); Store(mf.sharedMesh); }
            foreach (var mc in root.GetComponentsInChildren<MeshCollider>(true)) Store(mc.sharedMesh);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                foreach (var m in r.sharedMaterials)
                {
                    Store(m);
                    if (m != null) foreach (var id in m.GetTexturePropertyNameIDs()) Store(m.GetTexture(id));
                }
            AssetDatabase.SaveAssets();
            Debug.Log($"[PlantGame] {n} generated meshes / materials stored in {path}");
        }

        static GameObject Quad(string name, Transform parent, Vector3 pos, float size, Material m, bool flat)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Quad); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = pos; g.transform.localScale = new Vector3(size, size, 1f);
            if (flat) g.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            g.GetComponent<MeshRenderer>().sharedMaterial = m;
            g.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            g.GetComponent<MeshRenderer>().receiveShadows = false;
            Object.DestroyImmediate(g.GetComponent<Collider>());
            return g;
        }

        static VolumeProfile BuildProfile()
        {
            var p = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (p == null) { p = ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(p, ProfilePath); }
            p.components.Clear();
            var bloom = p.Add<Bloom>(true); bloom.intensity.Override(0.55f); bloom.threshold.Override(1.0f); bloom.scatter.Override(0.6f);
            bloom.tint.Override(Color.Lerp(Color.white, Palette.GlowCyan, 0.25f));
            var tm = p.Add<Tonemapping>(true); tm.mode.Override(TonemappingMode.ACES);
            var ca = p.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.15f); ca.contrast.Override(12f); ca.saturation.Override(22f);
            ca.colorFilter.Override(new Color(1f, 0.975f, 0.92f));
            var sh = p.Add<ShadowsMidtonesHighlights>(true);   // cool shadows, warm highlights
            sh.shadows.Override(new Vector4(0.92f, 1.0f, 1.08f, -0.05f)); sh.highlights.Override(new Vector4(1.06f, 1.02f, 0.95f, 0.05f));
            var vg = p.Add<Vignette>(true); vg.intensity.Override(0.15f); vg.smoothness.Override(0.6f);
            EditorUtility.SetDirty(p);
            return p;
        }

        static Material Mat(string name, string shader, System.Action<Material> setup)
        {
            string path = MaterialBuilder.Dir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            var sh = Shader.Find(shader);
            if (sh == null) { Debug.LogError("[PlantGame] Shader not found: " + shader); return null; }
            if (m == null) { m = new Material(sh) { name = name }; AssetDatabase.CreateAsset(m, path); } else m.shader = sh;
            setup(m);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Tileable fluffy noise, alpha channel (value noise, 4 octaves, wraps at the edges).</summary>
        static Texture2D EnsureCloudTexture()
        {
            const string path = "Assets/Art/Textures/T_Cloud.png";
            if (!File.Exists(path))
            {
                const int S = 256;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
                var rnd = new System.Random(77);
                float[][] grids = new float[4][];
                int[] res = { 4, 8, 16, 32 };
                for (int o = 0; o < 4; o++) { grids[o] = new float[res[o] * res[o]]; for (int i = 0; i < grids[o].Length; i++) grids[o][i] = (float)rnd.NextDouble(); }
                float Sample(int o, float u, float v)
                {
                    int n = res[o]; float x = u * n, y = v * n; int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
                    float fx = x - x0, fy = y - y0; fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
                    float a = grids[o][(y0 % n) * n + x0 % n], b = grids[o][(y0 % n) * n + (x0 + 1) % n], c = grids[o][((y0 + 1) % n) * n + x0 % n], d = grids[o][((y0 + 1) % n) * n + (x0 + 1) % n];
                    return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
                }
                for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                {
                    float u = x / (float)S, v = y / (float)S;
                    float val = Sample(0, u, v) * 0.5f + Sample(1, u, v) * 0.27f + Sample(2, u, v) * 0.15f + Sample(3, u, v) * 0.08f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(val)));
                }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp != null && (!imp.alphaIsTransparency || imp.wrapMode != TextureWrapMode.Repeat))
            {
                imp.alphaIsTransparency = false; imp.wrapMode = TextureWrapMode.Repeat; imp.sRGBTexture = false; imp.mipmapEnabled = true;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------ audio
        public static void SetupAudioImports()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = (AudioImporter)AssetImporter.GetAtPath(path);
                if (imp == null) continue;
                bool music = path.Contains("/Music/");
                var s = imp.defaultSampleSettings;
                s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                s.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
                s.quality = 0.6f;
                imp.defaultSampleSettings = s; imp.forceToMono = true; imp.loadInBackground = music;
                imp.SaveAndReimport();
            }
        }

        // ------------------------------------------------------------------ scene pieces
        public static void AddAtmosphere()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AtmospherePrefab);
            if (prefab == null) { BuildAtmosphere(); prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AtmospherePrefab); }
            if (prefab != null) PrefabUtility.InstantiatePrefab(prefab);
        }

        public static void AddSystems()
        {
            SetupAudioImports();
            var go = new GameObject("Systems");
            go.AddComponent<GameBootstrap>();
            go.AddComponent<GrowthVfx>();
            go.AddComponent<SfxBinder>();
            AddAudioClips(go.AddComponent<AudioManager>());
        }

        /// <summary>Title.unity only needs the music: menu track + the sfx library for button clicks.</summary>
        public static void AddTitleAudio()
        {
            SetupAudioImports();
            AddAudioClips(new GameObject("Audio").AddComponent<AudioManager>());
        }

        static void AddAudioClips(AudioManager am)
        {
            am.menuMusic = Clip("Music/music_menu"); am.gameplayMusic = Clip("Music/music_gameplay"); am.windAmbience = Clip("Music/amb_wind");
            am.winMusic = Clip("Music/music_win"); am.rainMusic = Clip("Music/music_rain"); am.autumnMusic = Clip("Music/music_autumn"); am.rainAmbience = Clip("Music/amb_rain");
            // T53: one tune per curated level (level 1 and generated levels keep gameplayMusic)
            string[] tunes = { "level_2:music_autumn", "level_3:music_rain", "level_4:music_l4", "level_5:music_l5", "level_6:music_l6", "level_7:music_l7", "level_8:music_l8", "level_9:music_l9", "level_10:music_l10" };
            foreach (var tn in tunes) { var kv = tn.Split(':'); var c = Clip("Music/" + kv[1]); if (c != null) am.levelMusic.Add(new AudioManager.Entry { name = kv[0], clips = new[] { c } }); }
            var groups = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<AudioClip>>();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/SFX" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var n = Path.GetFileNameWithoutExtension(path);
                int us = n.LastIndexOf('_');
                string key = us > 0 && int.TryParse(n.Substring(us + 1), out _) ? n.Substring(0, us) : n;
                if (!groups.TryGetValue(key, out var list)) groups[key] = list = new System.Collections.Generic.List<AudioClip>();
                list.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
            }
            foreach (var kv in groups) am.sfx.Add(new AudioManager.Entry { name = kv.Key, clips = kv.Value.ToArray() });
        }

        static AudioClip Clip(string rel) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/" + rel + ".wav");

        /// <summary>Title.unity: sky, camera and the TitleMenu (level select, random level, seed codes).</summary>
        [MenuItem("PlantGame/17 Build Title", false, 17)]
        public static void BuildTitle()
        {
            WorldKitBuilder.Build();
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
            ProjectSetup.ApplyLightingPreset();
            ProjectSetup.AddSun();
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>(); cam.clearFlags = CameraClearFlags.Skybox; cam.fieldOfView = 60f; cam.farClipPlane = 1500f;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0, 20f, 0);
            AddAtmosphere();
            // the title view has no use for distant stuff: the big empty far islands go, drifting rocks and cloud cards are pulled in (positions only, sizes stay)
            var farIslands = GameObject.Find("FarIslands"); if (farIslands != null) Object.DestroyImmediate(farIslands);
            foreach (var g in new[] { "FloatingRocks", "Islets", "CloudCards" })
            {
                var grp = GameObject.Find(g); if (grp == null) continue;
                bool debris = g != "CloudCards";   // rocks and islets sit below the showcase islands, not among them
                foreach (Transform c in grp.transform)
                {
                    float y = debris ? -22f - Mathf.Repeat(c.localPosition.x * 0.37f + c.localPosition.z * 0.53f, 1f) * 28f : c.localPosition.y;
                    c.localPosition = new Vector3(c.localPosition.x * 0.45f, y, c.localPosition.z * 0.45f);
                }
            }
            TrimTitleDebris(GameObject.Find("FloatingRocks"));
            KeepOneTitleIslet(GameObject.Find("Islets"));
            AddTitleAudio();
            // the world behind the menu: a small generated chain (water, trees, ruins, soils) whose plants grow while you look
            var showcase = new GameObject("TitleWorld").AddComponent<WorldGenerator>();
            showcase.useBootstrapConfig = false; showcase.spawnPlayer = false; showcase.showcaseWater = true;
            showcase.sandboxConfig = new RunConfig { seed = 7421, islandCount = 3, levelId = "title", theme = LevelTheme.Sunny, waterMode = WaterMode.Sources, allowedKinds = new[] { PlantKind.Bridge, PlantKind.Ladder }, sequence = new[] { PlantKind.Bridge, PlantKind.Ladder } };
            new GameObject("Title").AddComponent<PlantGame.UI.TitleMenu>();
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, ProjectSetup.ScenePath("Title"));
            ProjectSetup.RegisterScenes();
            Debug.Log("[PlantGame] Title scene built.");
        }

        /// <summary>The title keeps one islet (the one nearest the showcase; TitleMenu moves it under the middle island, SetupTour) with a few stones and grass on top; the rest go.</summary>
        static void KeepOneTitleIslet(GameObject group)
        {
            if (group == null) return;
            Transform keep = null; float best = float.MaxValue;
            foreach (Transform c in group.transform) { float d = new Vector2(c.localPosition.x, c.localPosition.z).magnitude; if (d < best) { best = d; keep = c; } }
            for (int i = group.transform.childCount - 1; i >= 0; i--) { var c = group.transform.GetChild(i); if (c != keep) Object.DestroyImmediate(c.gameObject); }
            if (keep == null) return;
            keep.localPosition = new Vector3(keep.localPosition.x * 0.75f, -16f, keep.localPosition.z * 0.75f);
            // a few stones and grass tufts on its top (clear of the tree by its foot); a temporary collider on its mesh finds the ground
            var mf = keep.GetComponent<MeshFilter>(); if (mf == null || mf.sharedMesh == null) return;
            var col = keep.gameObject.AddComponent<MeshCollider>(); col.sharedMesh = mf.sharedMesh;
            var rnd = new System.Random(4711); var centre = keep.position; float rad = keep.GetComponent<Renderer>().bounds.extents.x;
            var tree = centre + new Vector3(0.5f, 0f, 0.3f);
            for (int i = 0, placed = 0; i < 120 && placed < 34; i++)
            {
                float a = (float)(rnd.NextDouble() * Mathf.PI * 2), r = rad * (0.2f + (float)rnd.NextDouble() * 0.55f);
                var p = new Vector3(centre.x + Mathf.Cos(a) * r, centre.y, centre.z + Mathf.Sin(a) * r);
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(tree.x, tree.z)) < 1.2f) continue;
                if (!col.Raycast(new Ray(p + Vector3.up * 5f, Vector3.down), out var hit, 10f) || hit.normal.y < 0.8f) continue;
                p = hit.point;
                bool rock = placed % 6 == 0;
                var g = OrganicProps.Create(rock ? (placed % 12 == 0 ? "Rock_Medium_1" : "rock_smallA") : "grass_large", p, (float)rnd.NextDouble() * 360f, keep, LevelTheme.Sunny);
                if (g == null) continue;
                float s = rock ? 0.7f + (float)rnd.NextDouble() * 0.5f : 1.6f + (float)rnd.NextDouble() * 0.9f;
                g.transform.localScale *= s;
                if (rock) g.transform.position += Vector3.up * (0.12f * g.transform.lossyScale.y);   // half sunk in the turf
                placed++;
            }
            Object.DestroyImmediate(col);
        }

        /// <summary>The title view has half the floating debris, and only its bigger rock pieces (no logs, small chips or crystal bits).</summary>
        public static void TrimTitleDebris(GameObject group)
        {
            if (group == null) return;
            var items = new System.Collections.Generic.List<(Transform t, float size)>();
            foreach (Transform c in group.transform)
            {
                var rs = c.GetComponentsInChildren<Renderer>(); if (rs.Length == 0) continue;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                items.Add((c, Mathf.Max(b.size.x, b.size.y, b.size.z)));
            }
            items.Sort((a, b) => b.size.CompareTo(a.size));
            int keep = items.Count / 2;
            for (int i = 0; i < items.Count; i++)
                if (i >= keep || items[i].t.name.StartsWith("Log")) Object.DestroyImmediate(items[i].t.gameObject);
        }

        [MenuItem("PlantGame/12 Build Main", false, 12)]
        public static void BuildMainMenu() { WorldKitBuilder.Build(); BuildAtmosphere(); SandboxPlantTestBuilder.BuildMain(); }
    }
}
