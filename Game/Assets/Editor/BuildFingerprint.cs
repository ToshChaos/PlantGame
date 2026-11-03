using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlantGame.Editor
{
    /// <summary>
    /// Builders regenerate scenes and prefabs from scratch, and Unity gives every new object a random file ID when it saves, so an
    /// unchanged rebuild would still rewrite the whole file in git. This fingerprints what a scene / hierarchy would save (every
    /// serialized property, references written as hierarchy or asset paths) so a builder can skip the save when nothing changed.
    /// The fingerprint of a freshly built scene and of the same scene loaded from disk are equal.
    /// </summary>
    public static class BuildFingerprint
    {
        /// <summary>Fingerprint of the scene file at `path` (opens it); empty when there is no file yet.</summary>
        public static string OfSavedScene(string path)
        {
            if (!File.Exists(path)) return "";
            var text = Describe(EditorSceneManager.OpenScene(path, OpenSceneMode.Single));
            Saved[path] = text;
            return Hash128.Compute(text).ToString();
        }
        static readonly Dictionary<string, string> Saved = new Dictionary<string, string>();   // the described saved scene, written out next to the new one on a mismatch

        /// <summary>
        /// Saves the freshly built `scene` to `path` unless it matches `before` (OfSavedScene, taken before building). When it matches, the saved
        /// file is reopened instead, so the editor ends on the same scene either way. Returns true when it saved.
        /// </summary>
        public static bool SaveIfChanged(Scene scene, string path, string before)
        {
            var text = Describe(scene);
            if (before != "" && Hash128.Compute(text).ToString() == before)
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Debug.Log($"[PlantGame] {Path.GetFileName(path)} unchanged, not re-saved.");
                return false;
            }
            if (Saved.TryGetValue(path, out var old))   // what differed, for when a rebuild saves although nothing should have changed
            {
                var stem = "Temp/BuildFingerprint_" + Path.GetFileNameWithoutExtension(path);
                File.WriteAllText(stem + "_saved.txt", old); File.WriteAllText(stem + "_built.txt", text);
            }
            EditorSceneManager.SaveScene(scene, path);
            return true;
        }

        public static string Of(Scene scene) => Hash128.Compute(Describe(scene)).ToString();

        static string Describe(Scene scene)
        {
            var sb = new StringBuilder();
            foreach (var settings in SceneSettings()) Append(sb, settings, null);
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++) Walk(sb, roots[i].transform, i.ToString());
            return sb.ToString();
        }

        /// <summary>Fingerprint of a hierarchy that becomes a prefab (properties named `skip` are left out, e.g. a stored hash).</summary>
        /// <param name="name">when set, the described text is kept in Temp/BuildFingerprint_name_last.txt (the one before in _prev.txt) to compare two builds</param>
        public static string Of(GameObject root, string skip = null, string name = null)
        {
            var sb = new StringBuilder();
            _top = root.transform;   // reference paths stop at the prefab root (its place among the open scene's roots does not count)
            try { Walk(sb, root.transform, "0", skip); } finally { _top = null; }
            if (name != null)
            {
                string last = "Temp/BuildFingerprint_" + name + "_last.txt", prev = "Temp/BuildFingerprint_" + name + "_prev.txt";
                if (File.Exists(last)) File.Copy(last, prev, true);
                File.WriteAllText(last, sb.ToString());
            }
            return Hash128.Compute(sb.ToString()).ToString();
        }

        static Transform _top;

        static void Walk(StringBuilder sb, Transform t, string path, string skip = null)
        {
            sb.Append('#').Append(path).Append(' ').Append(t.name).Append('\n');
            Append(sb, t.gameObject, skip);
            foreach (var c in t.GetComponents<Component>()) if (c != null) Append(sb, c, skip);
            for (int i = 0; i < t.childCount; i++) Walk(sb, t.GetChild(i), path + "/" + i, skip);
        }

        static void Append(StringBuilder sb, Object o, string skip)
        {
            sb.Append(o.GetType().Name).Append('{');
            var it = new SerializedObject(o).GetIterator();
            bool enter = true;
            while (it.Next(enter))
            {
                // a reference's children are its file / path IDs (they differ between a built and a loaded scene), a string's are its characters
                enter = it.propertyType != SerializedPropertyType.ObjectReference && it.propertyType != SerializedPropertyType.String;
                if (skip != null && it.name == skip) continue;
                switch (it.propertyType)
                {
                    case SerializedPropertyType.Float: sb.Append(it.name).Append('=').Append(it.doubleValue.ToString("R")).Append(';'); break;
                    case SerializedPropertyType.Integer: case SerializedPropertyType.LayerMask: case SerializedPropertyType.ArraySize: case SerializedPropertyType.Character:
                        sb.Append(it.name).Append('=').Append(it.longValue).Append(';'); break;
                    case SerializedPropertyType.Enum: sb.Append(it.name).Append('=').Append(it.intValue).Append(';'); break;
                    case SerializedPropertyType.Boolean: sb.Append(it.name).Append('=').Append(it.boolValue).Append(';'); break;
                    case SerializedPropertyType.String: sb.Append(it.name).Append('=').Append(it.stringValue).Append(';'); break;
                    case SerializedPropertyType.ObjectReference: sb.Append(it.name).Append('=').Append(Ref(it.objectReferenceValue)).Append(';'); break;
                }
            }
            sb.Append("}\n");
        }

        /// <summary>A reference as something that survives a save: asset path, or path in the hierarchy, or (embedded generated objects) a content summary.</summary>
        static string Ref(Object o)
        {
            if (o == null) return "-";
            if (EditorUtility.IsPersistent(o)) return AssetDatabase.GetAssetPath(o) + ":" + o.name + ":" + o.GetType().Name;
            Transform t = o is GameObject g ? g.transform : o is Component c ? c.transform : null;
            if (t != null)
            {
                var p = new StringBuilder();
                for (var x = t; x != null && x != _top; x = x.parent) p.Insert(0, "/" + x.GetSiblingIndex());
                return p + ":" + o.GetType().Name;
            }
            if (o is Mesh m) return "mesh:" + m.name + ":" + m.vertexCount + ":" + m.bounds;
            return o.GetType().Name + ":" + o.name;
        }

        /// <summary>The scene's lighting / render settings objects (they are saved with the scene but are not GameObjects). Internal Unity API, hence reflection.</summary>
        static IEnumerable<Object> SceneSettings()
        {
            const BindingFlags f = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            var rs = typeof(RenderSettings).GetMethod("GetRenderSettings", f)?.Invoke(null, null) as Object;
            if (rs != null) yield return rs;
            var ls = System.Type.GetType("UnityEditor.LightmapEditorSettings, UnityEditor")?.GetMethod("GetLightmapSettings", f)?.Invoke(null, null) as Object;
            if (ls != null) yield return ls;
        }
    }
}
