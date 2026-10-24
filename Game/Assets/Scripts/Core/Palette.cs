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
        /// <summary>Vine seed (basic seed): a lime that stays readable against the grassy island tops.</summary>
        public static readonly Color SeedLime   = Hex("B8E35F");
        /// <summary>T33 seed colours: bamboo (vine seed), bean (beanstalk seed), succulent (dew seed); the puffball (shelf seed) is cloud white.</summary>
        public static readonly Color SeedBamboo = Hex("6BE23B");
        public static readonly Color SeedBean = Hex("8B4FD6");
        public static readonly Color SeedSucculent = Hex("2DBDAA");
        /// <summary>Mushroom caps (platform plant): red with white spots, white stalks.</summary>
        public static readonly Color MushroomRed = Hex("D6453D");
        /// <summary>Yellow flowers on the bridge (white ones use cloud white).</summary>
        public static readonly Color FlowerYellow = Hex("F2C84B");
        /// <summary>The parachute leaf (pickup and the open glide canopy).</summary>
        public static readonly Color LeafYellow = Hex("F2D94E");
        /// <summary>Tree bark (trunks and branches).</summary>
        public static readonly Color Bark       = Hex("7B5638");
        /// <summary>Furrowed earth of the soil patches (planting spots).</summary>
        public static readonly Color Soil       = Hex("7A5230");
        /// <summary>HUD text accents: prompts and warnings in orange, labels in purple (user request).</summary>
        public static readonly Color TextOrange = Hex("FF9A3C");
        public static readonly Color TextPurple = Hex("B58CFF");
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
