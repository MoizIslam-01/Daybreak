using System;

namespace Daybreak.Sim
{
    /// <summary>
    /// Day and week numbering shared by client and server. Pure: it takes a UTC time in rather than
    /// reading the clock, so it never violates the sim's no-wall-clock rule while still giving both
    /// sides one agreed definition of "which day is this."
    ///
    /// Battles resolve at 20:00 UTC daily (a locked decision). The integer day rolls at midnight
    /// UTC; the scheduler fires the resolve for that day at 20:00. Between 20:00 and midnight, a new
    /// lock targets tomorrow (its window has already resolved), which LockTargetDay accounts for.
    /// </summary>
    public static class GameCalendar
    {
        public const int ResolveHourUtc = 20;

        // Day 0 begins at this instant. Arbitrary fixed anchor so both sides agree.
        private static readonly DateTime Epoch = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>Whole days since the epoch (rolls at midnight UTC).</summary>
        public static int DayNumber(DateTime utc)
        {
            var span = utc.ToUniversalTime() - Epoch;
            return (int)Math.Floor(span.TotalDays);
        }

        /// <summary>
        /// The day a lock placed now is competing on. Before 20:00 UTC that's today; at/after 20:00
        /// today's battles have already resolved, so the lock targets tomorrow.
        /// </summary>
        public static int LockTargetDay(DateTime utc)
        {
            var u = utc.ToUniversalTime();
            return DayNumber(u) + (u.Hour >= ResolveHourUtc ? 1 : 0);
        }

        /// <summary>The day the resolve running at ~20:00 UTC should process (today's locks).</summary>
        public static int ResolveDayFor(DateTime utc) => DayNumber(utc);

        /// <summary>Weekly leaderboard bucket. Reset happens when this increments.</summary>
        public static int WeekNumber(int day) => (int)Math.Floor(day / 7.0);
    }
}
