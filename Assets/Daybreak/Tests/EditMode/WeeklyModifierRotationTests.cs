using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class WeeklyModifierRotationTests
    {
        [Test]
        public void ForWeek_CyclesThroughAll()
        {
            int n = WeeklyModifierRotation.Cycle().Length;
            Assert.AreEqual("none", WeeklyModifierRotation.ForWeek(0).Id);
            // A full cycle later returns to the same modifier.
            Assert.AreEqual(WeeklyModifierRotation.ForWeek(0).Id, WeeklyModifierRotation.ForWeek(n).Id);
            Assert.AreEqual(WeeklyModifierRotation.ForWeek(1).Id, WeeklyModifierRotation.ForWeek(n + 1).Id);
        }

        [Test]
        public void ForWeek_IsStableAndCoversDistinctModifiers()
        {
            var a = WeeklyModifierRotation.ForWeek(1).Id;
            var b = WeeklyModifierRotation.ForWeek(2).Id;
            Assert.AreNotEqual(a, b);
            Assert.AreEqual(a, WeeklyModifierRotation.ForWeek(1).Id); // deterministic
        }

        [Test]
        public void ForWeek_HandlesNegativeWeeks()
        {
            Assert.DoesNotThrow(() => WeeklyModifierRotation.ForWeek(-3));
            var m = WeeklyModifierRotation.ForWeek(-1);
            Assert.IsNotNull(m);
        }

        [Test]
        public void ById_RoundTrips_AndUnknownFallsBackToNone()
        {
            Assert.AreEqual("entrenched", WeeklyModifierRotation.ById("entrenched").Id);
            Assert.AreEqual("glass_cannons", WeeklyModifierRotation.ById("glass_cannons").Id);
            Assert.AreEqual("none", WeeklyModifierRotation.ById("nonsense").Id);
            Assert.AreEqual("none", WeeklyModifierRotation.ById(null).Id);
        }
    }
}
