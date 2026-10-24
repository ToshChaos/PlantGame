using UnityEngine;

namespace PlantGame.World
{
    /// <summary>Gentle bob and slow spin for floating rocks and backdrop islets.</summary>
    public class FloatBob : MonoBehaviour
    {
        public float amplitude = 0.6f, period = 6f, spin = 4f, wobble = 0f;   // wobble: degrees of slow tilt
        [Tooltip("Own oscillation phase in radians (negative = derived from the position). Islands each get their own so they never move in sync.")] public float phaseOffset = -1f;
        [Tooltip("Walkable islands: move a kinematic Rigidbody (in FixedUpdate) so the collider moves cleanly and the player is lifted along.")] public bool physics;
        Rigidbody _rb;
        Vector3 _base; float _phase;
        void Start()
        {
            _base = transform.position; _phase = phaseOffset >= 0f ? phaseOffset : (_base.x * 0.37f + _base.z * 0.21f);
            if (physics) { _rb = gameObject.AddComponent<Rigidbody>(); _rb.isKinematic = true; _rb.interpolation = RigidbodyInterpolation.Interpolate; }
        }
        void FixedUpdate() { if (_rb != null) _rb.MovePosition(_base + Vector3.up * Offset(Time.time)); }
        float Offset(float t) => Mathf.Sin(t * Mathf.PI * 2f / period + _phase) * amplitude;
        void Update()
        {
            if (_rb != null) return;
            transform.position = _base + Vector3.up * Offset(Time.time);
            transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.World);
            if (wobble > 0f)
            {
                float w = Time.time * 0.45f + _phase;   // tilt rate in degrees per second, added on top of the spin
                transform.Rotate(Mathf.Cos(w) * wobble * 0.45f * Time.deltaTime, 0f, Mathf.Sin(w * 0.8f) * wobble * 0.45f * Time.deltaTime, Space.World);
            }
        }
    }
}
