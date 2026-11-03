using System;
using System.Reflection;
using UnityEditor;

namespace PlantGame.Editor
{
    /// <summary>
    /// T63: sets the Game view to the reference phone (a 2022 phone at 1080 x 2340, landscape) so HUD and menu fit can be checked in the
    /// editor. Menu: PlantGame > Tools > Game View: Phone 2340x1080. Uses the editor's internal GameViewSizes (no public API).
    /// </summary>
    public static class PhoneGameView
    {
        public const int Width = 2340, Height = 1080;

        [MenuItem("PlantGame/Tools/Game View: Phone 2340x1080")]
        public static void Apply() => Set(Width, Height, "Phone 2340x1080");

        public static void Set(int width, int height, string label)
        {
            var asm = typeof(EditorWindow).Assembly;
            var sizesType = asm.GetType("UnityEditor.GameViewSizes");
            var instance = typeof(ScriptableSingleton<>).MakeGenericType(sizesType).GetProperty("instance").GetValue(null);
            var group = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { sizesType.GetProperty("currentGroupType").GetValue(instance) });
            var groupType = group.GetType();
            int count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null), index = -1;
            for (int i = 0; i < count && index < 0; i++)
            {
                var s = groupType.GetMethod("GetGameViewSize").Invoke(group, new object[] { i });
                if ((int)s.GetType().GetProperty("width").GetValue(s) == width && (int)s.GetType().GetProperty("height").GetValue(s) == height) index = i;
            }
            if (index < 0)
            {
                var sizeType = asm.GetType("UnityEditor.GameViewSize"); var kind = asm.GetType("UnityEditor.GameViewSizeType");
                var size = sizeType.GetConstructor(new[] { kind, typeof(int), typeof(int), typeof(string) }).Invoke(new object[] { Enum.ToObject(kind, 1), width, height, label });   // 1 = fixed resolution
                groupType.GetMethod("AddCustomSize").Invoke(group, new[] { size });
                index = count;
            }
            var gameView = EditorWindow.GetWindow(asm.GetType("UnityEditor.GameView"));
            gameView.GetType().GetMethod("SizeSelectionCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(gameView, new object[] { index, null });
        }
    }
}
