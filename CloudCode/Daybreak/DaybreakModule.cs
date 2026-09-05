using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Daybreak.Sim;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;

namespace Daybreak.CloudCode
{
    /// <summary>
    /// The Daybreak server module. It is deliberately thin: all game logic lives in Daybreak.Sim
    /// (the same code the client runs). The module only does IO — read locked squads, call the
    /// shared DailyResolver, write results back.
    ///
    /// Storage layout (Cloud Save):
    ///   Player data  "lockedSquad" : the SquadDto a player locked (their own; day is inside it)
    ///   Player data  "dayResult"   : that player's DayResultDto after a resolve
    ///   Game data    ("daybreak" / "players") : JSON array of all player ids that have ever locked
    /// </summary>
    public class DaybreakModule
    {
        private const string LockedSquadKey = "lockedSquad";
        private const string DayResultKey = "dayResult";
        private const string RosterCustomId = "daybreak";
        private const string RosterKey = "players";

        private readonly ILogger<DaybreakModule> _logger;

        public DaybreakModule(ILogger<DaybreakModule> logger)
        {
            _logger = logger;
        }

        // ---- M0 smoke test, kept ----

        [CloudCodeFunction("SayHello")]
        public HelloWorldResponse SayHello(IExecutionContext context, string name)
        {
            return new HelloWorldResponse
            {
                Message = $"Hello, {name}! Daybreak's server module is alive.",
                PlayerId = context.PlayerId,
                SimUnitCount = UnitConfig.Units.Count
            };
        }

        // ---- M4: lock a squad for the day ----

        /// <summary>
        /// Called by a player to lock their squad. Validates it against the shared rules, stores it
        /// in the player's own Cloud Save, and registers the player in the game-wide roster so the
        /// nightly resolve knows to include them.
        /// </summary>
        [CloudCodeFunction("LockSquad")]
        public async Task<LockResponse> LockSquad(IExecutionContext ctx, IGameApiClient api, string squadJson)
        {
            var dto = Json.From<SquadDto>(squadJson);
            if (dto == null) throw new Exception("Empty or unparseable squad.");

            var squad = SquadCodec.FromDto(dto);
            if (!squad.IsValid(out var error)) throw new Exception("Invalid squad: " + error);

            // Store the player's own locked squad (authenticated as the player).
            await api.CloudSaveData.SetItemAsync(ctx, ctx.AccessToken, ctx.ProjectId, ctx.PlayerId,
                new SetItemBody(LockedSquadKey, squadJson));

            await RegisterInRoster(ctx, api, ctx.PlayerId);

            _logger.LogInformation("Player {PlayerId} locked a squad for day {Day}.", ctx.PlayerId, dto.day);
            return new LockResponse { Ok = true, Day = dto.day };
        }

        // ---- M4: resolve the day (called by the scheduler trigger, or manually for testing) ----

        [CloudCodeFunction("ResolveDay")]
        public async Task<ResolveResponse> ResolveDay(IExecutionContext ctx, IGameApiClient api)
        {
            int day = GameCalendar.ResolveDayFor(DateTime.UtcNow);
            var roster = await ReadRoster(ctx, api);

            var entries = new List<PlayerEntry>();
            foreach (var playerId in roster)
            {
                var dto = await ReadLockedSquad(ctx, api, playerId);
                if (dto == null) continue;
                if (dto.day != day) continue; // only those who locked for today

                try { entries.Add(new PlayerEntry(playerId, SquadCodec.FromDto(dto))); }
                catch (Exception e) { _logger.LogWarning("Skipping {PlayerId}: {Err}", playerId, e.Message); }
            }

            if (entries.Count < 2)
            {
                _logger.LogInformation("ResolveDay {Day}: only {Count} valid entries, nothing to resolve.",
                    day, entries.Count);
                return new ResolveResponse { Day = day, PlayersResolved = entries.Count, BattlesRun = 0 };
            }

            var resolution = DailyResolver.ResolveDay(day, entries, WeeklyModifier.None, UnitConfig.Units);

            int battles = 0;
            foreach (var pr in resolution.Players)
            {
                var resultDto = SquadCodec.ToDto(pr);
                resultDto.day = day;
                battles += pr.Battles.Count;

                // Cross-player write (authenticated as Cloud Code).
                await api.CloudSaveData.SetItemAsync(ctx, ctx.ServiceToken, ctx.ProjectId, pr.PlayerId,
                    new SetItemBody(DayResultKey, Json.To(resultDto)));
            }

            _logger.LogInformation("ResolveDay {Day}: {Players} players, {Battles} battle-records written.",
                day, resolution.Players.Count, battles);
            return new ResolveResponse
            {
                Day = day,
                PlayersResolved = resolution.Players.Count,
                BattlesRun = battles / 2 // each battle recorded from both sides
            };
        }

        // ---- Cloud Save helpers ----

        private async Task RegisterInRoster(IExecutionContext ctx, IGameApiClient api, string playerId)
        {
            var roster = await ReadRoster(ctx, api);
            if (roster.Contains(playerId)) return;

            roster.Add(playerId);
            await api.CloudSaveData.SetCustomItemAsync(ctx, ctx.ServiceToken, ctx.ProjectId, RosterCustomId,
                new SetItemBody(RosterKey, Json.To(roster)));
        }

        private async Task<List<string>> ReadRoster(IExecutionContext ctx, IGameApiClient api)
        {
            try
            {
                var res = await api.CloudSaveData.GetCustomItemsAsync(ctx, ctx.ServiceToken, ctx.ProjectId,
                    RosterCustomId, new List<string> { RosterKey });
                var raw = res.Data.Results.FirstOrDefault(r => r.Key == RosterKey)?.Value?.ToString();
                var list = Json.From<List<string>>(raw);
                return list ?? new List<string>();
            }
            catch (ApiException e)
            {
                _logger.LogWarning("Roster read failed (treating as empty): {Err}", e.Message);
                return new List<string>();
            }
        }

        private async Task<SquadDto> ReadLockedSquad(IExecutionContext ctx, IGameApiClient api, string playerId)
        {
            try
            {
                var res = await api.CloudSaveData.GetItemsAsync(ctx, ctx.ServiceToken, ctx.ProjectId, playerId,
                    new List<string> { LockedSquadKey });
                var raw = res.Data.Results.FirstOrDefault(r => r.Key == LockedSquadKey)?.Value?.ToString();
                return Json.From<SquadDto>(raw);
            }
            catch (ApiException e)
            {
                _logger.LogWarning("Locked-squad read failed for {PlayerId}: {Err}", playerId, e.Message);
                return null;
            }
        }
    }

    public class HelloWorldResponse
    {
        public string Message { get; set; }
        public string PlayerId { get; set; }
        public int SimUnitCount { get; set; }
    }

    public class LockResponse
    {
        public bool Ok { get; set; }
        public int Day { get; set; }
    }

    public class ResolveResponse
    {
        public int Day { get; set; }
        public int PlayersResolved { get; set; }
        public int BattlesRun { get; set; }
    }
}
