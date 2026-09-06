namespace Daybreak.Sim
{
    /// <summary>
    /// Pure rules for team names and ids, shared by client and server. A team id is a URL-safe slug
    /// derived from the name (with a fallback), so it's readable and stable.
    /// </summary>
    public static class TeamRules
    {
        public const int MaxNameLength = 20;
        public const string DefaultName = "Team";
        public const string DefaultColorHex = "#7C9CBF";

        public static string SanitizeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return DefaultName;

            var sb = new System.Text.StringBuilder(raw.Length);
            bool prevSpace = false;
            foreach (char c in raw)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (prevSpace || sb.Length == 0) continue;
                    sb.Append(' ');
                    prevSpace = true;
                }
                else if (char.IsControl(c)) continue;
                else { sb.Append(c); prevSpace = false; }

                if (sb.Length >= MaxNameLength) break;
            }
            while (sb.Length > 0 && sb[sb.Length - 1] == ' ') sb.Length--;
            return sb.Length > 0 ? sb.ToString() : DefaultName;
        }

        /// <summary>Lowercase URL-safe slug: alnum kept, runs of anything else become one dash.</summary>
        public static string Slug(string name)
        {
            if (string.IsNullOrEmpty(name)) return "team";

            var sb = new System.Text.StringBuilder(name.Length);
            bool prevDash = false;
            foreach (char c in name)
            {
                char lower = char.ToLowerInvariant(c);
                bool alnum = (lower >= 'a' && lower <= 'z') || (lower >= '0' && lower <= '9');
                if (alnum) { sb.Append(lower); prevDash = false; }
                else if (!prevDash && sb.Length > 0) { sb.Append('-'); prevDash = true; }
            }
            while (sb.Length > 0 && sb[sb.Length - 1] == '-') sb.Length--;
            return sb.Length > 0 ? sb.ToString() : "team";
        }
    }
}
