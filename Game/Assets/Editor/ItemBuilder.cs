using System.IO;
using UnityEditor;
using UnityEngine;
using PlantGame.Core;
using PlantGame.Inventory;
using PlantGame.World;

namespace PlantGame.Editor
{
    /// <summary>
    /// Builds the ItemDefinition assets (ScriptableObjects/Items) and world prefabs (Prefabs/Items):
    /// a shaped, softly glowing pickup mesh (SeedMeshes) with a trigger collider on layer Interactable and a WorldItem.
    /// Menu: PlantGame > 6 Build Items. Idempotent (assets updated in place so references survive).
    /// </summary>
    public static class ItemBuilder
    {
        public const string ItemDir = "Assets/ScriptableObjects/Items";
        public const string PrefabDir = "Assets/Prefabs/Items";

        static readonly (string id, string name, string asset, Color color, ItemKind kind)[] Defs =
        {
            ("vine_seed", "Vine", "Item_VineSeed", Palette.SeedBamboo, ItemKind.Seed),
            ("beanstalk_seed", "Beanstalk", "Item_BeanstalkSeed", Palette.SeedBean, ItemKind.Seed),
            ("shelf_seed", "Mushroom", "Item_ShelfSeed", Palette.CloudWhite, ItemKind.Seed),
            ("dew_seed", "Dew", "Item_DewSeed", Palette.SeedSucculent, ItemKind.Seed),
            ("parachute_leaf", "Parachute Leaf", "Item_ParachuteLeaf", Palette.LeafYellow, ItemKind.Misc),
            ("heart_seed", "Heart", "Item_HeartSeed", new Color(0.9f, 0.14f, 0.2f), ItemKind.Key),
            ("heat_seed", "Heat pumpkin", "Item_HeatSeed", new Color(1f, 0.55f, 0.1f), ItemKind.Misc),
            ("coconut", "Coconut", "Item_Coconut", new Color(0.55f, 0.37f, 0.21f), ItemKind.Misc),   // only palms drop one; nothing can plant it
        };

        [MenuItem("PlantGame/Assets/Items", false, 12)]
        public static void Build()
        {
            Directory.CreateDirectory(ItemDir); Directory.CreateDirectory(PrefabDir);
            if (MaterialBuilder.Load("M_Crystal") == null) MaterialBuilder.Build();
            foreach (var d in Defs)
            {
                var path = $"{ItemDir}/{d.asset}.asset";
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item == null) { item = ScriptableObject.CreateInstance<ItemDefinition>(); AssetDatabase.CreateAsset(item, path); }
                item.id = d.id; item.displayName = d.name; item.kind = d.kind;
                item.worldPrefab = BuildPrefab(d.asset.Replace("Item_", "WorldItem_"), item, d.color);
                EditorUtility.SetDirty(item);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] Items built in " + ItemDir);
        }

        public static ItemDefinition Load(string asset) => AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{ItemDir}/{asset}.asset");

        static string SeedTexture(string id)
        {
            switch (id) { case "vine_seed": return "T_SeedBamboo"; case "beanstalk_seed": return "T_SeedBean"; case "shelf_seed": return "T_SeedPuff"; case "dew_seed": return "T_SeedSucc"; case "coconut": return "T_SeedCoconut"; case "heart_seed": return "T_SeedHeart"; case "heat_seed": return "T_SeedPumpkin"; default: return null; }
        }

