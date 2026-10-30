using UnityEngine;

namespace PlantGame.World
{
    /// <summary>
    /// Glowing golden butterflies drifting around the camera (mood from the reference art). Built at runtime: two wing quads per
    /// butterfly with a generated wing sprite and an HDR gold unlit material (the bloom makes them glow). Cheap: 16 butterflies,
    /// each wanders on smooth noise inside a radius around the player and wraps back when left behind.
    /// </summary>
    public class GoldenButterflies : MonoBehaviour
    {
        public int count = 16;
        public float radius = 22f, heightMin = 0.8f, heightMax = 4.5f;
        [ColorUsage(false, true)] public Color glow = new Color(4f, 2.3f, 0.6f);

        class Fly { public Transform root, wingL, wingR; public Vector3 pos, target; public float seed, speed, size; }
        /// <summary>When set (the title's fly-by sets it to what the camera looks at), the butterflies gather there instead of round the camera.</summary>
        public static Vector3? Focus;
        Fly[] _flies;
        Transform _anchor;
        Mesh _quad;
        Material _mat;

        void Start()
        {
            _mat = Core.RuntimeMats.New(Core.RuntimeMats.Unlit, "M_Butterfly", glow);
            Core.RuntimeMats.SetTransparent(_mat); _mat.SetFloat("_Cull", 0f);
            _mat.SetTexture("_BaseMap", WingTexture());
            _quad = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
            _flies = new Fly[count];
            var rnd = new System.Random(42);
            for (int i = 0; i < count; i++)
            {
                var f = new Fly { seed = (float)rnd.NextDouble() * 100f, speed = 0.8f + (float)rnd.NextDouble() * 1.1f, size = 0.28f + (float)rnd.NextDouble() * 0.3f };
                f.root = new GameObject("Butterfly" + i).transform; f.root.SetParent(transform, false);
                f.wingL = Wing(f.root, -1); f.wingR = Wing(f.root, 1);
                f.root.localScale = Vector3.one * f.size;
                _flies[i] = f;
            }
        }

        Transform Wing(Transform parent, int side)
        {
            var hinge = new GameObject(side < 0 ? "WingL" : "WingR").transform; hinge.SetParent(parent, false);
            var q = new GameObject("Quad"); q.transform.SetParent(hinge, false);
            q.AddComponent<MeshFilter>().sharedMesh = _quad;
            var r = q.AddComponent<MeshRenderer>(); r.sharedMaterial = _mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);                 // lie flat
            q.transform.localPosition = new Vector3(side * 0.5f, 0f, 0f);
            q.transform.localScale = new Vector3(side, 1f, 1f);                         // mirror the right wing
            return hinge;
        }

        static Texture2D WingTexture()
        {
            const int S = 64; var t = new Texture2D(S, S, TextureFormat.RGBA32, true) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                // wing drawn on the right half: upper lobe large, lower lobe smaller, hinge at the left edge
                float u = x / (float)(S - 1), v = y / (float)(S - 1) * 2f - 1f;
                float up = Mathf.Pow(u - 0.55f, 2f) / 0.2f + Mathf.Pow(v - 0.4f, 2f) / 0.28f;
                float low = Mathf.Pow(u - 0.4f, 2f) / 0.09f + Mathf.Pow(v + 0.45f, 2f) / 0.12f;
                float d = Mathf.Min(up, low);
                float a = Mathf.Clamp01((1f - d) * 2f);
                float vein = 1f - 0.2f * u;
                t.SetPixel(x, y, new Color(vein, vein, vein, a));
            }
            t.Apply(); return t;
        }

        void LateUpdate()
        {
            if (_flies == null) return;
            if (_anchor == null) { var c = Camera.main; if (c == null) return; _anchor = c.transform; }
            Vector3 centre = Focus ?? _anchor.position;
            float t = Time.time;
            foreach (var f in _flies)
            {
                if (f.pos == Vector3.zero) f.pos = centre + Random.insideUnitSphere * radius * 0.5f;
                // wander: a target that drifts on noise, the butterfly eases toward it
                float n1 = Mathf.PerlinNoise(f.seed, t * 0.07f) - 0.5f, n2 = Mathf.PerlinNoise(f.seed + 11f, t * 0.07f) - 0.5f, n3 = Mathf.PerlinNoise(f.seed + 23f, t * 0.12f);
                Vector3 off = new Vector3(n1 * 2f * radius, Mathf.Lerp(heightMin, heightMax, n3), n2 * 2f * radius);
                f.target = new Vector3(centre.x, FloorY(centre), centre.z) + off;
                Vector3 to = f.target - f.pos;
                f.pos += to.normalized * Mathf.Min(to.magnitude, f.speed * Time.deltaTime * 2f);
                Vector3 dir = to; dir.y = 0f;
                f.root.position = f.pos + Vector3.up * Mathf.Sin(t * 3f + f.seed) * 0.12f;
                if (dir.sqrMagnitude > 0.01f) f.root.rotation = Quaternion.Slerp(f.root.rotation, Quaternion.LookRotation(dir.normalized), 3f * Time.deltaTime);
                float flap = Mathf.Sin(t * (11f + f.speed * 3f) + f.seed) * 55f + 35f;
                f.wingL.localRotation = Quaternion.Euler(0f, 0f, flap);
                f.wingR.localRotation = Quaternion.Euler(0f, 0f, -flap);
            }
        }

        static float FloorY(Vector3 c) => Physics.Raycast(c + Vector3.up * 2f, Vector3.down, out var h, 60f) ? h.point.y : c.y - 1.5f;
    }
}
