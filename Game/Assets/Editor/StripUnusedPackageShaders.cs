using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;

namespace PlantGame.Editor
{
    /// <summary>
    /// T72: com.unity.ai.inference is only here for the editor tooling (nothing in Assets references it), but its Resources folder forces its compute and pixel
    /// shaders into every player: about 10 MB and well over a thousand shader warnings per clean build. Strips all their variants, on every platform; the empty
    /// assets stay. If the game ever runs a model with the Inference Engine, delete this file.
    /// </summary>
    class StripUnusedPackageShaders : IPreprocessShaders, IPreprocessComputeShaders
    {
        const string Package = "Packages/com.unity.ai.inference/";

        public int callbackOrder => 0;

        static bool Unused(Object shader) => AssetDatabase.GetAssetPath(shader).StartsWith(Package);

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            if (Unused(shader)) data.Clear();
        }

        public void OnProcessComputeShader(ComputeShader shader, string kernelName, IList<ShaderCompilerData> data)
        {
            if (Unused(shader)) data.Clear();
        }
    }
}
