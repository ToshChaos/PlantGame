using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using PlantGame.Core;
using PlantGame.Player;

namespace PlantGame.Editor
{
    /// <summary>
    /// T03: builds Assets/Prefabs/Player/Player.prefab (CharacterController + input + first-person camera).
    /// Menu: PlantGame > 4 Build Player Prefab. Needs layers from "1 Setup Project" (tag/layer fall back gracefully).
    /// </summary>
    public static class PlayerPrefabBuilder
    {
        public const string PrefabPath = "Assets/Prefabs/Player/Player.prefab";
        public const string InputPath = "Assets/Scripts/Player/PlantGameInput.inputactions";

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
            pivot.transform.localPosition = new Vector3(0f, 1.62f, 0f);
            var camGo = new GameObject("Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(pivot.transform, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 600f; cam.fieldOfView = 75f;
            cam.clearFlags = CameraClearFlags.SolidColor;   // T16 replaces this with the skybox
            cam.backgroundColor = Palette.MistBlue;
            camGo.AddComponent<AudioListener>();

            var pc = root.AddComponent<PlayerController>();
            pc.cameraPivot = pivot.transform;

            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[PlantGame] Player prefab saved: " + PrefabPath);
            return prefab;
        }
    }
}
