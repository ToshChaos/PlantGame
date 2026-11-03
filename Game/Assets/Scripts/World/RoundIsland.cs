using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;

namespace PlantGame.World
{
    /// <summary>
    /// A rounded floating-island mesh: a gently rolling top (world-scale UVs), a ragged rim and a rocky underside whose colour differs
    /// per island (grey, brown, sandstone, slate...). The outline is wobbled per island (deterministic from `seed`). MeshCollider, layer Ground.
    /// The transform sits at the centre of the TOP surface; the rolling terrain only ever rises above that plane (0..~0.4 m) except in
    /// carved water basins and flattened spots, so ponds and streams sit in real hollows. See IslandTerrain.
    /// </summary>
    public static class RoundIsland
    {
        public static GameObject Create(string name, Vector3 topCentre, float radius, float depth, Material topMat, Material sideMat, int seed, float tile = 3f, float wobble = 0.25f)
        {
            var rnd0 = new System.Random(seed * 7919 + 13);
            return Create(name, topCentre, new IslandShape { radius = radius, wobble = wobble, p1 = (float)rnd0.NextDouble() * 6.28f, p2 = (float)rnd0.NextDouble() * 6.28f }, depth, topMat, sideMat, seed, tile);
        }

        public static GameObject Create(string name, Vector3 topCentre, IslandShape shape, float depth, Material topMat, Material sideMat, int seed, float tile = 3f)
        {
            var go = new GameObject(name);
            go.transform.position = topCentre;
            var t = go.AddComponent<IslandTerrain>();
            t.shape = shape; t.depth = depth; t.seed = seed; t.tile = tile;
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>().sharedMaterials = new[] { topMat, RockTint(sideMat, seed) };
            go.AddComponent<MeshCollider>();
            int ground = LayerMask.NameToLayer(Layers.GroundName);
            if (ground >= 0) go.layer = ground;
            t.Rebuild();
            return go;
        }

        // rock types for the underside: multiplied with the rock texture
        static readonly Color[] RockColors =
        {
            new Color(0.62f, 0.63f, 0.67f),   // grey granite
            new Color(0.58f, 0.55f, 0.52f),   // warm grey (was brown earth: the undersides read grey, not brown)
            new Color(0.74f, 0.72f, 0.66f),   // pale sandy grey
            new Color(0.78f, 0.86f, 0.94f),   // pale blue-grey (the original look)
            new Color(0.42f, 0.42f, 0.47f),   // dark slate
            new Color(0.62f, 0.57f, 0.55f),   // rosy grey (was red clay)
        };
        static readonly Dictionary<string, Material> RockMats = new Dictionary<string, Material>();

        static Material RockTint(Material baseMat, int seed)
        {
            if (baseMat == null) return null;
            int k = (int)((uint)(seed * 2654435761u) >> 8) % RockColors.Length;
            string key = baseMat.name + "#" + k;
            if (RockMats.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(baseMat) { name = key };
            m.SetColor("_BaseColor", RockColors[k]);
            RockMats[key] = m;
            return m;
        }
    }

    /// <summary>
    /// Outline of an island: an ellipse (aspect, rotation) with lobes, an optional bay (concave bite) and a small wobble.
    /// Edge(angle) is the distance from the island centre to its rim in that direction (angle in radians, (cos a, sin a) = (x, z)),
    /// so layout code can put soils, bridges and decorations on the real rim instead of a circle.
    /// </summary>
    public class IslandShape
    {
        public float radius = 10f, aspect = 1f, rot, lobeN = 3f, lobeAmp, lobeP, bayAngle, bayDepth, bayWidth = 0.5f, wobble = 0.25f, p1, p2;

        public float Edge(float a)
        {
            float th = a - rot, A = radius * Mathf.Sqrt(aspect), B = radius / Mathf.Sqrt(aspect);
            float c = B * Mathf.Cos(th), s = A * Mathf.Sin(th);
            float r = A * B / Mathf.Sqrt(c * c + s * s);
            r *= 1f + lobeAmp * Mathf.Sin(lobeN * a + lobeP);
            float d = Mathf.Repeat(a - bayAngle + Mathf.PI, Mathf.PI * 2f) - Mathf.PI;
            r *= 1f - bayDepth * Mathf.Exp(-(d * d) / (2f * bayWidth * bayWidth));
            r += wobble * (0.6f * Mathf.Sin(a * 3f + p1) + 0.4f * Mathf.Sin(a * 5f + p2));
            return Mathf.Max(r, radius * 0.5f);
        }

        public float EdgeDeg(float degrees) => Edge(degrees * Mathf.Deg2Rad);

        public float MaxEdge { get { float m = 0; for (int i = 0; i < 72; i++) m = Mathf.Max(m, Edge(i * Mathf.PI * 2f / 72f)); return m; } }

        /// <summary>Random but deterministic shape of mean size `radius`: elongated, lobed, sometimes with a bay.</summary>
        public static IslandShape Random(System.Random rnd, float radius, float wobble)
        {
            var sh = new IslandShape { radius = radius, wobble = wobble };
            sh.aspect = 1f + (float)rnd.NextDouble() * 0.55f;
            sh.rot = (float)rnd.NextDouble() * Mathf.PI;
            sh.lobeN = 2 + rnd.Next(4);
            sh.lobeAmp = 0.06f + (float)rnd.NextDouble() * 0.14f;
            sh.lobeP = (float)rnd.NextDouble() * 6.28f;
            if (rnd.NextDouble() < 0.5) { sh.bayAngle = (float)rnd.NextDouble() * 6.28f; sh.bayDepth = 0.15f + (float)rnd.NextDouble() * 0.15f; sh.bayWidth = 0.4f + (float)rnd.NextDouble() * 0.3f; }
            sh.p1 = (float)rnd.NextDouble() * 6.28f; sh.p2 = (float)rnd.NextDouble() * 6.28f;
            return sh;
        }
    }
}
