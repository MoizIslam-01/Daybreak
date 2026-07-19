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
    }

    /// <summary>The weekly rule twist, applied at battle start. Params stay data-driven.</summary>
    public sealed class WeeklyModifier
    {
        public static readonly WeeklyModifier None = new WeeklyModifier { Id = "none" };

        public string Id;
    }
}
