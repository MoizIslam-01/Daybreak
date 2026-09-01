namespace Daybreak.Sim
{
    /// <summary>
    /// The weekly rule twist, applied inside the sim at battle start (guide §6.3). Kept as plain
    /// integer/permille data so it stays deterministic and, later, loadable from config.
    ///
    /// Effects come in two flavours:
    ///   • battle-start stat multipliers (permille; 1000 = no change), folded into effective stats
    ///     alongside synergies and Berserk — see EffectiveStats in BattleSimulator.
    ///   • damage-time rules (e.g. back-row damage reduction), applied when damage is dealt.
    ///
    /// Milestone 1 ships None plus the four examples from the guide. Config-driven loading and
    /// rotation is Milestone 6 — the shape here is deliberately simple to serialize.
    /// </summary>
    public sealed class WeeklyModifier
    {
        public string Id = "none";

        // --- Battle-start global stat multipliers (permille). ---
        public int AtkPermille = 1000;
        public int DefPermille = 1000;
        public int HpPermille = 1000;
        public int SpdPermille = 1000;

        // --- Battle-start archetype-targeted multiplier (applies to ALL stats of one archetype). ---
        public bool HasArchetypeBuff = false;
        public Archetype BuffArchetype = Archetype.Vanguard;
        public int ArchetypeAllStatsPermille = 1000;

        // --- Damage-time: multiplier on damage taken by back-row units (permille). ---
        public int BackRowDamageTakenPermille = 1000;

        // --- Arcane synergy: double the true-damage thresholds (0/4/9 -> 0/8/18). ---
        public bool DoubleArcaneTrueDamage = false;

        public bool IsNone => Id == "none";

        /// <summary>No twist. The default matchup.</summary>
        public static WeeklyModifier None => new WeeklyModifier { Id = "none" };

        /// <summary>Back-row units take 25% less damage. Rewards protected formations.</summary>
        public static WeeklyModifier Entrenched => new WeeklyModifier
        {
            Id = "entrenched",
            BackRowDamageTakenPermille = 750
        };

        /// <summary>All units +20% ATK, -20% HP. Everything dies faster.</summary>
        public static WeeklyModifier GlassCannons => new WeeklyModifier
        {
            Id = "glass_cannons",
            AtkPermille = 1200,
            HpPermille = 800
        };

        /// <summary>Vanguards +15% to all stats. Tank week.</summary>
        public static WeeklyModifier VanguardsHour => new WeeklyModifier
        {
            Id = "vanguards_hour",
            HasArchetypeBuff = true,
            BuffArchetype = Archetype.Vanguard,
            ArchetypeAllStatsPermille = 1150
        };

        /// <summary>Arcane true-damage thresholds doubled. Rewards stacking Arcane.</summary>
        public static WeeklyModifier ArcaneSurge => new WeeklyModifier
        {
            Id = "arcane_surge",
            DoubleArcaneTrueDamage = true
        };
    }
}
