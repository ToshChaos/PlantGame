using UnityEngine;
using PlantGame.Core;

namespace PlantGame.World
{
    /// <summary>What the top of an island is made of: grass, bare mud or rock. Mud and rock islands carry far less vegetation and more boulders.</summary>
    public enum GroundKind { Grass, Mud, Rock }

    public static class Ground
    {
        /// <summary>One ground per island, deterministic from the layout seed. The start island is always grass; larger worlds always get some bare ground.</summary>
        public static GroundKind[] Assign(int layoutSeed, int count)
        {
            var g = new GroundKind[count];
            bool bare = false;
            for (int i = 1; i < count; i++)
            {
                double d = new System.Random(unchecked(layoutSeed * 40503 + i * 7919 + 3)).NextDouble();
                g[i] = d < 0.45 ? GroundKind.Grass : d < 0.75 ? GroundKind.Mud : GroundKind.Rock;
                if (g[i] != GroundKind.Grass) bare = true;
            }
            int nBare = 0; for (int i = 0; i < count; i++) if (g[i] != GroundKind.Grass) nBare++;
            for (int i = 1; i < count && nBare > count / 2; i += 2) if (g[i] != GroundKind.Grass) { g[i] = GroundKind.Grass; nBare--; }   // at most half the islands are bare
            for (int i = count - 1; i >= 1 && nBare > count / 2; i--) if (g[i] != GroundKind.Grass) { g[i] = GroundKind.Grass; nBare--; }
            bare = nBare > 0;
            if (!bare && count >= 4) g[1 + (layoutSeed & 0xFF) % (count - 2)] = (layoutSeed & 0x100) == 0 ? GroundKind.Mud : GroundKind.Rock;
            return g;
        }

        static Material _snow;
        /// <summary>T50: snow-covered ground: the grass material without its texture, tinted white-blue.</summary>
        static Material SnowMat()
        {
            if (_snow != null) return _snow;
            var b = Scenery.Mat("M_Terrain"); if (b == null) return null;
            _snow = new Material(b) { name = "M_TerrainSnow", hideFlags = HideFlags.DontSave };
            if (_snow.HasProperty("_BaseMap")) _snow.SetTexture("_BaseMap", null);
            _snow.SetColor("_BaseColor", new Color(0.93f, 0.96f, 1f));
            return _snow;
        }

        public static Material Top(GroundKind k, bool snowCapped = false)
        {
            if ((snowCapped || Scenery.Theme == LevelTheme.Snow) && k != GroundKind.Rock) { var sm = SnowMat(); if (sm != null) return sm; }
            switch (k)
            {
                case GroundKind.Mud: return Kit.Mat("M_TerrainMud") ?? Scenery.Mat("M_Terrain");
                case GroundKind.Rock: return Kit.Mat("M_TerrainRock") ?? Scenery.Mat("M_Terrain");
                default: return Scenery.Mat("M_Terrain");
            }
        }

        /// <summary>Scales the number of trees, bushes and flowers.</summary>
        public static float Vegetation(GroundKind k) => k == GroundKind.Mud ? 0.45f : k == GroundKind.Rock ? 0.2f : 1f;

        /// <summary>Boulders and slabs added on top of the usual ruin.</summary>
        public static int Boulders(GroundKind k) => k == GroundKind.Mud ? 4 : k == GroundKind.Rock ? 9 : 3;
    }
}
