using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using PlantGame.Core;
using PlantGame.Player;

namespace PlantGame.Editor
{
    /// <summary>
    /// T03/T31: builds Assets/Prefabs/Player/Player.prefab (CharacterController + input + third-person orbit camera + model).
    /// The model is Art/Models/Characters/Char_Player_01.fbx (Source/Blender/scripts/char_player.py); mesh name suffix picks
    /// the PlayerRole (Skin, Hair, Cloth, Cloak, Gear, Trim, Glow, Leaf, Dark); PlayerOutfit assets map roles to materials. Without the FBX the prefab still builds (no model).
    /// Menu: PlantGame > 4 Build Player Prefab. Needs layers from "1 Setup Project" (tag/layer fall back gracefully).
    /// </summary>
    public static class PlayerPrefabBuilder
    {
        public const string PrefabPath = "Assets/Prefabs/Player/Player.prefab";
        public const string InputPath = "Assets/Scripts/Player/PlantGameInput.inputactions";
        public const string OutfitPath = "Assets/ScriptableObjects/Player/Outfit_Explorer.asset";
        public const string ModelPath = "Assets/Art/Models/Characters/Char_Player_01.fbx";

        [MenuItem("PlantGame/4 Build Player Prefab", false, 4)]
        public static GameObject Build()
        {
            var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath);
            if (input == null) { Debug.LogError("[PlantGame] Input actions not found at " + InputPath); return null; }

            var root = new GameObject("Player");
            int layer = LayerMask.NameToLayer(Layers.PlayerName);
            if (layer >= 0) root.layer = layer; else Debug.LogWarning("[PlantGame] Layer 'Player' missing; run PlantGame > 1 Setup Project.");
            root.tag = Layers.PlayerTag;

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f; cc.slopeLimit = 45f; cc.skinWidth = 0.03f;

            var reader = root.AddComponent<PlayerInputReader>();
            var so = new SerializedObject(reader);
            so.FindProperty("actions").objectReferenceValue = input;
            so.ApplyModifiedPropertiesWithoutUndo();

            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(root.transform, false);
            pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var camGo = new GameObject("Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 0f, -4.5f);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f; cam.farClipPlane = 600f; cam.fieldOfView = 60f;
            cam.clearFlags = CameraClearFlags.Skybox;       // T16: the Atmosphere assigns the skybox
            cam.backgroundColor = Palette.MistBlue;
            camGo.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
            camGo.AddComponent<AudioListener>();

            var pc = root.AddComponent<PlayerController>();
            pc.cameraPivot = pivot.transform;
            pc.cameraTransform = camGo.transform;

            var model = BuildModel(root.transform, layer);

            // T04/T05: interaction, inventory, watering can (stub until T27)
            root.AddComponent<PlantGame.Inventory.Inventory>();
            root.AddComponent<PlantGame.Plants.WateringCan>();
            var sensor = root.AddComponent<InteractionSensor>();
            sensor.eye = camGo.transform;
            var interaction = root.AddComponent<PlayerInteraction>();
            interaction.input = reader;
            interaction.model = model;
            root.AddComponent<PlantGame.Core.InteractionMarkers>();   // diamond / circle world markers
            root.AddComponent<PlantGame.UI.PlayerHud>();              // hotbar, prompt, water, pause menu

            if (model != null)
            {
                pc.model = model;
                root.AddComponent<PlantGame.Plants.WateringCanVisual>();
                var anim = root.AddComponent<PlayerAnimator>();
                anim.controller = pc; anim.model = model;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] Player prefab saved: " + PrefabPath);
            return prefab;
        }

        /// <summary>The default Explorer outfit asset: one M_Player&lt;Role&gt; material per PlayerRole (kept if it already exists, missing slots refilled).</summary>
        static PlayerOutfit BuildOutfit()
        {
            var outfit = AssetDatabase.LoadAssetAtPath<PlayerOutfit>(OutfitPath);
            if (outfit == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(OutfitPath));
                outfit = ScriptableObject.CreateInstance<PlayerOutfit>();
                AssetDatabase.CreateAsset(outfit, OutfitPath);
            }
            var roles = (PlayerRole[])System.Enum.GetValues(typeof(PlayerRole));
            if (outfit.materials == null || outfit.materials.Length != roles.Length) outfit.materials = new Material[roles.Length];
            foreach (var role in roles)
            {
                if (outfit.materials[(int)role] == null) outfit.materials[(int)role] = MaterialBuilder.Load("M_Player" + role);
                if (outfit.materials[(int)role] == null) Debug.LogWarning("[PlantGame] M_Player" + role + " missing; run PlantGame > 2 Build Materials.");
            }
            EditorUtility.SetDirty(outfit);
            return outfit;
        }

        /// <summary>"Model" child holding the FBX instance (unpacked, so the prefab owns it) with player materials.</summary>
        static Transform BuildModel(Transform parent, int layer)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (fbx == null) { Debug.LogWarning("[PlantGame] Player model missing at " + ModelPath + " (run char_player.py + sync_to_unity.py)."); return null; }
            var outfit = BuildOutfit();

            var holder = new GameObject("Model").transform;
            holder.SetParent(parent, false);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, holder);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = Quaternion.identity;

            foreach (var t in holder.GetComponentsInChildren<Transform>(true))
            {
                if (layer >= 0) t.gameObject.layer = layer;
                var r = t.GetComponent<MeshRenderer>();
                if (r == null) continue;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            var applier = holder.gameObject.AddComponent<PlayerOutfitApplier>();
            applier.outfit = outfit;
            applier.Apply();
            return holder;
        }
    }
}
