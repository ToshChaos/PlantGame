using System.Collections.Generic;
using System.Text;
using PlantGame.Plants;

namespace PlantGame.Core
{
    /// <summary>
    /// Shareable, human-typable level code made of five lowercase words joined by dashes, e.g. "mossy-heron-gentle-brook-amber".
    /// 40 bits = 24-bit seed | islandCount 4b | kindsMask 4b | genVersion 4b | checksum 4b, one word (of 256) per byte.
    /// Case-insensitive; spaces, dashes and underscores all separate words. Seeds are 24 bits (RunConfig.seed must stay below 16,777,216
    /// for a code to give the same world). Old "LEAF-XXXX-XXXX-XX" codes are still read. Water mode and theme are not stored (random levels
    /// use ponds and streams; the theme comes from the seed).
    /// </summary>
    public static class SeedCode
    {
        public const int GeneratorVersion = 3;   // bump when world-gen output changes; old codes then warn
        public const int SeedBits = 24, SeedMask = (1 << SeedBits) - 1;
        public const string LegacyPrefix = "LEAF";
        const string LegacyAlphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
        const int LegacySymbols = 9;

        static readonly string[] Words =
        {
            "amber", "azure", "brave", "breezy", "bright", "calm", "clever", "cloudy", "cosy", "crisp",
            "dainty", "dusky", "eager", "early", "fancy", "fluffy", "gentle", "giddy", "glad", "golden",
            "grand", "happy", "hardy", "hazy", "honest", "humble", "icy", "jolly", "keen", "kind",
            "lazy", "leafy", "lively", "lucky", "lunar", "mellow", "merry", "mighty", "misty", "modest",
            "mossy", "nimble", "noble", "odd", "plain", "plucky", "polite", "proud", "quick", "quiet",
            "rapid", "rosy", "rustic", "sandy", "shiny", "silent", "silky", "sleepy", "slow", "smooth",
            "snowy", "soft", "sparky", "speedy", "spicy", "steady", "sunny", "sweet", "swift", "tidy",
            "tiny", "velvet", "vivid", "warm", "wild", "windy", "wise", "witty", "young", "zesty",
            "cheery", "coral", "dewy", "dreamy", "earthy", "elfin", "fresh", "frosty", "gleamy", "grassy",
            "hollow", "indigo", "ivory", "jade", "lilac", "minty", "olive", "opal", "pearl", "plump",
            "rainy", "ruby", "sage", "silver", "spry", "stormy", "teal", "tawny", "thorny", "toasty",
            "umber", "wooly", "woven", "airy", "balmy", "bold", "chilly", "cuddly", "dapper", "fond",
            "glossy", "gusty", "hushy", "lofty", "lush", "mild", "nutty", "perky", "pretty", "regal",
            "sturdy", "tender", "vast", "wavy", "acorn", "badger", "beacon", "birch", "breeze", "brook",
            "bumble", "butter", "canyon", "clover", "comet", "cricket", "daisy", "dune", "eagle", "ember",
            "fern", "finch", "firefly", "fjord", "fox", "frog", "gale", "glade", "grove", "harbor",
            "hare", "hazel", "heron", "hill", "iris", "island", "ivy", "jay", "kite", "lagoon",
            "lark", "lily", "lotus", "marsh", "meadow", "minnow", "moth", "newt", "oak", "orchid",
            "otter", "owl", "panda", "pebble", "pine", "pond", "poppy", "puffin", "quail", "rabbit",
            "raven", "reed", "ridge", "river", "robin", "seed", "shore", "skylark", "sparrow", "spring",
            "sprout", "squirrel", "stream", "swallow", "thistle", "thrush", "tulip", "turtle", "valley", "violet",
            "vole", "willow", "wren", "yarrow", "zephyr", "bramble", "cedar", "cliff", "cloud", "cove",
            "creek", "crow", "dawn", "dew", "drift", "elm", "falcon", "flint", "gust", "heath",
            "hawk", "juniper", "kestrel", "leaf", "lynx", "maple", "moss", "mushroom", "nest", "orchard",
            "petal", "pollen", "quill", "rain", "root", "sky", "sun", "stone", "thorn", "vine",
            "wave", "wind", "bee", "berry", "bloom", "branch",
        };
        static Dictionary<string, int> _index;
        static Dictionary<string, int> Index
        {
            get
            {
                if (_index != null) return _index;
                _index = new Dictionary<string, int>();
                for (int i = 0; i < Words.Length; i++) _index[Words[i]] = i;
                return _index;
            }
        }

        public static int KindsMask(PlantKind[] kinds)
        {
            int m = 0;
            if (kinds != null) foreach (var k in kinds) m |= 1 << (int)k;
            return m & 0xF;
        }

        public static PlantKind[] KindsFromMask(int mask)
        {
            var l = new List<PlantKind>();
            for (int i = 0; i < 4; i++) if ((mask & (1 << i)) != 0) l.Add((PlantKind)i);
            return l.ToArray();
        }

