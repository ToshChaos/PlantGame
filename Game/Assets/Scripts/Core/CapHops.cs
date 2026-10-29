using System.Collections.Generic;
using UnityEngine;
using PlantGame.Plants;

namespace PlantGame.Core
{
    /// <summary>
    /// Mushroom patches: from the ground (or the neighbouring cap) the player can hop onto the next cap with E (or a tap on its circle).
    /// Cap 1 from the ground near the patch or from cap 2, cap 2 from cap 1 or 3, cap 3 from cap 2.
    /// </summary>
    public static class CapHops
    {
        public struct Option { public Vector3 point; public PlantInstance plant; public int index; }

        public const float GroundReach = 4.5f;
        static readonly List<Vector3> _tops = new List<Vector3>();
        static readonly List<float> _radii = new List<float>();

        /// <summary>Caps the player at 'pos' can hop to right now (cleared first).</summary>
        public static void Find(Vector3 pos, List<Option> into)
        {
            into.Clear();
            foreach (var pl in PlantInstance.All)
            {
                if (pl.kind != PlantKind.Platform || pl.CapTops(_tops, _radii) < 2) continue;
                int on = -1;
                for (int i = 0; i < _tops.Count && on < 0; i++)
                {
                    Vector2 d = new Vector2(pos.x - _tops[i].x, pos.z - _tops[i].z);
                    if (d.magnitude <= _radii[i] + 0.5f && Mathf.Abs(pos.y - _tops[i].y) < 0.7f) on = i;
                }
                if (on >= 0)
                {
                    if (on > 0) into.Add(new Option { point = _tops[on - 1], plant = pl, index = on - 1 });
                    if (on < _tops.Count - 1) into.Add(new Option { point = _tops[on + 1], plant = pl, index = on + 1 });
                }
                else
                {
                    Vector2 d = new Vector2(pos.x - _tops[0].x, pos.z - _tops[0].z);
                    if (d.magnitude <= GroundReach && pos.y < _tops[0].y - 0.3f && pos.y > _tops[0].y - 4f)
                        into.Add(new Option { point = _tops[0], plant = pl, index = 0 });
                }
            }
        }

        /// <summary>The option best lined up with the camera / facing direction.</summary>
        public static bool Pick(List<Option> opts, Vector3 pos, Vector3 facing, out Option best)
        {
            best = default; float bs = -2f;
            facing.y = 0f; facing.Normalize();
            foreach (var o in opts)
            {
                Vector3 d = o.point - pos; d.y = 0f;
                float s = d.sqrMagnitude < 0.01f ? 0f : Vector3.Dot(facing, d.normalized);
                if (s > bs) { bs = s; best = o; }
            }
            return bs > -2f;
        }
    }
}
