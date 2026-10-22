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
    /// the material: _W M_PlayerWhite, _K M_PlayerBlack, _G M_PlayerGold. Without the FBX the prefab still builds (no model).
    /// Menu: PlantGame > 4 Build Player Prefab. Needs layers from "1 Setup Project" (tag/layer fall back gracefully).
    /// </summary>
    public static class PlayerPrefabBuilder
    {
        public const string PrefabPath = "Assets/Prefabs/Player/Player.prefab";
        public const string InputPath = "Assets/Scripts/Player/PlantGameInput.inputactions";
        public const string ModelPath = "Assets/Art/Models/Characters/Char_Player_01.fbx";

        [MenuItem("PlantGame/4 Build Player Prefab")]
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
            cam.clearFlags = CameraClearFlags.SolidColor;   // T16 replaces this with the skybox
            cam.backgroundColor = Palette.MistBlue;
            camGo.AddComponent<AudioListener>();

            var pc = root.AddComponent<PlayerController>();
            pc.cameraPivot = pivot.transform;
            pc.cameraTransform = camGo.transform;

            var model = BuildModel(root.transform, layer);
            if (model != null)
            {
                pc.model = model;
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

        /// <summary>"Model" child holding the FBX instance (unpacked, so the prefab owns it) with player materials.</summary>
        static Transform BuildModel(Transform parent, int layer)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (fbx == null) { Debug.LogWarning("[PlantGame] Player model missing at " + ModelPath + " (run char_player.py + sync_to_unity.py)."); return null; }
            var white = MaterialBuilder.Load("M_PlayerWhite");
            var black = MaterialBuilder.Load("M_PlayerBlack");
            var gold = MaterialBuilder.Load("M_PlayerGold");
            if (white == null || black == null || gold == null) Debug.LogWarning("[PlantGame] Player materials missing; run PlantGame > 2 Build Materials.");

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
                string n = t.name;
                r.sharedMaterial = n.EndsWith("_G") ? gold : n.EndsWith("_K") ? black : white;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            return holder;
        }
    }
}
