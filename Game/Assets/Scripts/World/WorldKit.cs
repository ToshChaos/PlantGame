using System.Collections.Generic;
using UnityEngine;
using PlantGame.Core;
using PlantGame.Inventory;
using PlantGame.Plants;

namespace PlantGame.World
{
    /// <summary>
    /// Everything the runtime world generator needs from the project (materials, items, baked plant prefabs, the player).
    /// One asset at Resources/WorldKit, filled by the editor menu `PlantGame > 14 Build World Kit`, so a standalone build carries it.
    /// </summary>
    public class WorldKit : ScriptableObject
    {
        public Material[] materials;
        public ItemDefinition[] items;
        public GameObject[] bridges, ladders, platforms, collectors;
        public GameObject player;
        /// <summary>Imported scenery models (Kenney CC0 kits), looked up by file name via Kit.Prop.</summary>
        public GameObject[] props;
        public LevelDefinition[] levels;
    }

    /// <summary>Static lookups into the loaded WorldKit.</summary>
    public static class Kit
    {
        static WorldKit _kit;
        static Dictionary<string, Material> _mats;
        static Dictionary<string, ItemDefinition> _items;
        static Dictionary<string, GameObject> _props;

        public static WorldKit Asset
        {
            get
            {
                if (_kit == null) { _kit = Resources.Load<WorldKit>("WorldKit"); _mats = null; _items = null; _props = null; }
                return _kit;
            }
        }

        public static Material Mat(string name)
        {
            var a = Asset;   // first: loading the asset resets the caches
            if (_mats == null || a == null)
            {
                _mats = new Dictionary<string, Material>();
                if (a != null && a.materials != null) foreach (var m in a.materials) if (m != null) _mats[m.name] = m;
            }
            return _mats.TryGetValue(name, out var r) ? r : null;
        }

        /// <summary>Imported scenery model by file name (e.g. "tree_oak"), or null when the kit has no props (procedural fallback).</summary>
        public static GameObject Prop(string name)
        {
            var a = Asset;
            if (_props == null || a == null)
            {
                _props = new Dictionary<string, GameObject>();
                if (a != null && a.props != null) foreach (var g in a.props) if (g != null) _props[g.name] = g;
            }
            return _props.TryGetValue(name, out var r) ? r : null;
        }

        public static ItemDefinition Item(string id)
        {
            var a = Asset;
            if (_items == null || a == null)
            {
                _items = new Dictionary<string, ItemDefinition>();
                if (a != null && a.items != null) foreach (var i in a.items) if (i != null) _items[i.id] = i;
            }
            return _items.TryGetValue(id, out var r) ? r : null;
        }

        public static GameObject[] Plants(PlantKind k)
        {
            var a = Asset; if (a == null) return new GameObject[0];
            switch (k) { case PlantKind.Bridge: return a.bridges; case PlantKind.Ladder: return a.ladders; case PlantKind.Platform: return a.platforms; default: return a.collectors; }
        }
    }
}
