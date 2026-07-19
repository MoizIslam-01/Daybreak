using System;

namespace Daybreak.Sim
{
    /// <summary>
    /// The authoritative battle resolver: pure, deterministic, and Unity-free so the identical
    /// code runs in the client (practice/preview) and in the Cloud Code module (real resolves).
    ///
    /// Rules of the house — enforce these in review:
    ///   no floats, no System.Random, no DateTime, no unordered collection iteration.
    ///
    /// Milestone 1 fills this in. The signature is fixed now so the client and the server module
    /// can be wired against it in Milestone 0.
    /// </summary>
    public static class BattleSimulator
    {
        /// <summary>Hard cap on rounds; at the cap, higher remaining HP percentage wins.</summary>
        public const int MaxRounds = 30;

        /// <summary>Archetype damage multipliers, in permille to keep the math integral.</summary>
        public const int FavorablePermille = 1250;
        public const int NeutralPermille = 1000;
        public const int UnfavorablePermille = 800;

        public static BattleResult Simulate(Squad a, Squad b, WeeklyModifier mod, int seed, UnitCatalog defs)
        {
            throw new NotImplementedException(
                "BattleSimulator.Simulate lands in Milestone 1. See section 4.5 of the design guide for the full spec.");
        }

        /// <summary>
        /// The counter triangle: Vanguard beats Skirmisher beats Marksman beats Vanguard.
        /// Implemented in M0 because the whole design hangs off it and it is trivially testable.
        /// </summary>
        public static int ArchetypeMultiplierPermille(Archetype attacker, Archetype defender)
        {
            if (attacker == defender) return NeutralPermille;
            return Beats(attacker, defender) ? FavorablePermille : UnfavorablePermille;
        }

        private static bool Beats(Archetype attacker, Archetype defender)
        {
            switch (attacker)
            {
                case Archetype.Vanguard: return defender == Archetype.Skirmisher;
                case Archetype.Skirmisher: return defender == Archetype.Marksman;
                case Archetype.Marksman: return defender == Archetype.Vanguard;
                default: return false;
            }
        }
    }
}
