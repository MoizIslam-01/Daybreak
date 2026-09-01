using System;
using System.Collections.Generic;

namespace Daybreak.Sim
{
    /// <summary>One player's locked entry for a day: who they are and the squad they submitted.</summary>
    public sealed class PlayerEntry
    {
        public string PlayerId;
        public Squad Squad;

        public PlayerEntry() { }
        public PlayerEntry(string playerId, Squad squad) { PlayerId = playerId; Squad = squad; }
    }

    /// <summary>
    /// One battle from a single player's perspective. The replay is just (opponent, seed): the
    /// client regenerates the full event log by re-running the sim, so this stays a few bytes.
    /// </summary>
    public struct BattleRecord
    {
        public string OpponentId;
        public bool Won;
        public int Seed;
    }

    /// <summary>A player's whole day: record, tiebreak, and the per-battle replay pointers.</summary>
    public sealed class PlayerDayResult
    {
        public string PlayerId;
        public int Wins;
        public int Losses;
        public int RemainingHpAcrossWins; // weekly leaderboard tiebreak
        public List<BattleRecord> Battles = new List<BattleRecord>();
    }

    public sealed class DayResolution
    {
        public int Day;
        public List<PlayerDayResult> Players = new List<PlayerDayResult>();
    }

    /// <summary>
    /// The nightly round-robin, as a pure function. Every active player's locked squad fights every
    /// other player's once; ghost battles, so nobody has to be online. Deterministic end to end:
    /// the per-battle seed is a pure function of (day, the two ids), so the server and any client
    /// re-deriving a replay produce the identical fight.
    ///
    /// This lives in Daybreak.Sim so the Cloud Code module and the unit tests call the exact same
    /// resolution logic — the module only adds IO (load squads, store results).
    /// </summary>
    public static class DailyResolver
    {
        public static DayResolution ResolveDay(int day, IReadOnlyList<PlayerEntry> players,
            WeeklyModifier mod, UnitCatalog defs)
        {
            if (players == null) throw new ArgumentNullException(nameof(players));
            if (defs == null) throw new ArgumentNullException(nameof(defs));
            if (mod == null) mod = WeeklyModifier.None;

            // Stable pairing order: sort by id so side-A assignment (and thus the seed) is fixed.
            var sorted = new List<PlayerEntry>(players);
            sorted.Sort((x, y) => string.CompareOrdinal(x.PlayerId, y.PlayerId));

            var byId = new Dictionary<string, PlayerDayResult>(StringComparer.Ordinal);
            foreach (var p in sorted)
                if (!byId.ContainsKey(p.PlayerId))
                    byId[p.PlayerId] = new PlayerDayResult { PlayerId = p.PlayerId };

            for (int i = 0; i < sorted.Count; i++)
            {
                for (int j = i + 1; j < sorted.Count; j++)
                {
                    var a = sorted[i]; // smaller id  -> side A
                    var b = sorted[j]; // larger id   -> side B

                    int seed = DeterministicRng.BattleSeed(day, a.PlayerId, b.PlayerId);
                    var result = BattleSimulator.Simulate(a.Squad, b.Squad, mod, seed, defs);

                    bool aWon = result.WinnerSide == BattleResult.SideA;
                    Record(byId[a.PlayerId], b.PlayerId, aWon, seed, aWon ? result.WinnerRemainingHp : 0);
                    Record(byId[b.PlayerId], a.PlayerId, !aWon, seed, !aWon ? result.WinnerRemainingHp : 0);
                }
            }

            // Output in the caller's original order for friendliness; per-player battle lists are
            // already in deterministic (sorted-opponent) order from the loop above.
            var resolution = new DayResolution { Day = day };
            foreach (var p in players)
                resolution.Players.Add(byId[p.PlayerId]);
            return resolution;
        }

        private static void Record(PlayerDayResult r, string opponentId, bool won, int seed, int remainingHp)
        {
            r.Battles.Add(new BattleRecord { OpponentId = opponentId, Won = won, Seed = seed });
            if (won) { r.Wins++; r.RemainingHpAcrossWins += remainingHp; }
            else r.Losses++;
        }
    }
}
