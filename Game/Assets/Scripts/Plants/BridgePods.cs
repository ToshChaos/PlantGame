using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;
using PlantGame.World;

namespace PlantGame.Plants
{
    /// <summary>
    /// Glowing lantern pods that hang on thin tendrils under a vine bridge's deck, in the bridge's own flower colour. Each pod drops in as the
    /// growth front passes its spot, then swings slowly. Rendering only: no colliders, nothing the layout or the seed code sees; the spots
    /// are hashed from the plant's position. Added by PlantInstance when a bridge starts growing (or stands grown at once).
    /// </summary>
    public class BridgePods : MonoBehaviour
    {
        const float SpacingM = 2.6f, DropSeconds = 0.7f, SwayDegrees = 6f, Glowing = 0.5f;
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        static Mesh _pod, _calyx, _tendril;
        static Material _green;
        static readonly Dictionary<Color, Material> _glow = new Dictionary<Color, Material>();

        class Pod { public Transform pivot; public Renderer glow; public float at, shown = -1f, phase, period, size; public Quaternion rest; }
        readonly List<Pod> _pods = new List<Pod>();
        PlantInstance _plant; Color _colour; MaterialPropertyBlock _mpb; bool _pulse;

        public static void Hang(PlantInstance plant, bool instantly)
        {
            if (plant == null || plant.GetComponent<BridgePods>() != null) return;
            var bp = plant.gameObject.AddComponent<BridgePods>();
            bp.Build(plant, instantly);
        }

