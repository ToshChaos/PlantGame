using UnityEngine;

namespace PlantGame.Inventory
{
    /// <summary>
    /// The last moment of a pickup: its look (the prefab's `Visual` child, `ItemBob.visual`) leaves the pickup, arcs to the player's chest in a
    /// fifth of a second while it shrinks, and is gone. The pickup itself is already in the inventory and destroyed.
    /// </summary>
    public class PickupFly : MonoBehaviour
    {
        const float Seconds = 0.22f, ChestHeight = 1.1f, Arc = 0.35f;
        Transform _to; Vector3 _from, _scale; float _t;

        public static void Launch(Transform visual, Transform to)
        {
            if (visual == null || to == null) return;
            visual.SetParent(null, true);
            var r = visual.GetComponent<Renderer>(); if (r != null) r.SetPropertyBlock(null);   // drop the focus tint it was wearing
            var fly = visual.gameObject.AddComponent<PickupFly>();
            fly._to = to; fly._from = visual.position; fly._scale = visual.localScale;
        }

        void Update()
        {
            _t += Time.deltaTime / Seconds;
            if (_t >= 1f || _to == null) { Destroy(gameObject); return; }
            float k = _t * _t;   // slow off the ground, quick into the hand
            transform.position = Vector3.Lerp(_from, _to.position + Vector3.up * ChestHeight, k) + Vector3.up * (Arc * Mathf.Sin(_t * Mathf.PI));
            transform.localScale = _scale * Mathf.Lerp(1f, 0.15f, k);
            transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World);
        }
    }
}
