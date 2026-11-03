using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PlantGame.Core;
using PlantGame.Inventory;
using PlantGame.Plants;
using PlantGame.World;

namespace PlantGame.Editor
{
    /// <summary>
    /// Fills Resources/WorldKit (materials, seed items, baked plant prefabs, player, curated levels) for the runtime world generator,
    /// and creates the curated LevelDefinition assets. Menu: PlantGame > 14 Build World Kit. Rerun after adding materials, items or prefabs.
    /// </summary>
    public static class WorldKitBuilder
    {
        public const string KitPath = "Assets/Resources/WorldKit.asset", LevelDir = "Assets/ScriptableObjects/World";

        [MenuItem("PlantGame/15 Build World Kit", false, 15)]
        public static void Build()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CollectorPrefabBuilder.PrefabPath(1)) == null && File.Exists("Assets/Art/Models/Plants/Plant_Collector_01.fbx")) CollectorPrefabBuilder.Build();
            if (MaterialBuilder.Load("M_Terrain") == null) MaterialBuilder.Build();
            if (MaterialBuilder.Load("M_Water") == null) WaterMaterialBuilder.Build();
            if (ItemBuilder.Load("Item_VineSeed") == null) ItemBuilder.Build();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BridgePrefabBuilder.PrefabPath(1)) == null) BridgePrefabBuilder.Build();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LadderPrefabBuilder.PrefabPath(1)) == null) LadderPrefabBuilder.Build();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlatformPrefabBuilder.PrefabPath(1)) == null) PlatformPrefabBuilder.Build();
            var player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabBuilder.PrefabPath) ?? PlayerPrefabBuilder.Build();

            Directory.CreateDirectory("Assets/Resources"); Directory.CreateDirectory(LevelDir);
            var kit = AssetDatabase.LoadAssetAtPath<WorldKit>(KitPath);
            if (kit == null) { kit = ScriptableObject.CreateInstance<WorldKit>(); AssetDatabase.CreateAsset(kit, KitPath); }

            EnsureThemeMaterials();
            var mats = new List<Material>();
            foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { MaterialBuilder.Dir })) mats.Add(AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g)));
            var leaves = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Quaternius/Q_Leaves_NormalTree.mat"); if (leaves != null) mats.Add(leaves);   // leaf cards for procedural crowns
            kit.materials = mats.ToArray();
            var items = new List<ItemDefinition>();
            foreach (var g in AssetDatabase.FindAssets("t:ItemDefinition", new[] { ItemBuilder.ItemDir })) items.Add(AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(g)));
            kit.items = items.ToArray();
            kit.bridges = Prefabs(PlantKind.Bridge); kit.ladders = Prefabs(PlantKind.Ladder); kit.platforms = Prefabs(PlantKind.Platform); kit.collectors = Prefabs(PlantKind.Collector);
            kit.player = player;
            var props = new List<GameObject>();
            foreach (var g in AssetDatabase.FindAssets("t:Model", new[] { KenneyPropBuilder.ModelDir, QuaterniusPropBuilder.ModelDir, RuinsPropBuilder.ModelDir })) props.Add(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)));
            kit.props = props.ToArray();

            var levels = new List<LevelDefinition>();
            var B = PlantKind.Bridge; var L = PlantKind.Ladder; var P = PlantKind.Platform; var G = PlantKind.Glider;
            levels.Add(Level("level_1", "1: First Steps", "Plant, water, cross. A bridge and a bridge: that is all you need.", 1101, 3, LevelTheme.Sunny, GoalKind.Gate, WaterMode.Sources, new[] { B, B }));
            levels.Add(Level("level_2", "2: Beanstalk Hill", "An autumn afternoon: golden trees and falling leaves. Add a beanstalk.", 2207, 4, LevelTheme.Autumn, GoalKind.Fruit, WaterMode.Sources, new[] { B, L, B }));
            levels.Add(Level("level_3", "3: Water from the Sky", "Rain, but no water to carry: a dew patch on the first island holds one drop; grow a Water Collector from the second island's dew seed and wait for every charge.", 3357, 4, LevelTheme.Rain, GoalKind.Fire, WaterMode.CollectorOnly, new[] { B, P, L }));
            levels.Add(Level("level_4", "4: Skyfall", "Climb the beanstalk. At the top, pick a leaf from the glowing leaf plant (Q), jump off the edge and press E in the air to glide.", 4413, 5, LevelTheme.Sunset, GoalKind.Gate, WaterMode.Sources, new[] { B, L, G, B }));
            levels.Add(Level("level_5", "5: Stepping Stones", "Seven islands, six gaps and only four seed slots. Plan what you carry.", 20261, 7, LevelTheme.Sunny, GoalKind.Gate, WaterMode.Sources, null));
            var back = Level("level_6", "6: There and Back", "Something precious waits far away. Bring it home.", 6619, 3, LevelTheme.Sunrise, GoalKind.Gate, WaterMode.Sources, new[] { B, L });
            back.roundTrip = true; EditorUtility.SetDirty(back);
            levels.Add(back);
            // T50: ten levels
            var snowy = Level("level_7", "7: Snowed In", "Snow on every ledge. The campfire waits at the end of the white.", 7719, 5, LevelTheme.Snow, GoalKind.Fire, WaterMode.Sources, new[] { B, L, P, B });
            var windy = Level("level_8", "8: Windy Summit", "Climb to the top. The wind pushes harder the higher you go.", 8843, 6, LevelTheme.Sunny, GoalKind.Flag, WaterMode.Sources, new[] { B, L, L, B, L });
            windy.wind = true; windy.spiral = true; EditorUtility.SetDirty(windy);
            var fog = Level("level_9", "9: Into the Mist", "Dense fog. Look for the warm glow, listen for water.", 9967, 5, LevelTheme.Fog, GoalKind.Gate, WaterMode.Sources, new[] { B, P, B, L });
            var storm = Level("level_10", "10: Storm Season", "Eight islands, rain and gusts. Everything you have learned.", 10303, 8, LevelTheme.Rain, GoalKind.Fruit, WaterMode.Sources, new[] { B, L, B, P, L, B, P });
            storm.wind = true; EditorUtility.SetDirty(storm);
            levels.Add(snowy); levels.Add(windy); levels.Add(fog); levels.Add(storm);
            kit.levels = levels.ToArray();
            EditorUtility.SetDirty(kit);
            AssetDatabase.SaveAssets();
            Debug.Log($"[PlantGame] WorldKit built: {kit.materials.Length} materials, {kit.items.Length} items, plants {kit.bridges.Length}/{kit.ladders.Length}/{kit.platforms.Length}/{kit.collectors.Length}, {kit.levels.Length} levels, {kit.props.Length} props.");
        }

        /// <summary>Autumn recolours of the island top and foliage (Scenery.Mat swaps them in): clones of the green materials.</summary>
        static void EnsureThemeMaterials()
        {
            Clone("M_TerrainAutumn", "M_Terrain", Palette.Hex("A98F3C"));
            Clone("M_AutumnGold", "M_FoliageLight", Palette.Hex("F2C23C"));
            Clone("M_AutumnRed", "M_Foliage", Palette.Hex("C8502D"));
            var orange = MaterialBuilder.Load("M_Autumn"); if (orange != null) { orange.SetColor("_BaseColor", Palette.Hex("E8892E")); EditorUtility.SetDirty(orange); }
        }

        static void Clone(string name, string from, Color tint)
        {
            var src = MaterialBuilder.Load(from);
            if (src == null) return;
            string path = $"{MaterialBuilder.Dir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(src) { name = name }; AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(m);
        }

        static GameObject[] Prefabs(PlantKind kind)
        {
            var list = new List<GameObject>();
            for (int n = 1; n <= 6; n++)
            {
                var g = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Plants/Plant_{kind}_{n:00}.prefab");
                if (g != null) list.Add(g);
            }
            return list.ToArray();
        }

        static LevelDefinition Level(string id, string title, string desc, int seed, int islands, LevelTheme theme, GoalKind goal, WaterMode water, PlantKind[] sequence)
        {
            string path = $"{LevelDir}/{id}.asset";
            var d = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (d == null) { d = ScriptableObject.CreateInstance<LevelDefinition>(); AssetDatabase.CreateAsset(d, path); }
            d.id = id; d.title = title; d.description = desc; d.seed = seed; d.islandCount = islands;
            d.sideIslands = id == "level_2" || id == "level_4" ? 1 : 0;   // an optional dead-end island off the main chain
            d.sequence = sequence; d.goal = goal; d.waterMode = water; d.roundTrip = false; d.spiral = false;
            d.allowedKinds = new[] { PlantKind.Bridge, PlantKind.Ladder, PlantKind.Platform };
            d.theme = theme; d.wind = false;
            EditorUtility.SetDirty(d);
            return d;
        }
    }
}
