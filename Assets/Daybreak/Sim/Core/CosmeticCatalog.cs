using System.Collections.Generic;

namespace Daybreak.Sim
{
    /// <summary>
    /// The cosmetic shop's inventory. Titles are short flair shown next to a player's name —
    /// bragging rights only, no gameplay effect. Kept as shared data so client and server agree on
    /// prices. (Skins/banners/frames come later; the shape here scales to them.)
    /// </summary>
    public static class CosmeticCatalog
    {
        private static readonly CosmeticDto[] Items =
        {
            Title("underdog", "Underdog", 30),
            Title("tactician", "Tactician", 50),
            Title("gladiator", "Gladiator", 50),
            Title("veteran", "Veteran", 60),
            Title("strategist", "Strategist", 75),
            Title("warlord", "Warlord", 100),
            Title("mastermind", "Mastermind", 150),
        };

        /// <summary>The weekly champion cosmetic — dated, ungrindable, granted only on reset.</summary>
        public const string DawnCrownPrefix = "dawn_crown_w";

        public static string DawnCrownId(int week) => DawnCrownPrefix + week;

        public static bool IsDawnCrown(string id) =>
            !string.IsNullOrEmpty(id) && id.StartsWith(DawnCrownPrefix);

        public static IReadOnlyList<CosmeticDto> All() => Items;

        public static CosmeticDto Get(string id)
        {
            if (!string.IsNullOrEmpty(id))
                foreach (var c in Items)
                    if (c.id == id) return c;
            return null;
        }

        /// <summary>Human-readable name for any cosmetic id, including the special Dawn Crown ids.</summary>
        public static string DisplayName(string id)
        {
            if (string.IsNullOrEmpty(id)) return "";
            if (IsDawnCrown(id)) return "Dawn Crown (Wk " + id.Substring(DawnCrownPrefix.Length) + ")";
            var c = Get(id);
            return c != null ? c.name : "";
        }

        private static CosmeticDto Title(string id, string name, int cost) =>
            new CosmeticDto { id = id, name = name, kind = "title", cost = cost };
    }
}
