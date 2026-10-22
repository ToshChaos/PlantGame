using System.Collections.Generic;
using UnityEngine;

namespace PlantGame.Player
{
    /// <summary>
    /// T03: put on a trigger BoxCollider (layer Climbable) that covers a ladder / vine.
    /// Orientation: the volume's local +Z (forward) points AWAY from the climb surface, towards where the player stands.
    /// The climb line runs vertically through the box centre (plus 'standOff' along forward).
    /// 'land' is where the player is placed when reaching the top; the box should extend above Land height and
    /// horizontally cover Land, so S at the top edge can start a descent.
    /// T11 adds this component to the CLIMB_* trigger boxes of ladder plants.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class ClimbableVolume : MonoBehaviour
    {
        public Transform land;
        [Tooltip("Distance of the climb line from the box centre along forward (metres).")]
        public float standOff = 0f;

        static readonly List<ClimbableVolume> _all = new List<ClimbableVolume>();
        /// <summary>All active volumes. The player tests containment itself each frame (trigger callbacks race with teleports).</summary>
        public static IReadOnlyList<ClimbableVolume> All => _all;

        BoxCollider _box;
        BoxCollider Box => _box != null ? _box : (_box = GetComponent<BoxCollider>());

        public Vector3 SurfaceNormal
        {
            get { var f = transform.forward; f.y = 0f; return f.sqrMagnitude < 1e-4f ? Vector3.forward : f.normalized; }
        }

        /// <summary>Horizontal position (x, z) the player is pulled to while climbing; y is ignored.</summary>
        public Vector3 ClimbLinePoint => Box.bounds.center + SurfaceNormal * standOff;
        public float TopY => land != null ? land.position.y : Box.bounds.max.y;
        public float BottomY => Box.bounds.min.y;

        /// <summary>Where the player ends up when reaching the top.</summary>
        public Vector3 LandPosition
        {
            get
            {
                if (land != null) return land.position;
                var p = ClimbLinePoint - SurfaceNormal * 0.8f;   // over the lip, away from the player side
                p.y = Box.bounds.max.y;
                return p;
            }
        }

        void OnEnable() { _all.Add(this); }
        void OnDisable() { _all.Remove(this); }

        /// <summary>True when the world point is inside the (oriented) box.</summary>
        public bool Contains(Vector3 worldPoint)
        {
            var local = transform.InverseTransformPoint(worldPoint) - Box.center;
            var h = Box.size * 0.5f;
            return Mathf.Abs(local.x) <= h.x && Mathf.Abs(local.y) <= h.y && Mathf.Abs(local.z) <= h.z;
        }

        void Reset()
        {
            Box.isTrigger = true;
            int layer = LayerMask.NameToLayer("Climbable");
            if (layer >= 0) gameObject.layer = layer;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.49f, 0.95f, 0.9f, 0.9f);
            var c = Box.bounds.center;
            Gizmos.DrawLine(new Vector3(ClimbLinePoint.x, Box.bounds.min.y, ClimbLinePoint.z), new Vector3(ClimbLinePoint.x, Box.bounds.max.y, ClimbLinePoint.z));
            Gizmos.DrawRay(c, SurfaceNormal);
            if (land != null) Gizmos.DrawWireSphere(land.position, 0.25f);
        }
    }
}
