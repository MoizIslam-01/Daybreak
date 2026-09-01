using System;
using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class GameCalendarTests
    {
        private static DateTime Utc(int y, int mo, int d, int h, int mi) =>
            new DateTime(y, mo, d, h, mi, 0, DateTimeKind.Utc);

        [Test]
        public void DayNumber_RollsAtMidnightUtc()
        {
            int d1 = GameCalendar.DayNumber(Utc(2026, 3, 10, 23, 59));
            int d2 = GameCalendar.DayNumber(Utc(2026, 3, 11, 0, 1));
            Assert.AreEqual(d1 + 1, d2);
        }

        [Test]
        public void SameDay_DifferentTimes_ShareDayNumber()
        {
            Assert.AreEqual(
                GameCalendar.DayNumber(Utc(2026, 3, 10, 1, 0)),
                GameCalendar.DayNumber(Utc(2026, 3, 10, 19, 0)));
        }

        [Test]
        public void LockBefore8pm_TargetsToday_After8pm_TargetsTomorrow()
        {
            var morning = Utc(2026, 3, 10, 9, 0);
            var evening = Utc(2026, 3, 10, 20, 30);

            Assert.AreEqual(GameCalendar.DayNumber(morning), GameCalendar.LockTargetDay(morning));
            Assert.AreEqual(GameCalendar.DayNumber(evening) + 1, GameCalendar.LockTargetDay(evening));
        }

        [Test]
        public void ResolveAt8pm_ProcessesThatDaysLocks()
        {
            var beforeResolve = Utc(2026, 3, 10, 9, 0);
            var resolveMoment = Utc(2026, 3, 10, 20, 0);
            // A lock placed in the morning targets today; the 20:00 resolve processes today.
            Assert.AreEqual(GameCalendar.LockTargetDay(beforeResolve), GameCalendar.ResolveDayFor(resolveMoment));
        }

        [Test]
        public void WeekNumber_GroupsSevenDays()
        {
            Assert.AreEqual(GameCalendar.WeekNumber(0), GameCalendar.WeekNumber(6));
            Assert.AreEqual(GameCalendar.WeekNumber(6) + 1, GameCalendar.WeekNumber(7));
        }
    }
}
