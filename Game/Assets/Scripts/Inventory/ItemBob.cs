using UnityEngine;
using PlantGame.Core;

namespace PlantGame.Inventory
{
    /// <summary>
    /// Life for a pickup. Once it has come to rest it lifts a hand's width off the ground, bobs, leans a little and spins; its glow
    /// breathes, and on the High tier a few motes in its own colour rise from it. Each one picks its own direction (clockwise or not),
    /// a speed between 0.2 and `degreesPerSecond` deg/s and its own rhythm, so a patch of seeds never moves in unison.
    /// The prefab is one object (mesh, colliders, Rigidbody): Awake moves the mesh onto a `Visual` child, so only the look floats
    /// while the colliders and the body stay on the ground where the sensor and the markers expect them.
    /// </summary>
    public class ItemBob : MonoBehaviour
    {
        /// <summary>Upper bound of the random spin speed (the old fixed speed).</summary>
        public float degreesPerSecond = 60f;
        public const float MinDegreesPerSecond = 0.2f;
        [Tooltip("Gap between the ground and the lowest point of the resting pickup, metres.")] public float hoverGap = 0.1f;
        [Tooltip("The bob rises this far above the hover height and comes back, metres.")] public float bobHeight = 0.08f;
        public float bobSeconds = 2.6f;
        [Tooltip("How far the pickup leans while it floats, degrees.")] public float tiltDegrees = 6f;
        [Tooltip("The glow swings between these multiples of the material's own emission.")] public Vector2 glowRange = new Vector2(0.8f, 1.9f);

        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor"), BaseColorId = Shader.PropertyToID("_BaseColor");

        float _speed, _phase, _lift, _hover, _yaw;
        Transform _visual; Renderer _renderer; Rigidbody _rb;
        bool _glows; Color _emission; MaterialPropertyBlock _mpb;

        void Awake()
        {
            _speed = Random.Range(MinDegreesPerSecond, Mathf.Max(MinDegreesPerSecond, degreesPerSecond)) * (Random.value < 0.5f ? -1f : 1f);
            _phase = Random.value * 6.2832f;
            _rb = GetComponent<Rigidbody>();
            // the solid sphere is small (4 cm on the parachute leaf) and a dropped seed lands at about 4 m/s: with discrete collision it can
            // step right through the island's mesh and fall for ever. Swept collision stops that (islands are kinematic bodies, hence Dynamic).
            if (_rb != null && !_rb.isKinematic) _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            BuildVisual();
        }

        /// <summary>Moves the look onto a child so it can float; without a mesh on the root the root itself just spins as it always did.</summary>
        void BuildVisual()
        {
            var mf = GetComponent<MeshFilter>(); var mr = GetComponent<MeshRenderer>();
            if (mf == null || mr == null || mf.sharedMesh == null) { _visual = transform; _renderer = GetComponentInChildren<Renderer>(); return; }
            var v = new GameObject("Visual"); v.layer = gameObject.layer; v.transform.SetParent(transform, false);
            v.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var r = v.AddComponent<MeshRenderer>(); r.sharedMaterial = mr.sharedMaterial; r.shadowCastingMode = mr.shadowCastingMode; r.receiveShadows = mr.receiveShadows;
            mr.enabled = false;
            _visual = v.transform; _renderer = r;

            // how far to lift: the solid sphere rests on the ground, so the ground is at its lowest point
            float ground = 0f;
            foreach (var c in GetComponents<SphereCollider>()) if (!c.isTrigger) { ground = c.center.y - c.radius; break; }
            _hover = Mathf.Max(0f, hoverGap - (mf.sharedMesh.bounds.min.y - ground));

            var m = r.sharedMaterial;
            _glows = m != null && m.HasProperty(EmissionId) && m.IsKeywordEnabled("_EMISSION");
            if (_glows) { _emission = m.GetColor(EmissionId); _mpb = new MaterialPropertyBlock(); }
            if (!QualityTier.Lite && m != null)
            {
                var tint = m.HasProperty(BaseColorId) ? m.GetColor(BaseColorId) : Color.white; tint.a = 1f;
                Motes.Ambient("Motes", transform, transform.position + Vector3.up * (_hover + 0.12f), Vector3.one * 0.35f,
                    tint, Color.Lerp(tint, Color.white, 0.5f), 0.12f, 1.7f, 0.3f, 3f, 10);
            }
        }

        void Update()
        {
            if (_visual == transform) { transform.Rotate(0f, _speed * Time.deltaTime, 0f, Space.World); return; }

            // float only at rest: a seed that is falling or rolling carries its look with it
            bool resting = _rb == null || _rb.isKinematic || _rb.IsSleeping() || _rb.linearVelocity.sqrMagnitude < 0.04f;
            _lift = Mathf.MoveTowards(_lift, resting ? 1f : 0f, Time.deltaTime * 1.6f);
            _yaw += _speed * Time.deltaTime;
            if (_renderer != null && !_renderer.isVisible && _lift >= 1f) return;   // settled and off screen: nothing to show

            float k = _lift * _lift * (3f - 2f * _lift), t = Time.time * (6.2832f / Mathf.Max(0.1f, bobSeconds)) + _phase;
            _visual.localPosition = Vector3.up * (k * (_hover + bobHeight * (0.5f + 0.5f * Mathf.Sin(t))));
            _visual.localRotation = Quaternion.Euler(tiltDegrees * k * Mathf.Sin(t * 0.7f), _yaw, tiltDegrees * k * Mathf.Cos(t * 0.53f));

            // the focus highlight writes the same property block while the player looks at this pickup: it wins
            if (!_glows || (TryGetComponent<FocusHighlight>(out var focus) && focus.enabled)) return;
            float g = Mathf.Lerp(glowRange.x, glowRange.y, 0.5f + 0.5f * Mathf.Sin(t * 1.3f + 1f));
            _renderer.GetPropertyBlock(_mpb);
            _mpb.SetColor(EmissionId, _emission * g);
            _renderer.SetPropertyBlock(_mpb);
        }
    }
}
