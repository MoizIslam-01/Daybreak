namespace Daybreak.Sim
{
    /// <summary>
    /// Rules for player-set profile fields, shared by client and server so validation is identical
    /// wherever a name is accepted. Pure and Unity-free.
    /// </summary>
    public static class ProfileRules
    {
        public const int MaxNameLength = 16;
        public const int MinNameLength = 1;
        public const string DefaultName = "Player";
        public const string DefaultColorHex = "#7C9CBF";

        /// <summary>
        /// Trim, collapse inner whitespace, strip control characters, and clamp to the length limit.
        /// Empty or all-whitespace names fall back to the default so nothing is ever nameless.
        /// </summary>
        public static string SanitizeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return DefaultName;

            var sb = new System.Text.StringBuilder(raw.Length);
            bool prevSpace = false;
            foreach (char c in raw)
            {
                // Whitespace first: a tab is BOTH whitespace and a control char, and we want to
                // collapse it to a space, not strip it.
                if (char.IsWhiteSpace(c))
                {
                    if (prevSpace || sb.Length == 0) continue; // no leading or doubled spaces
                    sb.Append(' ');
                    prevSpace = true;
                }
                else if (char.IsControl(c))
                {
                    continue;
                }
                else
                {
                    sb.Append(c);
                    prevSpace = false;
                }

                if (sb.Length >= MaxNameLength) break;
            }

            // Drop any trailing space left by the clamp.
            while (sb.Length > 0 && sb[sb.Length - 1] == ' ') sb.Length--;

            return sb.Length >= MinNameLength ? sb.ToString() : DefaultName;
        }

        /// <summary>Accepts "#RRGGBB" (case-insensitive); anything else becomes the default color.</summary>
        public static string SanitizeColor(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length != 7 || hex[0] != '#') return DefaultColorHex;
            for (int i = 1; i < 7; i++)
            {
                char c = hex[i];
                bool ok = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!ok) return DefaultColorHex;
            }
            return hex.ToUpperInvariant();
        }

        public static ProfileDto Sanitize(ProfileDto profile)
        {
            if (profile == null) profile = new ProfileDto();
            return new ProfileDto
            {
                displayName = SanitizeName(profile.displayName),
                colorHex = SanitizeColor(profile.colorHex),
                emoji = string.IsNullOrEmpty(profile.emoji) ? "" : profile.emoji,
                teamId = profile.teamId ?? ""
            };
        }
    }
}
