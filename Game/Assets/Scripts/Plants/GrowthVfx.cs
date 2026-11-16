using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;
using PlantGame.Inventory;

namespace PlantGame.Plants
{
    /// <summary>
    /// Particles for planting, growing, finishing a plant, picking up an item and winning. Everything is built at runtime
    /// from one soft-dot material (no prefabs, no textures): palette cyan / white / soft green. Cheap: short-lived bursts, one
    /// following emitter per growing plant.
    /// </summary>
    public class GrowthVfx : MonoBehaviour
    {
        class Follow { public PlantInstance plant; public ParticleSystem ps; public Transform anchor, tip; }

        readonly List<Follow> _active = new List<Follow>();
        Material _mat;
        Transform _player;

        void Awake() { _mat = Motes.Mat; }

        void OnEnable()
        {
            PlantInstance.Sprouted += OnSprout; PlantInstance.GrowthStarted += OnGrowStart; PlantInstance.Grown += OnGrown;
            GameEvents.ItemPickedUp += OnPickup; GameEvents.GameWon += OnWin; GameEvents.Watered += OnWatered;
            SeedTree.SeedRegrown += OnSeedRegrown;
        }

        void OnDisable()
        {
            PlantInstance.Sprouted -= OnSprout; PlantInstance.GrowthStarted -= OnGrowStart; PlantInstance.Grown -= OnGrown;
            GameEvents.ItemPickedUp -= OnPickup; GameEvents.GameWon -= OnWin; GameEvents.Watered -= OnWatered;
            SeedTree.SeedRegrown -= OnSeedRegrown;
        }

        Transform Player() { if (_player == null) { var pc = PlantGame.Player.PlayerController.Instance; if (pc != null) _player = pc.transform; } return _player; }

