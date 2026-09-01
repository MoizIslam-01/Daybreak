namespace Daybreak.Sim
{
    /// <summary>
    /// Outcome of one battle. A stored replay is just (squadA, squadB, modifier, seed);
    /// the log regenerates by re-running the sim.
    /// </summary>
    public sealed class BattleResult
    {
        public const int SideA = 0;
        public const int SideB = 1;

        public int WinnerSide;
        public BattleEvent[] Log;
        public int Seed;

        /// <summary>Winner's summed remaining HP — the weekly leaderboard tiebreak (guide §6.2).</summary>
        public int WinnerRemainingHp;

        /// <summary>Convenience for tests/harness: the last event, always a BattleEnd.</summary>
        public bool WonByA => WinnerSide == SideA;
    }
}
