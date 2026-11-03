using UnityEngine;

namespace PlantGame.World
{
    /// <summary>Drifting cloud card: slides along the wind and wraps around the camera, faces the camera (yaw only).</summary>
    public class CloudDrift : MonoBehaviour
    {
        public Vector3 wind = new Vector3(1f, 0f, 0.3f);
        public float speed = 1.2f;
        public float wrapRadius = 260f;
        Camera _cam;

        void Update()
        {
            if (_cam == null) _cam = Camera.main;
            transform.position += wind.normalized * speed * Time.deltaTime;
            if (_cam == null) return;
            var c = _cam.transform.position; var d = transform.position - c; d.y = 0f;
            if (d.magnitude > wrapRadius) transform.position = new Vector3(c.x - d.x * 0.98f, transform.position.y, c.z - d.z * 0.98f);
            var look = transform.position - c; look.y = 0f;
            if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(look.normalized);
        }
    }
}
