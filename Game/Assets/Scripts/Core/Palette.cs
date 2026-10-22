using UnityEngine;

namespace PlantGame.Core
{
    /// <summary>Art-direction palette, exact hexes from AGENTS.md 5.7 / docs/art-bible.md.</summary>
    public static class Palette
    {
        public static readonly Color CloudWhite = Hex("F4F8F6");
        public static readonly Color MistBlue   = Hex("BFD9EE");
        public static readonly Color SkyBlue    = Hex("6FA8DC");
        public static readonly Color DeepBlue   = Hex("2C5E8F");
        public static readonly Color LeafGreen  = Hex("5DB46B");
        public static readonly Color Moss       = Hex("2F7D4F");
        /// <summary>Magic / accent only: glow, rim, water edge.</summary>
        public static readonly Color GlowCyan   = Hex("7CF2E6");
        /// <summary>Player character only (T31): white / black / gold.</summary>
        public static readonly Color PlayerWhite = Hex("F2F2EE");
        public static readonly Color PlayerBlack = Hex("17181C");
        public static readonly Color Gold        = Hex("D9A441");

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex.TrimStart('#'), out var c);
            return c;
        }
    }
}