        /// <summary>Seed pickup material: the mesh carries the detail, plain colour with a soft glow and the item's seed-coat texture.</summary>
        static Material SeedMaterial(string matName, Color color, string itemId)
        {
            var path = $"{MaterialBuilder.Dir}/{matName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = matName }; AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", color);
            string tex = SeedTexture(itemId);
            if (tex != null && TextureBuilder.Albedo(tex) == null) TextureBuilder.Build();
            mat.SetTexture("_BaseMap", tex != null ? TextureBuilder.Albedo(tex) : null);
            mat.SetTexture("_BumpMap", tex != null ? TextureBuilder.Normal(tex) : null);
            if (tex != null) { mat.SetFloat("_BumpScale", 1f); mat.EnableKeyword("_NORMALMAP"); } else mat.DisableKeyword("_NORMALMAP");
            mat.SetFloat("_Smoothness", 0.45f);
            MaterialBuilder.SetEmission(mat, color * 0.22f);
            var glow = itemId == "heat_seed" ? AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureBuilder.Dir}/T_SeedPumpkin_E.png") : null;
            mat.SetTexture("_EmissionMap", glow);
            if (glow != null) MaterialBuilder.SetEmission(mat, new Color(1.5f, 1.5f, 1.5f));   // the glowing face; the rind is black in the map
            MaterialBuilder.ValidateUrp(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>Yellow, slightly glowing leaf material shared by the pickup and the open glide leaf (Kit.Mat("M_GlideLeaf")).</summary>
        public static Material LeafMaterial()
        {
            var path = $"{MaterialBuilder.Dir}/M_GlideLeaf.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) { mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_GlideLeaf" }; AssetDatabase.CreateAsset(mat, path); }
            mat.SetColor("_BaseColor", Palette.LeafYellow); mat.SetFloat("_Smoothness", 0.3f); mat.SetFloat("_Metallic", 0f);
            MaterialBuilder.SetEmission(mat, Palette.LeafYellow * 0.22f);
            MaterialBuilder.ValidateUrp(mat);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        /// <summary>The parachute leaf pickup: a flat pointed yellow blade (about 0.9 m tip to tip) that rests on the ground.</summary>
        static GameObject BuildLeafPrefab(string name, ItemDefinition item)
        {
            Directory.CreateDirectory("Assets/Art/Models/Items");
            const string meshPath = "Assets/Art/Models/Items/Leaf_Pickup.asset";
            var src = PlantGame.World.LeafMesh.Build(0.95f, 0.36f, 0.04f, 0f);   // no V fold: a folded blade collapses to a half-moon when seen from low angles
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null) { mesh = Object.Instantiate(src); mesh.name = "Leaf_Pickup"; AssetDatabase.CreateAsset(mesh, meshPath); }
            else { mesh.Clear(); EditorUtility.CopySerialized(src, mesh); mesh.name = "Leaf_Pickup"; }
            var verts = mesh.vertices;   // hover about a forearm over the ground: the island top is lumpy and would swallow part of a flat blade
            var tilt = Quaternion.Euler(24f, 0f, 0f);   // leaning a little about its length so the outline reads from the side as well
            for (int i = 0; i < verts.Length; i++) { verts[i] = tilt * verts[i]; verts[i].y += 0.25f; }
            mesh.vertices = verts; mesh.RecalculateBounds(); EditorUtility.SetDirty(mesh);

            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = LeafMaterial();
            var solid = go.AddComponent<SphereCollider>(); solid.radius = 0.04f; solid.center = new Vector3(0, 0.045f, 0);   // small and low: the flat blade rests on the ground instead of sinking in
            var pick = go.AddComponent<SphereCollider>(); pick.isTrigger = true; pick.radius = 0.7f;                         // generous pick area
            int layer = LayerMask.NameToLayer(Layers.InteractableName);
            if (layer >= 0) go.layer = layer; else Debug.LogWarning("[PlantGame] Layer 'Interactable' missing; run PlantGame > 1 Setup Project.");
            go.AddComponent<WorldItem>().item = item;
            go.AddComponent<ItemBob>();
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = false; rb.useGravity = true; rb.linearDamping = 1.5f; rb.constraints = RigidbodyConstraints.FreezeRotation;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        /// <summary>Bamboo / bean / puffball / succulent pickups. The mesh is centred on the origin, a small sphere collider lets it rest on the ground.</summary>
        static GameObject BuildShapedPrefab(string name, ItemDefinition item, Color color)
        {
            Directory.CreateDirectory("Assets/Art/Models/Items");
            string meshPath = "Assets/Art/Models/Items/" + name.Replace("WorldItem_", "Seed_") + ".asset";
            var src = SeedMeshes.For(item.id);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null) { mesh = Object.Instantiate(src); AssetDatabase.CreateAsset(mesh, meshPath); }
            else { mesh.Clear(); EditorUtility.CopySerialized(src, mesh); }
            mesh.name = Path.GetFileNameWithoutExtension(meshPath);
            EditorUtility.SetDirty(mesh);

            var go = new GameObject(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = SeedMaterial("M_" + name.Replace("WorldItem_", "Seed"), color, item.id);
            var solid = go.AddComponent<SphereCollider>(); solid.radius = SeedMeshes.RestRadius(item.id);
            var pick = go.AddComponent<SphereCollider>(); pick.isTrigger = true; pick.radius = 0.7f;
            int layer = LayerMask.NameToLayer(Layers.InteractableName);
            if (layer >= 0) go.layer = layer; else Debug.LogWarning("[PlantGame] Layer 'Interactable' missing; run PlantGame > 1 Setup Project.");
            go.AddComponent<WorldItem>().item = item;
            go.AddComponent<ItemBob>();
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = false; rb.useGravity = true; rb.linearDamping = 1.5f; rb.constraints = RigidbodyConstraints.FreezeRotation;
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject BuildPrefab(string name, ItemDefinition item, Color color)
        {
            if (item.id == "parachute_leaf") return BuildLeafPrefab(name, item);
            if (SeedMeshes.For(item.id) == null) throw new System.InvalidOperationException($"[PlantGame] Item '{item.id}' has no pickup mesh: add it to SeedMeshes.For");
            return BuildShapedPrefab(name, item, color);
        }
    }
}
