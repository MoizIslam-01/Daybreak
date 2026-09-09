using System.Collections.Generic;
using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    public class StandingsCalculatorTests
    {
        private static PlayerStandingInput P(string id, string name, string team, int wins, int hp) =>
            new PlayerStandingInput { PlayerId = id, Name = name, TeamId = team, Wins = wins, RemainingHp = hp, ColorHex = "#FFFFFF" };

        [Test]
        public void Players_SortByWins_ThenHp_ThenName()
        {
            var players = new List<PlayerStandingInput>
            {
                P("p1", "Alice", "", 2, 50),
                P("p2", "Bob", "", 3, 10),
                P("p3", "Cara", "", 2, 90),
                P("p4", "Dave", "", 2, 90),
            };
            var s = StandingsCalculator.Compute(1, players, null);

            Assert.AreEqual("Bob", s.players[0].name);   // most wins
            Assert.AreEqual("Cara", s.players[1].name);  // 2 wins, hp 90, name < Dave
            Assert.AreEqual("Dave", s.players[2].name);  // 2 wins, hp 90, name after Cara
            Assert.AreEqual("Alice", s.players[3].name); // 2 wins, hp 50 (lowest hp)
            Assert.AreEqual(1, s.players[0].rank);
            Assert.AreEqual(4, s.players[3].rank);
        }

        [Test]
        public void Teams_SumMemberWins_AndSortDesc()
        {
            var players = new List<PlayerStandingInput>
            {
                P("p1", "Alice", "owls", 2, 0),
                P("p2", "Bob", "owls", 3, 0),
                P("p3", "Cara", "foxes", 4, 0),
            };
            var teams = new List<TeamDto>
            {
                new TeamDto { id = "owls", name = "Owls", colorHex = "#111111" },
                new TeamDto { id = "foxes", name = "Foxes", colorHex = "#222222" },
            };
            var s = StandingsCalculator.Compute(1, players, teams);

            Assert.AreEqual(2, s.teams.Length);
            Assert.AreEqual("Owls", s.teams[0].name);   // 5 wins
            Assert.AreEqual(5, s.teams[0].totalWins);
            Assert.AreEqual(2, s.teams[0].memberCount);
            Assert.AreEqual("Foxes", s.teams[1].name);  // 4 wins
            Assert.AreEqual(4, s.teams[1].totalWins);
        }

        [Test]
        public void TeamlessPlayers_AreNotCounted_InAnyTeam()
        {
            var players = new List<PlayerStandingInput> { P("p1", "Solo", "", 9, 0) };
            var teams = new List<TeamDto> { new TeamDto { id = "owls", name = "Owls" } };
            var s = StandingsCalculator.Compute(1, players, teams);

            Assert.AreEqual(1, s.teams.Length);
            Assert.AreEqual(0, s.teams[0].totalWins);
            Assert.AreEqual(0, s.teams[0].memberCount);
        }

        [Test]
        public void EmptyInput_ProducesEmptyStandings()
        {
            var s = StandingsCalculator.Compute(3, new List<PlayerStandingInput>(), new List<TeamDto>());
            Assert.AreEqual(3, s.week);
            Assert.AreEqual(0, s.players.Length);
            Assert.AreEqual(0, s.teams.Length);
        }

        // ---- display-week fallback (weekly-reset gap) ----

        [Test]
        public void DisplayWeek_IsCurrentWeek_WhenItHasAnyRecord()
        {
            Assert.AreEqual(5, StandingsCalculator.ResolveDisplayWeek(5, new List<int> { 4, 5, 4 }));
        }

        [Test]
        public void DisplayWeek_IsCurrentWeek_EvenIfOnlyOnePlayerHasScoredIt()
        {
            Assert.AreEqual(5, StandingsCalculator.ResolveDisplayWeek(5, new List<int> { 3, 3, 5 }));
        }

        [Test]
        public void DisplayWeek_FallsBackToMostRecentWeekWithResults()
        {
            // The reset window: week 5 has begun but no resolve has written a week-5 record yet.
            Assert.AreEqual(4, StandingsCalculator.ResolveDisplayWeek(5, new List<int> { 4, 4, 2 }));
        }

        [Test]
        public void DisplayWeek_FallsBackAcrossAGapOfSkippedWeeks()
        {
            Assert.AreEqual(2, StandingsCalculator.ResolveDisplayWeek(7, new List<int> { 2, 1 }));
        }

        [Test]
        public void DisplayWeek_IsCurrentWeek_WhenThereAreNoRecordsAtAll()
        {
            Assert.AreEqual(3, StandingsCalculator.ResolveDisplayWeek(3, new List<int>()));
            Assert.AreEqual(3, StandingsCalculator.ResolveDisplayWeek(3, null));
        }

        [Test]
        public void DisplayWeek_IgnoresWeeksAheadOfTheClock()
        {
            // Bad/skewed data shouldn't drag the board into a week that hasn't happened.
            Assert.AreEqual(3, StandingsCalculator.ResolveDisplayWeek(4, new List<int> { 9, 3 }));
            Assert.AreEqual(4, StandingsCalculator.ResolveDisplayWeek(4, new List<int> { 9 }));
        }

        [Test]
        public void FallbackWeek_ShowsThatWeeksWins_NotZeroes()
        {
            // What the bug looked like end-to-end: stale week-4 records, clock says week 5.
            var recorded = new List<int> { 4, 4 };
            int week = StandingsCalculator.ResolveDisplayWeek(5, recorded);

            var players = new List<PlayerStandingInput> { P("p1", "Alice", "", 3, 20), P("p2", "Bob", "", 1, 5) };
            var s = StandingsCalculator.Compute(week, players, null);

            Assert.AreEqual(4, s.week);
            Assert.AreEqual("Alice", s.players[0].name);
            Assert.AreEqual(3, s.players[0].wins);
        }
    }
}
