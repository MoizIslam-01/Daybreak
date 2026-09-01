using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    /// <summary>
    /// Not an assertion suite — a readable harness. Run these explicitly (right-click -> Run) to
    /// print a battle blow-by-blow into the Test Runner output and eyeball whether it feels
    /// sensible. This is the "battles feel sensible on paper" half of the M1 acceptance bar.
    /// </summary>
    public class BattleHarnessTests
    {
        private UnitCatalog _defs;

        [SetUp]
        public void Setup() => _defs = TestRoster.Catalog();

        [Test, Explicit("Prints a battle; run manually to read it.")]
        public void PrintBalancedMirrorBattle()
        {
            var a = TestRoster.Squad("Alice", Tactic.Balanced,
                "aegis", "warhound", "houndmaster", "arcanist", "flicker");
            var b = TestRoster.Squad("Bob", Tactic.FocusFire,
                "sentinel", "ripper", "pavise", "windrunner", "charger");

            var result = BattleSimulator.Simulate(a, b, WeeklyModifier.None, 42, _defs);
            TestContext.WriteLine(BattleLogFormatter.Format(a, b, WeeklyModifier.None, result, _defs));
        }

        [Test, Explicit("Prints a battle under each weekly modifier.")]
        public void PrintBattlesUnderEachModifier()
        {
            var a = TestRoster.Squad("Alice", Tactic.Balanced,
                "aegis", "warhound", "houndmaster", "arcanist", "flicker");
            var b = TestRoster.Squad("Bob", Tactic.Berserk,
                "runeshield", "hexblade", "arcanist", "sentinel", "charger");

            var mods = new[]
            {
                WeeklyModifier.None, WeeklyModifier.Entrenched, WeeklyModifier.GlassCannons,
                WeeklyModifier.VanguardsHour, WeeklyModifier.ArcaneSurge
            };
            foreach (var mod in mods)
            {
                var result = BattleSimulator.Simulate(a, b, mod, 42, _defs);
                TestContext.WriteLine(BattleLogFormatter.Format(a, b, mod, result, _defs));
                TestContext.WriteLine("");
            }
        }
    }
}
