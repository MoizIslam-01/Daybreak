using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class SerializationTests
    {
        [Test]
        public void Squad_RoundTrips_ThroughDto()
        {
            var squad = TestRoster.Squad("moiz", Tactic.Berserk,
                "aegis", "warhound", "houndmaster", "arcanist", "flicker");

            var dto = SquadCodec.ToDto(squad, day: 42, lockedAtUnixMs: 123456789L);
            Assert.AreEqual("moiz", dto.ownerId);
            Assert.AreEqual("Berserk", dto.tactic);
            Assert.AreEqual(42, dto.day);
            Assert.AreEqual(5, dto.units.Length);

            var back = SquadCodec.FromDto(dto);
            Assert.AreEqual(squad.OwnerId, back.OwnerId);
            Assert.AreEqual(squad.Tactic, back.Tactic);
            Assert.AreEqual(squad.Units.Length, back.Units.Length);
            for (int i = 0; i < squad.Units.Length; i++)
            {
                Assert.AreEqual(squad.Units[i].UnitId, back.Units[i].UnitId);
                Assert.AreEqual(squad.Units[i].Row, back.Units[i].Row);
                Assert.AreEqual(squad.Units[i].Col, back.Units[i].Col);
            }
        }

        [Test]
        public void RoundTrippedSquad_StillValidatesAndSimulates()
        {
            var defs = TestRoster.Catalog();
            var squad = TestRoster.Squad("a", Tactic.Balanced,
                "sentinel", "ripper", "pavise", "windrunner", "charger");

            var back = SquadCodec.FromDto(SquadCodec.ToDto(squad, 1, 0));
            Assert.IsTrue(back.IsValid(out var err), err);

            // And it produces the same battle as the original — the DTO preserved everything.
            var opponent = TestRoster.Squad("b", Tactic.FocusFire,
                "aegis", "warhound", "houndmaster", "arcanist", "flicker");
            var r1 = BattleSimulator.Simulate(squad, opponent, WeeklyModifier.None, 99, defs);
            var r2 = BattleSimulator.Simulate(back, opponent, WeeklyModifier.None, 99, defs);
            Assert.AreEqual(r1.WinnerSide, r2.WinnerSide);
            Assert.AreEqual(r1.Log.Length, r2.Log.Length);
        }

        [Test]
        public void DayResult_ConvertsToDto()
        {
            var pdr = new PlayerDayResult { PlayerId = "moiz", Wins = 2, Losses = 1, RemainingHpAcrossWins = 140 };
            pdr.Battles.Add(new BattleRecord { OpponentId = "sam", Won = true, Seed = 111 });
            pdr.Battles.Add(new BattleRecord { OpponentId = "alex", Won = false, Seed = 222 });

            var dto = SquadCodec.ToDto(pdr);
            Assert.AreEqual(2, dto.wins);
            Assert.AreEqual(1, dto.losses);
            Assert.AreEqual(140, dto.remainingHpAcrossWins);
            Assert.AreEqual(2, dto.battles.Length);
            Assert.AreEqual("sam", dto.battles[0].opponentId);
            Assert.IsTrue(dto.battles[0].won);
            Assert.AreEqual(222, dto.battles[1].seed);
        }
    }
}