        void Build(PlantInstance plant, bool instantly)
        {
            _plant = plant; _mpb = new MaterialPropertyBlock(); _pulse = !QualityTier.Lite;
            _colour = plant.FlowerColor.a > 0f ? plant.FlowerColor : new Color(0.949f, 0.784f, 0.294f);
            var cols = new List<BoxCollider>();
            foreach (var c in plant.GetComponentsInChildren<BoxCollider>(true)) if (c.name.StartsWith("COL_")) cols.Add(c);
            if (cols.Count < 3 || !plant.TryGetEnds(out var a, out var t)) { enabled = false; return; }
            cols.Sort((x, y) => Order(x).CompareTo(Order(y)));

            // the holder undoes the plant's own scale (a bridge is stretched along its span), so pods keep their shape and swing true
            var holder = new GameObject("Pods").transform; holder.SetParent(plant.transform, false);
            var ls = plant.transform.lossyScale; holder.localScale = new Vector3(1f / Mathf.Max(0.01f, ls.x), 1f / Mathf.Max(0.01f, ls.y), 1f / Mathf.Max(0.01f, ls.z));
            Vector3 span = t - a; span.y = 0f;
            var yaw = Quaternion.LookRotation(span.sqrMagnitude > 0.01f ? span.normalized : plant.transform.forward);
            int seed = Mathf.RoundToInt(a.x * 11.7f + a.z * 5.3f + t.x * 3.1f);
            int count = Mathf.Clamp(Mathf.RoundToInt((t - a).magnitude / SpacingM), 3, 6);
            Meshes();

            for (int i = 0; i < count; i++)
            {
                float f = Mathf.Lerp(0.14f, 0.86f, (i + 0.25f + 0.5f * Scenery.Hash01(seed, 10 + i)) / count);   // along the deck, never at the very ends
                var c = cols[Mathf.Clamp(Mathf.FloorToInt(f * cols.Count), 0, cols.Count - 1)];
                float side = (i % 2 == 0 ? 1f : -1f) * Mathf.Lerp(0.3f, 0.66f, Scenery.Hash01(seed, 20 + i)) * c.size.x * 0.5f;
                float along = (Scenery.Hash01(seed, 30 + i) - 0.5f) * c.size.z * 0.6f;
                // from just inside the vine bundle (above the box's underside), so the tendril grows out of the deck
                Vector3 top = c.transform.TransformPoint(c.center + new Vector3(side, -c.size.y * 0.5f + 0.1f, along));
                float len = Mathf.Lerp(0.35f, 0.95f, Scenery.Hash01(seed, 40 + i)), size = Mathf.Lerp(1.05f, 1.4f, Scenery.Hash01(seed, 50 + i));

                // a pod near the end of the span could hang into the island's rock: leave that one out
                if (Physics.CheckCapsule(top + Vector3.down * 0.25f, top + Vector3.down * (len + 0.3f * size), 0.17f * size, Layers.SolidMask, QueryTriggerInteraction.Ignore)) continue;
                var pivot = new GameObject("Pod" + i).transform; pivot.SetParent(holder, false);
                pivot.position = top; pivot.rotation = yaw * Quaternion.Euler(0f, Scenery.Hash01(seed, 60 + i) * 360f, 0f);
                ShapeMeshes.Obj("Tendril", _tendril, _green, pivot, Vector3.zero, Vector3.zero, new Vector3(1f, len, 1f));
                ShapeMeshes.Obj("Calyx", _calyx, _green, pivot, new Vector3(0f, -len, 0f), Vector3.zero, Vector3.one * size);
                var pod = ShapeMeshes.Obj("Lantern", _pod, Glow(_colour), pivot, new Vector3(0f, -len - 0.02f * size, 0f), Vector3.zero, Vector3.one * size);
                foreach (var r in pivot.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _pods.Add(new Pod
                {
                    pivot = pivot, glow = pod.GetComponent<Renderer>(), at = f, size = size, rest = pivot.localRotation,
                    phase = Scenery.Hash01(seed, 70 + i) * 6.28f, period = Mathf.Lerp(2.6f, 3.8f, Scenery.Hash01(seed, 80 + i)),
                });
                pivot.localScale = instantly ? Vector3.one : Vector3.zero;
                if (instantly) _pods[_pods.Count - 1].shown = DropSeconds;
            }
        }

        static int Order(Collider c) { var p = c.name.Split('_'); return p.Length > 2 && int.TryParse(p[2], out int i) ? i : 0; }

        void Update()
        {
            if (_plant == null || _pods.Count == 0) return;
            float k = Mathf.Clamp01((_plant.Growth - _plant.sproutGrowth) / Mathf.Max(0.01f, 1f - _plant.sproutGrowth));
            bool seen = false;
            foreach (var p in _pods)
            {
                if (p.shown < 0f) { if (k < p.at + 0.04f && !_plant.IsGrown) continue; p.shown = 0f; }
                if (p.shown < DropSeconds)
                {
                    p.shown += Time.deltaTime;
                    float u = Mathf.Clamp01(p.shown / DropSeconds);
                    float back = 1f + 2.2f * Mathf.Pow(u - 1f, 3f) + 1.2f * Mathf.Pow(u - 1f, 2f);   // ease out with a small overshoot
                    p.pivot.localScale = new Vector3(Mathf.Clamp01(u * 2.5f), back, Mathf.Clamp01(u * 2.5f));
                }
                seen |= p.glow.isVisible;
            }
            if (!seen) return;
            float time = Time.time;
            foreach (var p in _pods)
            {
                if (p.shown < 0f) continue;
                float s = Mathf.Sin(time * 6.283f / p.period + p.phase);
                p.pivot.localRotation = p.rest * Quaternion.Euler(SwayDegrees * 0.35f * Mathf.Sin(time * 6.283f / (p.period * 1.37f) + p.phase * 2f), 0f, SwayDegrees * s);
                if (_pulse)
                {
                    _mpb.SetColor(EmissionId, _colour * (Glowing + 0.2f * Mathf.Sin(time * 1.7f + p.phase)));
                    p.glow.SetPropertyBlock(_mpb);
                }
            }
        }

        // ------------------------------------------------------------------ shared meshes and materials (session statics)

        static void Meshes()
        {
            if (_pod != null) return;
            // a ribbed lantern pod (a physalis husk), hanging from y = 0, bottom to top
            var prof = new List<Vector2> { new Vector2(0.001f, -0.36f), new Vector2(0.03f, -0.35f), new Vector2(0.07f, -0.32f), new Vector2(0.105f, -0.28f), new Vector2(0.13f, -0.235f),
                new Vector2(0.14f, -0.19f), new Vector2(0.135f, -0.14f), new Vector2(0.115f, -0.09f), new Vector2(0.08f, -0.045f), new Vector2(0.035f, -0.01f), new Vector2(0.012f, 0f) };
            _pod = ShapeMeshes.Lathe(prof, 20, false, (ang, i) => 1f + 0.1f * Mathf.Cos(ang * 5f), "BridgePod");
            // leafy cap: five pointed sepals over the pod's shoulder
            var cap = new List<Vector2> { new Vector2(0.155f, -0.125f), new Vector2(0.115f, -0.05f), new Vector2(0.032f, 0f), new Vector2(0.001f, 0.015f) };
            _calyx = ShapeMeshes.Lathe(cap, 20, false, (ang, i) => i == 0 ? 0.55f + 0.45f * Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 2.5f)), 3f) : 1f, "BridgePodCalyx");
            // tendril: 1 m long (scaled to length), a gentle S so it does not read as a string
            var pts = new List<Vector3>();
            for (int i = 0; i <= 8; i++) { float u = i / 8f; pts.Add(new Vector3(Mathf.Sin(u * Mathf.PI * 2f) * 0.04f, -u, Mathf.Sin(u * Mathf.PI) * 0.03f)); }
            _tendril = ShapeMeshes.Tube(pts, u => Mathf.Lerp(0.03f, 0.014f, u), 5);
            foreach (var m in new[] { _pod, _calyx, _tendril }) m.hideFlags = HideFlags.DontSave;
            _green = RuntimeMats.NewLit("M_BridgePodStem", new Color(0.24f, 0.55f, 0.32f)); _green.hideFlags = HideFlags.DontSave;
        }

        static Material Glow(Color c)
        {
            if (_glow.TryGetValue(c, out var m) && m != null) return m;
            m = RuntimeMats.NewLit("M_BridgePod", c);
            m.SetFloat("_Smoothness", 0.55f);
            m.EnableKeyword("_EMISSION"); m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive; m.SetColor(EmissionId, c * Glowing);
            return _glow[c] = m;
        }
    }
}
