using System.Collections.Generic;
using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class DailyResolverTests
    {
        private UnitCatalog _defs;

        [SetUp]
        public void Setup() => _defs = TestRoster.Catalog();

        private List<PlayerEntry> FourPlayers()
        {
            return new List<PlayerEntry>
            {
                new PlayerEntry("moiz", TestRoster.Squad("moiz", Tactic.Balanced,
                    "aegis", "warhound", "houndmaster", "arcanist", "flicker")),
                new PlayerEntry("sam", TestRoster.Squad("sam", Tactic.FocusFire,
                    "sentinel", "ripper", "pavise", "windrunner", "charger")),
                new PlayerEntry("alex", TestRoster.Squad("alex", Tactic.Berserk,
                    "runeshield", "hexblade", "arcanist", "sentinel", "charger")),
                new PlayerEntry("jo", TestRoster.Squad("jo", Tactic.ProtectBackline,
                    "aegis", "pavise", "windrunner", "ripper", "flicker")),
            };
        }

        [Test]
        public void RoundRobin_EachPlayerFightsEveryoneElseOnce()
        {
            var players = FourPlayers();
            var res = DailyResolver.ResolveDay(1, players, WeeklyModifier.None, _defs);

            Assert.AreEqual(4, res.Players.Count);
            foreach (var p in res.Players)
            {
                Assert.AreEqual(3, p.Battles.Count, p.PlayerId + " should have 3 battles");
                Assert.AreEqual(3, p.Wins + p.Losses, p.PlayerId + " wins+losses should be 3");
            }
        }

        [Test]
        public void TotalWins_EqualsTotalLosses_And_BattleCount()
        {
            var players = FourPlayers();
            var res = DailyResolver.ResolveDay(1, players, WeeklyModifier.None, _defs);

            int wins = 0, losses = 0;
            foreach (var p in res.Players) { wins += p.Wins; losses += p.Losses; }

            Assert.AreEqual(wins, losses, "Every battle has one winner and one loser.");
            Assert.AreEqual(4 * 3 / 2, wins, "N*(N-1)/2 battles total.");
        }

        [Test]
        public void EachPairwiseResult_IsConsistentBetweenTheTwoPlayers()
        {
            var players = FourPlayers();
            var res = DailyResolver.ResolveDay(1, players, WeeklyModifier.None, _defs);

            var byId = new Dictionary<string, PlayerDayResult>();
            foreach (var p in res.Players) byId[p.PlayerId] = p;

            foreach (var p in res.Players)
            foreach (var battle in p.Battles)
            {
                var opp = byId[battle.OpponentId];
                var mirror = opp.Battles.Find(x => x.OpponentId == p.PlayerId);
                Assert.AreEqual(battle.Won, !mirror.Won,
                    p.PlayerId + " vs " + battle.OpponentId + ": exactly one should win");
                Assert.AreEqual(battle.Seed, mirror.Seed, "Same battle, same seed from both sides.");
            }
        }

        [Test]
        public void Resolution_IsFullyDeterministic()
        {
            var r1 = DailyResolver.ResolveDay(7, FourPlayers(), WeeklyModifier.None, _defs);
            var r2 = DailyResolver.ResolveDay(7, FourPlayers(), WeeklyModifier.None, _defs);

            Assert.AreEqual(r1.Players.Count, r2.Players.Count);
            for (int i = 0; i < r1.Players.Count; i++)
            {
                var a = r1.Players[i];
                var b = r2.Players[i];
                Assert.AreEqual(a.PlayerId, b.PlayerId);
                Assert.AreEqual(a.Wins, b.Wins);
                Assert.AreEqual(a.RemainingHpAcrossWins, b.RemainingHpAcrossWins);
                Assert.AreEqual(a.Battles.Count, b.Battles.Count);
                for (int j = 0; j < a.Battles.Count; j++)
                {
                    Assert.AreEqual(a.Battles[j].OpponentId, b.Battles[j].OpponentId);
                    Assert.AreEqual(a.Battles[j].Seed, b.Battles[j].Seed);
                    Assert.AreEqual(a.Battles[j].Won, b.Battles[j].Won);
                }
            }
        }

        [Test]
        public void StoredSeed_RegeneratesTheSameOutcome()
        {
            // This is what a replay does: from (opponent, seed) alone, re-run the sim and get the
            // identical winner — no need to store the event log.
            var players = FourPlayers();
            int day = 3;
            var res = DailyResolver.ResolveDay(day, players, WeeklyModifier.None, _defs);

            var byId = new Dictionary<string, Squad>();
            foreach (var p in players) byId[p.PlayerId] = p.Squad;

            var moiz = res.Players.Find(p => p.PlayerId == "moiz");
            foreach (var battle in moiz.Battles)
            {
                // Side A is always the lexicographically-smaller id.
                bool moizIsA = string.CompareOrdinal("moiz", battle.OpponentId) < 0;
                var a = moizIsA ? byId["moiz"] : byId[battle.OpponentId];
                var b = moizIsA ? byId[battle.OpponentId] : byId["moiz"];

                var replay = BattleSimulator.Simulate(a, b, WeeklyModifier.None, battle.Seed, _defs);
                bool moizWon = moizIsA ? replay.WonByA : !replay.WonByA;
                Assert.AreEqual(battle.Won, moizWon, "Replay from seed must match recorded result.");
            }
        }

        [Test]
        public void EmptyOrSingle_ProducesNoBattles()
        {
            var none = DailyResolver.ResolveDay(1, new List<PlayerEntry>(), WeeklyModifier.None, _defs);
            Assert.AreEqual(0, none.Players.Count);

            var one = DailyResolver.ResolveDay(1, new List<PlayerEntry>
            {
                new PlayerEntry("solo", TestRoster.Squad("solo", Tactic.Balanced,
                    "aegis", "warhound", "houndmaster", "arcanist", "flicker"))
            }, WeeklyModifier.None, _defs);
            Assert.AreEqual(1, one.Players.Count);
            Assert.AreEqual(0, one.Players[0].Battles.Count);
        }
    }
}
