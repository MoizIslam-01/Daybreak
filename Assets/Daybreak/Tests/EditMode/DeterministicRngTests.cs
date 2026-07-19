using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class DeterministicRngTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new DeterministicRng(12345);
            var b = new DeterministicRng(12345);

            for (int i = 0; i < 1000; i++)
                Assert.AreEqual(a.NextUInt(), b.NextUInt(), "Diverged at draw " + i);
        }

        [Test]
        public void DifferentSeeds_Diverge()
        {
            var a = new DeterministicRng(1);
            var b = new DeterministicRng(2);

            bool differed = false;
            for (int i = 0; i < 32 && !differed; i++)
                differed = a.NextUInt() != b.NextUInt();

            Assert.IsTrue(differed, "Two different seeds produced identical output.");
        }

        [Test]
        public void ZeroSeed_DoesNotCollapse()
        {
            var rng = new DeterministicRng(0);
            var first = rng.NextUInt();

            Assert.AreNotEqual(0u, first, "Seed 0 must not stick at zero — xorshift has 0 as a fixed point.");
            Assert.AreNotEqual(first, rng.NextUInt());
        }

        [Test]
        public void NextInt_StaysInRange()
        {
            var rng = new DeterministicRng(99);
            for (int i = 0; i < 5000; i++)
            {
                int v = rng.NextInt(7);
                Assert.GreaterOrEqual(v, 0);
                Assert.Less(v, 7);
            }
        }

        [Test]
        public void BattleSeed_IsStableForSameInputs()
        {
            Assert.AreEqual(
                DeterministicRng.BattleSeed(42, "moiz", "sam"),
                DeterministicRng.BattleSeed(42, "moiz", "sam"));
        }

        [Test]
        public void BattleSeed_VariesByDayAndPlayers()
        {
            int baseline = DeterministicRng.BattleSeed(42, "moiz", "sam");

            Assert.AreNotEqual(baseline, DeterministicRng.BattleSeed(43, "moiz", "sam"));
            Assert.AreNotEqual(baseline, DeterministicRng.BattleSeed(42, "moiz", "alex"));
        }
    }
}
