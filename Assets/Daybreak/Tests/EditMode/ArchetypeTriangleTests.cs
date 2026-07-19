using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class ArchetypeTriangleTests
    {
        [TestCase(Archetype.Vanguard, Archetype.Skirmisher)]
        [TestCase(Archetype.Skirmisher, Archetype.Marksman)]
        [TestCase(Archetype.Marksman, Archetype.Vanguard)]
        public void FavorableMatchups_Get125Percent(Archetype attacker, Archetype defender)
        {
            Assert.AreEqual(1250, BattleSimulator.ArchetypeMultiplierPermille(attacker, defender));
        }

        [TestCase(Archetype.Skirmisher, Archetype.Vanguard)]
        [TestCase(Archetype.Marksman, Archetype.Skirmisher)]
        [TestCase(Archetype.Vanguard, Archetype.Marksman)]
        public void UnfavorableMatchups_Get80Percent(Archetype attacker, Archetype defender)
        {
            Assert.AreEqual(800, BattleSimulator.ArchetypeMultiplierPermille(attacker, defender));
        }

        [TestCase(Archetype.Vanguard)]
        [TestCase(Archetype.Skirmisher)]
        [TestCase(Archetype.Marksman)]
        public void MirrorMatchups_AreNeutral(Archetype arch)
        {
            Assert.AreEqual(1000, BattleSimulator.ArchetypeMultiplierPermille(arch, arch));
        }
    }
}
