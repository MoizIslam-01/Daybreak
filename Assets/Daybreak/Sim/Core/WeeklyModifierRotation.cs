namespace Daybreak.Sim
{
    /// <summary>
    /// Picks the active weekly modifier from the week number, cycling a fixed roster. This is the
    /// cheapest longevity lever in the game (just numbers) and the biggest driver of "ugh, they
    /// changed it again" chatter. Pure and deterministic so client and server agree on the week's rule.
    /// </summary>
    public static class WeeklyModifierRotation
    {
        /// <summary>The rotation, in order. Week 0 is the plain baseline; twists follow.</summary>
        public static WeeklyModifier[] Cycle()
        {
            return new[]
            {
                WeeklyModifier.None,
                WeeklyModifier.Entrenched,
                WeeklyModifier.GlassCannons,
                WeeklyModifier.VanguardsHour,
                WeeklyModifier.ArcaneSurge
            };
        }

        public static WeeklyModifier ForWeek(int week)
        {
            var cycle = Cycle();
            int i = ((week % cycle.Length) + cycle.Length) % cycle.Length; // safe for negative weeks
            return cycle[i];
        }

        /// <summary>Resolve a modifier by id (for regenerating a stored replay exactly).</summary>
        public static WeeklyModifier ById(string id)
        {
            if (!string.IsNullOrEmpty(id))
                foreach (var m in Cycle())
                    if (m.Id == id) return m;
            return WeeklyModifier.None;
        }
    }
}
