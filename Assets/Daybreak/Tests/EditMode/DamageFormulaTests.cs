using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class DamageFormulaTests
    {
        [Test]
        public void Neutral_SubtractsDefense()
        {
            // 20 ATK, neutral (1000), 5 DEF -> 15.
            Assert.AreEqual(15, BattleSimulator.PhysicalDamage(20, 1000, 5));
        }

        [Test]
        public void Favorable_AppliesMultiplierBeforeDefense()
        {
            // 20*1250/1000 = 25, minus 5 DEF = 20.
            Assert.AreEqual(20, BattleSimulator.PhysicalDamage(20, 1250, 5));
        }

        [Test]
        public void Unfavorable_AppliesReduction()
        {
            // 20*800/1000 = 16, minus 5 = 11.
            Assert.AreEqual(11, BattleSimulator.PhysicalDamage(20, 800, 5));
        }

        [Test]
        public void IntegerDivision_Truncates()
        {
            // 14*1250/1000 = 17 (17.5 truncated), minus 8 = 9.
            Assert.AreEqual(9, BattleSimulator.PhysicalDamage(14, 1250, 8));
        }

        [Test]
        public void HighDefense_ClampsToMinimumOne()
        {
            // 13*800/1000 = 10, minus 100 DEF would be negative -> clamps to 1.
            Assert.AreEqual(1, BattleSimulator.PhysicalDamage(13, 800, 100));
        }
    }
}
