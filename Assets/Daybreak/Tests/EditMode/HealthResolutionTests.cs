using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class HealthResolutionTests
    {
        [Test]
        public void HigherPercentage_Wins_EvenWithLessAbsoluteHp()
        {
            var rng = new DeterministicRng(1);
            // A: 30/100 = 30%. B: 40/200 = 20%. A wins despite lower absolute HP.
            int winner = BattleSimulator.ResolveByHealthPercent(30, 100, 40, 200, ref rng);
            Assert.AreEqual(BattleResult.SideA, winner);
        }

        [Test]
        public void SideBHigherPercentage_Wins()
        {
            var rng = new DeterministicRng(1);
            int winner = BattleSimulator.ResolveByHealthPercent(10, 100, 60, 200, ref rng);
            Assert.AreEqual(BattleResult.SideB, winner);
        }

        [Test]
        public void ExactTie_IsBrokenDeterministicallyBySeed()
        {
            var rng1 = new DeterministicRng(12345);
            var rng2 = new DeterministicRng(12345);
            int w1 = BattleSimulator.ResolveByHealthPercent(50, 100, 100, 200, ref rng1);
            int w2 = BattleSimulator.ResolveByHealthPercent(50, 100, 100, 200, ref rng2);
            Assert.AreEqual(w1, w2, "Same seed must break the tie the same way.");
        }

        [Test]
        public void ExactTie_NeverReturnsDraw()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                var rng = new DeterministicRng(seed);
                int w = BattleSimulator.ResolveByHealthPercent(50, 100, 50, 100, ref rng);
                Assert.IsTrue(w == BattleResult.SideA || w == BattleResult.SideB);
            }
        }
    }
}