        public static string Encode(RunConfig c) => Encode(c.seed & SeedMask, c.islandCount, KindsMask(c.allowedKinds), GeneratorVersion);

        public static string Encode(int seed, int islands, int kindsMask, int version)
        {
            var nib = new int[10];   // nibble 0..5 seed (24 bits), 6 islands, 7 mask, 8 version, 9 checksum
            for (int i = 0; i < 6; i++) nib[i] = (seed >> (20 - i * 4)) & 0xF;
            nib[6] = islands & 0xF; nib[7] = kindsMask & 0xF; nib[8] = version & 0xF;
            int sum = 0; for (int i = 0; i < 9; i++) sum += nib[i] * (i + 1);
            nib[9] = sum & 0xF;
            var sb = new StringBuilder();
            for (int w = 0; w < 5; w++)
            {
                if (w > 0) sb.Append('-');
                sb.Append(Words[nib[w * 2] * 16 + nib[w * 2 + 1]]);
            }
            return sb.ToString();
        }

        public static bool TryDecode(string code, out RunConfig c) => TryDecode(code, out c, out _);

        /// <summary>error is a friendly message (empty on success). A code from another generator version still decodes (true) but sets `error` to a warning.</summary>
        public static bool TryDecode(string code, out RunConfig c, out string error)
        {
            c = default; error = "";
            if (string.IsNullOrWhiteSpace(code)) { error = "Type or paste a seed code."; return false; }
            var up = code.Trim().ToUpperInvariant();
            var parts = code.Trim().ToLowerInvariant().Split(new[] { '-', ' ', '_' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (up.StartsWith(LegacyPrefix) && (parts.Length != 5 || !Index.ContainsKey(parts[1]))) return TryDecodeLegacy(up, out c, out error);   // "leaf" is also a seed word, so only fall back to the old format when this is not five words
            if (parts.Length != 5) { error = parts.Length < 5 ? "A seed code has five words, joined by dashes." : "That code has too many words: it should be five."; return false; }
            var nib = new int[10];
            for (int w = 0; w < 5; w++)
            {
                if (!Index.TryGetValue(parts[w], out int v)) { error = "'" + parts[w] + "' is not a seed word. Check the spelling."; return false; }
                nib[w * 2] = v >> 4; nib[w * 2 + 1] = v & 0xF;
            }
            int sum = 0; for (int i = 0; i < 9; i++) sum += nib[i] * (i + 1);
            if (nib[9] != (sum & 0xF)) { error = "The words do not match each other: one of them is mistyped."; return false; }
            int seed = 0; for (int i = 0; i < 6; i++) seed = (seed << 4) | nib[i];
            return Finish(seed, nib[6], nib[7], nib[8], out c, out error);
        }

        static bool Finish(int seed, int islands, int mask, int version, out RunConfig c, out string error)
        {
            c = default; error = "";
            if (islands < 4 || islands > 8) { error = "Island count out of range."; return false; }
            if ((mask & 7) == 0) { error = "This code allows no plants."; return false; }
            c = new RunConfig { seed = seed, islandCount = islands, allowedKinds = KindsFromMask(mask & 7), levelId = null, mode = GameMode.Exploration, waterMode = WaterMode.Sources };
            if (version != GeneratorVersion) error = "This code is from another game version (" + version + "); the world may differ.";
            return true;
        }

        // ---------------------------------------------------------------- old LEAF-XXXX-XXXX-XX codes (32-bit seeds)
        static bool TryDecodeLegacy(string upper, out RunConfig c, out string error)
        {
            c = default; error = "";
            var sb = new StringBuilder();
            foreach (char ch in upper.Replace("-", "").Replace(" ", "").Replace("_", "").Substring(LegacyPrefix.Length)) sb.Append(ch == 'O' ? '0' : ch == 'I' || ch == 'L' ? '1' : ch);
            var s = sb.ToString();
            if (s.Length != LegacySymbols + 1) { error = "That code is too " + (s.Length < LegacySymbols + 1 ? "short." : "long."); return false; }
            ulong v = 0; int sum = 0;
            for (int i = 0; i < LegacySymbols; i++)
            {
                int sym = LegacyAlphabet.IndexOf(s[i]);
                if (sym < 0) { error = "Unexpected character '" + s[i] + "'."; return false; }
                v = (v << 5) | (uint)sym; sum += sym * (i + 1);
            }
            int chk = LegacyAlphabet.IndexOf(s[LegacySymbols]);
            if (chk < 0 || chk != sum % 32) { error = "Check symbol does not match: a character was mistyped."; return false; }
            return Finish(unchecked((int)(uint)(v & 0xFFFFFFFF)), (int)((v >> 32) & 0xF), (int)((v >> 36) & 0xF), (int)((v >> 40) & 0xF), out c, out error);
        }
    }
}
