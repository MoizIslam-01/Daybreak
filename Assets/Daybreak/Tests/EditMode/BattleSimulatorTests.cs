using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class BattleSimulatorTests
    {
        private UnitCatalog _defs;

        [SetUp]
        public void Setup() => _defs = TestRoster.Catalog();

        private Squad SquadA(Tactic t = Tactic.Balanced) =>
            TestRoster.Squad("A", t, "aegis", "warhound", "houndmaster", "arcanist", "flicker");

        private Squad SquadB(Tactic t = Tactic.Balanced) =>
            TestRoster.Squad("B", t, "sentinel", "ripper", "pavise", "windrunner", "charger");

        // ---- The Milestone 1 acceptance criterion. ----

        [Test]
        public void SameInputsAndSeed_ProduceByteIdenticalLog()
        {
            for (int seed = 1; seed <= 25; seed++)
            {
                var r1 = BattleSimulator.Simulate(SquadA(), SquadB(), WeeklyModifier.None, seed, _defs);
                var r2 = BattleSimulator.Simulate(SquadA(), SquadB(), WeeklyModifier.None, seed, _defs);

                Assert.AreEqual(r1.WinnerSide, r2.WinnerSide, "Winner differed at seed " + seed);
                Assert.AreEqual(r1.Log.Length, r2.Log.Length, "Log length differed at seed " + seed);
                for (int i = 0; i < r1.Log.Length; i++)
                    Assert.IsTrue(r1.Log[i].Equals(r2.Log[i]),
                        "Event " + i + " differed at seed " + seed);
            }
        }

        [Test]
        public void Determinism_HoldsAcrossTacticsAndModifiers()
        {
            var tactics = new[] { Tactic.Balanced, Tactic.FocusFire, Tactic.ProtectBackline, Tactic.Berserk };
            var mods = new[]
            {
                WeeklyModifier.None, WeeklyModifier.Entrenched, WeeklyModifier.GlassCannons,
                WeeklyModifier.VanguardsHour, WeeklyModifier.ArcaneSurge
            };

            foreach (var tac in tactics)
            foreach (var mod in mods)
            {
                var r1 = BattleSimulator.Simulate(SquadA(tac), SquadB(), mod, 777, _defs);
                var r2 = BattleSimulator.Simulate(SquadA(tac), SquadB(), mod, 777, _defs);
                Assert.AreEqual(r1.Log.Length, r2.Log.Length, "len " + tac + "/" + mod.Id);
                for (int i = 0; i < r1.Log.Length; i++)
                    Assert.IsTrue(r1.Log[i].Equals(r2.Log[i]), "evt " + i + " " + tac + "/" + mod.Id);
            }
        }

        // ---- Battle integrity invariants. ----

        [Test]
        public void Battle_AlwaysProducesAWinner_NeverADraw()
        {
            for (int seed = 1; seed <= 25; seed++)
            {
                var r = BattleSimulator.Simulate(SquadA(), SquadB(), WeeklyModifier.None, seed, _defs);
                Assert.IsTrue(r.WinnerSide == 0 || r.WinnerSide == 1, "seed " + seed);
            }
        }

        [Test]
        public void Log_StartsWithBattleStart_EndsWithBattleEnd()
        {
            var r = BattleSimulator.Simulate(SquadA(), SquadB(), WeeklyModifier.None, 5, _defs);
            Assert.AreEqual(EventType.BattleStart, r.Log[0].Type);
            Assert.AreEqual(EventType.BattleEnd, r.Log[r.Log.Length - 1].Type);
            Assert.AreEqual(r.WinnerSide, r.Log[r.Log.Length - 1].Value);
        }

        [Test]
        public void EveryDamageEvent_IsAtLeastOne_AndTicksAreMonotonic()
        {
            var r = BattleSimulator.Simulate(SquadA(), SquadB(), WeeklyModifier.None, 9, _defs);
            int prevTick = -1;
            foreach (var e in r.Log)
            {
                Assert.Greater(e.Tick, prevTick, "Ticks must strictly increase.");
                prevTick = e.Tick;
                if (e.Type == EventType.Damage)
                    Assert.GreaterOrEqual(e.Value, 1, "Damage must be at least 1.");
            }
        }

        [Test]
        public void AllEventIndices_AreWithinRange()
        {
            var r = BattleSimulator.Simulate(SquadA(), SquadB(), WeeklyModifier.None, 3, _defs);
            foreach (var e in r.Log)
            {
                if (e.Source >= 0) Assert.Less(e.Source, 10);
                if (e.Target >= 0) Assert.Less(e.Target, 10);
            }
        }

        [Test]
        public void RoundCount_NeverExceedsCap()
        {
            for (int seed = 1; seed <= 25; seed++)
            {
                var r = BattleSimulator.Simulate(SquadA(), SquadB(), WeeklyModifier.None, seed, _defs);
                int rounds = 0;
                foreach (var e in r.Log) if (e.Type == EventType.RoundStart) rounds++;
                Assert.LessOrEqual(rounds, BattleSimulator.MaxRounds, "seed " + seed);
            }
        }

        [Test]
        public void MeleeAttacker_HitsFrontRowFirst()
        {
            // Give side A a strong Swift synergy so flicker (SPD 11, melee) is the fastest overall
            // and therefore the first to act; its target must be an enemy front-row unit.
            var a = TestRoster.Squad("A", Tactic.Balanced,
                "flicker", "charger", "windrunner", "aegis", "warhound"); // 3x Swift
            var b = SquadB();
            var r = BattleSimulator.Simulate(a, b, WeeklyModifier.None, 1, _defs);

            // First Attack event in the log.
            BattleEvent firstAttack = default;
            bool found = false;
            foreach (var e in r.Log)
                if (e.Type == EventType.Attack) { firstAttack = e; found = true; break; }
            Assert.IsTrue(found, "Expected at least one attack.");

            // Side B front row occupies instance indices 5, 6, 7 (grid slots (0,0)(0,1)(0,2)).
            // flicker is a Skirmisher (melee), so it must strike the enemy front row.
            Assert.AreEqual(0, firstAttack.Source, "flicker should act first (index 0).");
            int t = firstAttack.Target;
            Assert.IsTrue(t == 5 || t == 6 || t == 7,
                "A melee unit must target the enemy front row (indices 5-7) while it lives; got " + t);
        }

        [Test]
        public void InvalidSquad_Throws()
        {
            var good = SquadA();
            var bad = new Squad
            {
                OwnerId = "bad",
                Tactic = Tactic.Balanced,
                Units = new[] { new Placement("aegis", 0, 0) } // only 1 unit
            };
            Assert.Throws<System.ArgumentException>(
                () => BattleSimulator.Simulate(good, bad, WeeklyModifier.None, 1, _defs));
        }
    }
}
