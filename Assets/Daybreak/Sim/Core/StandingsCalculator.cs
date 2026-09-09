using System.Collections.Generic;

namespace Daybreak.Sim
{
    /// <summary>One player's weekly totals fed into the standings (gathered from Cloud Save server-side).</summary>
    public struct PlayerStandingInput
    {
        public string PlayerId;
        public string Name;
        public string ColorHex;
        public string TeamId;
        public string Title;    // equipped cosmetic title id
        public int Wins;
        public int RemainingHp; // tiebreak
    }

    /// <summary>
    /// Computes the weekly individual and team standings, purely and deterministically. We compute
    /// standings from our own data rather than the UGS Leaderboards service — for a friend group
    /// that gives full control over names, colors, and the team roll-up in one place, and it's
    /// trivially testable.
    ///
    /// Individual order: wins desc, then remaining HP desc, then name (ordinal), then id.
    /// Team order: total wins desc, then name.
    /// </summary>
    public static class StandingsCalculator
    {
        /// <summary>
        /// Picks which week the board should actually display.
        ///
        /// Weekly records are stored one per player, keyed by week, and are only (re)written by the
        /// nightly resolve. So between a week rolling over at midnight UTC and that night's 20:00
        /// UTC resolve, no record matches the current week — computing standings against it would
        /// show an all-zero board for up to 20 hours even though last week's results are sitting
        /// right there. When nothing has been recorded for the current week yet, fall back to the
        /// most recent earlier week that does have results.
        /// </summary>
        /// <param name="currentWeek">The week the clock says we are in.</param>
        /// <param name="recordedWeeks">The week number on each player's stored weekly record.</param>
        /// <returns>
        /// <paramref name="currentWeek"/> as soon as any record belongs to it (a zero there is a
        /// real score, not a gap), otherwise the highest recorded week below it — and
        /// <paramref name="currentWeek"/> again when there is nothing older to fall back to.
        /// </returns>
        public static int ResolveDisplayWeek(int currentWeek, IReadOnlyList<int> recordedWeeks)
        {
            if (recordedWeeks == null) return currentWeek;

            int best = int.MinValue;
            for (int i = 0; i < recordedWeeks.Count; i++)
            {
                int w = recordedWeeks[i];
                if (w == currentWeek) return currentWeek;   // the new week has already scored
                if (w < currentWeek && w > best) best = w;  // ignore anything ahead of the clock
            }
            return best == int.MinValue ? currentWeek : best;
        }

        public static StandingsDto Compute(int week, IReadOnlyList<PlayerStandingInput> players,
            IReadOnlyList<TeamDto> teams)
        {
            var playerList = new List<PlayerStandingInput>(players ?? new PlayerStandingInput[0]);
            playerList.Sort(ComparePlayers);

            var playerDtos = new StandingDto[playerList.Count];
            for (int i = 0; i < playerList.Count; i++)
            {
                var p = playerList[i];
                playerDtos[i] = new StandingDto
                {
                    rank = i + 1,
                    playerId = p.PlayerId,
                    name = p.Name,
                    colorHex = p.ColorHex,
                    teamId = p.TeamId,
                    title = p.Title,
                    wins = p.Wins,
                    remainingHp = p.RemainingHp
                };
            }

            // Team roll-up: sum member wins keyed by teamId.
            var winsByTeam = new Dictionary<string, int>();
            var membersByTeam = new Dictionary<string, int>();
            foreach (var p in playerList)
            {
                if (string.IsNullOrEmpty(p.TeamId)) continue;
                winsByTeam.TryGetValue(p.TeamId, out var w);
                winsByTeam[p.TeamId] = w + p.Wins;
                membersByTeam.TryGetValue(p.TeamId, out var m);
                membersByTeam[p.TeamId] = m + 1;
            }

            var teamList = new List<TeamStandingDto>();
            if (teams != null)
            {
                foreach (var t in teams)
                {
                    if (t == null || string.IsNullOrEmpty(t.id)) continue;
                    winsByTeam.TryGetValue(t.id, out var w);
                    membersByTeam.TryGetValue(t.id, out var m);
                    teamList.Add(new TeamStandingDto
                    {
                        teamId = t.id,
                        name = t.name,
                        colorHex = t.colorHex,
                        totalWins = w,
                        memberCount = m
                    });
                }
            }
            teamList.Sort(CompareTeams);
            for (int i = 0; i < teamList.Count; i++) teamList[i].rank = i + 1;

            return new StandingsDto { week = week, players = playerDtos, teams = teamList.ToArray() };
        }

        private static int ComparePlayers(PlayerStandingInput a, PlayerStandingInput b)
        {
            if (a.Wins != b.Wins) return b.Wins - a.Wins;
            if (a.RemainingHp != b.RemainingHp) return b.RemainingHp - a.RemainingHp;
            int byName = string.CompareOrdinal(a.Name ?? "", b.Name ?? "");
            if (byName != 0) return byName;
            return string.CompareOrdinal(a.PlayerId ?? "", b.PlayerId ?? "");
        }

        private static int CompareTeams(TeamStandingDto a, TeamStandingDto b)
        {
            if (a.totalWins != b.totalWins) return b.totalWins - a.totalWins;
            return string.CompareOrdinal(a.name ?? "", b.name ?? "");
        }
    }
}
