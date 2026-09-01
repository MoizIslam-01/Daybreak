using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class SynergyRulesTests
    {
        private UnitCatalog _defs;

        [SetUp]
        public void Setup() => _defs = TestRoster.Catalog();

        [Test]
        public void TwoGuardians_Give15PercentDefense()
        {
            // aegis + sentinel are both Guardian; other three are non-Guardian.
            var squad = TestRoster.Squad("t", Tactic.Balanced,
                "aegis", "sentinel", "ripper", "flicker", "windrunner");
            var buffs = SynergyRules.Compute(squad, _defs, false);
            Assert.AreEqual(1150, buffs.DefPermille);
        }

        [Test]
        public void ThreeGuardians_Give30PercentDefense()
        {
            var squad = TestRoster.Squad("t", Tactic.Balanced,
                "aegis", "sentinel", "pavise", "ripper", "flicker");
            var buffs = SynergyRules.Compute(squad, _defs, false);
            Assert.AreEqual(1300, buffs.DefPermille);
        }

        [Test]
        public void TwoPack_Give12PercentAttack_ThreeGive25()
        {
            var two = TestRoster.Squad("t", Tactic.Balanced,
                "warhound", "ripper", "aegis", "flicker", "windrunner");
            Assert.AreEqual(1120, SynergyRules.Compute(two, _defs, false).AtkPermille);

            var three = TestRoster.Squad("t", Tactic.Balanced,
                "warhound", "ripper", "houndmaster", "aegis", "flicker");
            Assert.AreEqual(1250, SynergyRules.Compute(three, _defs, false).AtkPermille);
        }

        [Test]
        public void TwoSwift_Give15PercentSpeed_ThreeGive30()
        {
            var two = TestRoster.Squad("t", Tactic.Balanced,
                "charger", "flicker", "aegis", "ripper", "pavise");
            Assert.AreEqual(1150, SynergyRules.Compute(two, _defs, false).SpdPermille);

            var three = TestRoster.Squad("t", Tactic.Balanced,
                "charger", "flicker", "windrunner", "aegis", "ripper");
            Assert.AreEqual(1300, SynergyRules.Compute(three, _defs, false).SpdPermille);
        }

        [Test]
        public void ArcaneTrueDamage_ScalesWithCopies()
        {
            var two = TestRoster.Squad("t", Tactic.Balanced,
                "runeshield", "hexblade", "aegis", "ripper", "flicker");
            Assert.AreEqual(4, SynergyRules.Compute(two, _defs, false).ArcaneTrueDamage);

            var three = TestRoster.Squad("t", Tactic.Balanced,
                "runeshield", "hexblade", "arcanist", "aegis", "ripper");
            Assert.AreEqual(9, SynergyRules.Compute(three, _defs, false).ArcaneTrueDamage);
        }

        [Test]
        public void ArcaneSurge_DoublesTrueDamage()
        {
            var two = TestRoster.Squad("t", Tactic.Balanced,
                "runeshield", "hexblade", "aegis", "ripper", "flicker");
            Assert.AreEqual(8, SynergyRules.Compute(two, _defs, true).ArcaneTrueDamage);
        }

        [Test]
        public void SingleCopy_GivesNoBuff()
        {
            // One of each tag except a spare — pick so no tag reaches 2 is impossible with 5 units,
            // so this checks a tag with exactly 1 copy stays neutral while another triggers.
            var squad = TestRoster.Squad("t", Tactic.Balanced,
                "aegis", "warhound", "runeshield", "charger", "flicker"); // 4 Vanguards (tags G/P/A/Sw) + Swift skirm
            var buffs = SynergyRules.Compute(squad, _defs, false);
            // Guardian x1, Pack x1, Arcane x1, Swift x2 (charger + flicker).
            Assert.AreEqual(1000, buffs.DefPermille);
            Assert.AreEqual(1000, buffs.AtkPermille);
            Assert.AreEqual(0, buffs.ArcaneTrueDamage);
            Assert.AreEqual(1150, buffs.SpdPermille);
        }
    }
}