        ParticleSystem Make(string name, Vector3 pos, Color a, Color b, float size, float life, float speed, int burst, float rate, float gravity, float shapeRadius, bool loop)
        {
            var go = new GameObject(name); go.transform.SetParent(transform, false); go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = loop; main.duration = 1.2f; main.playOnAwake = false;
            main.startLifetime = life; main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(a, b);
            main.gravityModifier = gravity; main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;
            var em = ps.emission; em.rateOverTime = rate;
            if (burst > 0) em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burst) });
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = shapeRadius;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                      new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.15f), new GradientAlphaKey(0, 1) });
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.2f));
            var r = go.GetComponent<ParticleSystemRenderer>(); r.sharedMaterial = _mat; r.renderMode = ParticleSystemRenderMode.Billboard;
            return ps;
        }

        void Burst(string name, Vector3 pos, Color a, Color b, float size, float life, float speed, int count, float gravity = 0f, float radius = 0.3f)
        {
            var ps = Make(name, pos, a, b, size, life, speed, count, 0f, gravity, radius, false);
            ps.Play();
            Destroy(ps.gameObject, life + 1.5f);
        }

        static readonly Color Cyan = new Color(0.486f, 0.949f, 0.902f), White = new Color(0.957f, 0.973f, 0.965f), Green = new Color(0.365f, 0.706f, 0.42f), Gold = new Color(1f, 0.86f, 0.45f),
            Firefly = new Color(1f, 0.8f, 0.25f), FireflyPale = new Color(1f, 0.95f, 0.65f);

        void OnSprout(PlantInstance p) { Burst("VFX_SeedPlant", p.transform.position + Vector3.up * 0.2f, Green, Cyan, 0.12f, 0.9f, 1.4f, 18, -0.1f, 0.4f); }

        void OnWatered() { var pl = Player(); if (pl != null) Burst("VFX_Water", pl.position + pl.forward * 1f + Vector3.up * 1.1f, Cyan, White, 0.07f, 0.6f, 1.2f, 10, 0.5f, 0.15f); }

        void OnGrowStart(PlantInstance p)
        {
            Transform a = null, t = null;
            foreach (var tr in p.GetComponentsInChildren<Transform>(true)) { if (tr.name == "Anchor") a = tr; else if (tr.name == "Tip") t = tr; }
            if (a == null || t == null) return;
            var ps = Make("VFX_GrowthFront", a.position, Cyan, White, 0.1f, 1.0f, 0.5f, 0, 28f, -0.05f, 0.25f, true);
            ps.Play();
            _active.Add(new Follow { plant = p, ps = ps, anchor = a, tip = t });
        }

        void Update()
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var f = _active[i];
                if (f.plant == null || f.ps == null) { if (f.ps != null) Destroy(f.ps.gameObject); _active.RemoveAt(i); continue; }
                // path estimate: straight anchor -> tip, with an arch for long horizontal spans (bridges)
                Vector3 a = f.anchor.position, t = f.tip.position, d = t - a;
                float k = Mathf.Clamp01((f.plant.Growth - f.plant.sproutGrowth) / (1f - f.plant.sproutGrowth));
                float arch = f.plant.kind == PlantKind.Bridge ? Mathf.Sin(k * Mathf.PI) * 0.12f * new Vector2(d.x, d.z).magnitude : 0f;
                f.ps.transform.position = Vector3.Lerp(a, t, k) + Vector3.up * arch;
                if (!f.plant.IsGrowing) { f.ps.Stop(); Destroy(f.ps.gameObject, 1.5f); _active.RemoveAt(i); }
            }
        }

        void OnGrown(PlantInstance p)
        {
            Transform t = null;
            foreach (var tr in p.GetComponentsInChildren<Transform>(true)) if (tr.name == "Tip") t = tr;
            var pos = t != null ? t.position : p.transform.position + Vector3.up;
            Burst("VFX_Grown", pos, White, Cyan, 0.16f, 1.4f, 2.2f, 36, 0.1f, 0.5f);
            Burst("VFX_GrownBase", p.transform.position + Vector3.up * 0.3f, Cyan, Green, 0.1f, 1.0f, 1.2f, 16, -0.05f, 0.8f);
            Fireflies(p);
        }

        /// <summary>
        /// A grown plant stays alive: a few slow fireflies drift round it for as long as it stands (High tier only). A bridge gets them along
        /// its span, the other kinds inside their own bounds. The emitter is a child of the plant, so it rides the island and goes with it.
        /// </summary>
        void Fireflies(PlantInstance p)
        {
            if (QualityTier.Lite) return;
            Vector3 centre, box; Quaternion rot = Quaternion.identity;
            if (p.kind == PlantKind.Bridge && p.TryGetEnds(out var a, out var t) && (t - a).sqrMagnitude > 1f)
            {
                centre = (a + t) * 0.5f + Vector3.up * 0.9f; rot = Quaternion.LookRotation(t - a);
                box = new Vector3(2.2f, 1.6f, (t - a).magnitude * 0.9f);
            }
            else
            {
                var rs = p.GetComponentsInChildren<MeshRenderer>(); if (rs.Length == 0) return;
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                centre = b.center; box = Vector3.Max(b.size * 0.85f, Vector3.one * 1.2f);
            }
            float volume = box.x * box.y * box.z;
            var ps = Motes.Ambient("VFX_Fireflies", p.transform, centre, Vector3.one, Firefly, p.kind == PlantKind.Collector ? Cyan : FireflyPale, 0.2f, 4.5f, 0.12f, Mathf.Clamp(volume * 0.18f, 3f, 6f), 30);
            // the box is given in world metres: undo the plant's own scale (a stretched bridge, the smaller collector) and turn it along the span
            ps.transform.rotation = rot;
            var ls = ps.transform.lossyScale;
            var sh = ps.shape; sh.scale = new Vector3(box.x / Mathf.Max(0.01f, ls.x), box.y / Mathf.Max(0.01f, ls.y), box.z / Mathf.Max(0.01f, ls.z));
        }

        /// <summary>A seed source grew a new seed: a little puff in the seed's place so the eye catches it.</summary>
        void OnSeedRegrown(Vector3 pos) { Burst("VFX_SeedRegrown", pos, Gold, White, 0.11f, 1.1f, 1.0f, 14, -0.08f, 0.35f); }

        void OnPickup(ItemDefinition i)
        {
            var pl = Player(); if (pl == null) return;
            Burst("VFX_Pickup", pl.position + Vector3.up * 1.0f, White, Cyan, 0.09f, 0.8f, 1.6f, 14, -0.2f, 0.3f);
        }

        void OnWin()
        {
            var pl = Player(); if (pl == null) return;
            var petals = Make("VFX_WinPetals", pl.position + Vector3.up * 5f, Green, White, 0.18f, 3.5f, 1.5f, 0, 0f, 0.15f, 5f, false);
            var em = petals.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 90), new ParticleSystem.Burst(0.4f, 40) });
            var main = petals.main; main.maxParticles = 160;
            petals.Play(); Destroy(petals.gameObject, 6f);
        }
    }
}
