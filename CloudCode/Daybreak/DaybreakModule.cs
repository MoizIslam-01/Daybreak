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
        private const string ProfileKey = "profile";
        private const string RosterCustomId = "daybreak";
        private const string RosterKey = "players";
        private const string WeeklyKey = "weekly";
        private const string WalletKey = "wallet";
        private const string ChampionsKey = "champions";
        private const string LeaderboardId = "weekly_wins";
        private const int SparksPerLock = 10;
        private const int SparksPerWin = 2;

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
            await AwardLockSparks(ctx, api, ctx.PlayerId, dto.day);

            _logger.LogInformation("Player {PlayerId} locked a squad for day {Day}.", ctx.PlayerId, dto.day);
            return new LockResponse { Ok = true, Day = dto.day };
        }

        // ---- M4: resolve the day (called by the scheduler trigger, or manually for testing) ----

        [CloudCodeFunction("ResolveDay")]
        public async Task<ResolveResponse> ResolveDay(IExecutionContext ctx, IGameApiClient api)
        {
            int day = GameCalendar.ResolveDayFor(DateTime.UtcNow);
            var roster = await ReadRoster(ctx, api);

            // Before touching this week's records, close out last week if it hasn't been crowned.
            // On the first resolve of a new week the weekly records still hold last week's totals
            // (UpdateWeekly hasn't reset them yet), so we can finalize accurately here.
            await FinalizeWeekIfNeeded(ctx, api, GameCalendar.WeekNumber(day) - 1, roster);

            var entries = new List<PlayerEntry>();
            var squadMap = new Dictionary<string, SquadDto>();   // to embed squads in replays
            var profileMap = new Dictionary<string, ProfileDto>(); // to embed names/colors
            foreach (var playerId in roster)
            {
                var dto = await ReadLockedSquad(ctx, api, playerId);
                if (dto == null) continue;
                // Auto-repeat: a player who didn't lock today still fights with their LAST locked
                // squad (design pillar — the flakiest friend keeps participating). So we include
                // every roster player who has ever locked, regardless of the squad's day.

                try
                {
                    entries.Add(new PlayerEntry(playerId, SquadCodec.FromDto(dto)));
                    squadMap[playerId] = dto;
                    profileMap[playerId] = Json.From<ProfileDto>(await ReadPlayerItem(ctx, api, playerId, ProfileKey));
                }
                catch (Exception e) { _logger.LogWarning("Skipping {PlayerId}: {Err}", playerId, e.Message); }
            }

            if (entries.Count < 2)
            {
                _logger.LogInformation("ResolveDay {Day}: only {Count} valid entries, nothing to resolve.",
                    day, entries.Count);
                return new ResolveResponse { Day = day, PlayersResolved = entries.Count, BattlesRun = 0 };
            }

            var modifier = WeeklyModifierRotation.ForWeek(GameCalendar.WeekNumber(day));
            var resolution = DailyResolver.ResolveDay(day, entries, modifier, UnitConfig.Units);

            int battles = 0;
            foreach (var pr in resolution.Players)
            {
                var resultDto = BuildDayResult(pr, day, squadMap, profileMap, modifier.Id);
                battles += pr.Battles.Count;

                // Cross-player write (authenticated as Cloud Code).
                await api.CloudSaveData.SetItemAsync(ctx, ctx.ServiceToken, ctx.ProjectId, pr.PlayerId,
                    new SetItemBody(DayResultKey, Json.To(resultDto)));

                await UpdateWeekly(ctx, api, pr.PlayerId, day, pr.Wins, pr.RemainingHpAcrossWins);
                await AwardWinSparks(ctx, api, pr.PlayerId, day, pr.Wins);
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

        /// <summary>
        /// Builds the stored day result, embedding each opponent's squad snapshot and the player's
        /// own squad so the client can regenerate every battle offline from (squads, seed) — the
        /// replay format from the design (§8.3). Modifier is "none" until M6 rotates modifiers.
        /// </summary>
        private static DayResultDto BuildDayResult(PlayerDayResult pr, int day,
            Dictionary<string, SquadDto> squadMap, Dictionary<string, ProfileDto> profileMap, string modifierId)
        {
            var battles = new BattleRecordDto[pr.Battles.Count];
            for (int i = 0; i < pr.Battles.Count; i++)
            {
                var b = pr.Battles[i];
                squadMap.TryGetValue(b.OpponentId, out var oppSquad);
                var oppProfile = ProfileRules.Sanitize(Lookup(profileMap, b.OpponentId));
                battles[i] = new BattleRecordDto
                {
                    opponentId = b.OpponentId,
                    opponentName = oppProfile.displayName,
                    opponentColor = oppProfile.colorHex,
                    won = b.Won,
                    seed = b.Seed,
                    modifierId = modifierId,
                    opponentSquad = oppSquad
                };
            }

            squadMap.TryGetValue(pr.PlayerId, out var mine);
            var myProfile = ProfileRules.Sanitize(Lookup(profileMap, pr.PlayerId));
            return new DayResultDto
            {
                day = day,
                wins = pr.Wins,
                losses = pr.Losses,
                remainingHpAcrossWins = pr.RemainingHpAcrossWins,
                myName = myProfile.displayName,
                myColor = myProfile.colorHex,
                mySquad = mine,
                battles = battles
            };
        }

        private static ProfileDto Lookup(Dictionary<string, ProfileDto> map, string id)
        {
            return id != null && map.TryGetValue(id, out var p) ? p : null;
        }

        // ---- weekly wins + leaderboard ----

        /// <summary>
        /// Accumulates the player's wins for the current week in Cloud Save. Tracking the total
        /// ourselves means the weekly reset is just the week number rolling over. This value is the
        /// source of truth the leaderboard board will display — the actual leaderboard submit is
        /// wired in M5 (the Leaderboards milestone), once the Cloud Code Leaderboards model type is
        /// pinned to a matching SDK version. `weekly_wins` sort/update config is already set on the
        /// dashboard and ready for it.
        /// </summary>
        private async Task UpdateWeekly(IExecutionContext ctx, IGameApiClient api,
            string playerId, int day, int dayWins, int dayRemainingHp)
        {
            int week = GameCalendar.WeekNumber(day);

            var raw = await ReadPlayerItem(ctx, api, playerId, WeeklyKey);
            var record = Json.From<WeeklyRecord>(raw);
            if (record == null || record.week != week)
                record = new WeeklyRecord { week = week, wins = 0, remainingHp = 0, lastDayCounted = -1 };

            // Idempotent: only add a given day's totals once, even if the resolve fires again.
            if (record.lastDayCounted == day) return;

            record.wins += dayWins;
            record.remainingHp += dayRemainingHp;
            record.lastDayCounted = day;
            await api.CloudSaveData.SetItemAsync(ctx, ctx.ServiceToken, ctx.ProjectId, playerId,
                new SetItemBody(WeeklyKey, Json.To(record)));
        }

        /// <summary>
        /// Returns the current week's individual + team standings, computed from each player's
        /// weekly Cloud Save record. A JSON string (field-based DTOs don't survive property-only
        /// return serialization); the client parses it with JsonUtility.
        /// </summary>
        [CloudCodeFunction("GetStandings")]
        public async Task<string> GetStandings(IExecutionContext ctx, IGameApiClient api)
        {
            int week = GameCalendar.WeekNumber(GameCalendar.ResolveDayFor(DateTime.UtcNow));
            var roster = await ReadRoster(ctx, api);

            var inputs = new List<PlayerStandingInput>();
            foreach (var playerId in roster)
            {
                var weekly = Json.From<WeeklyRecord>(await ReadPlayerItem(ctx, api, playerId, WeeklyKey));
                int wins = (weekly != null && weekly.week == week) ? weekly.wins : 0;
                int hp = (weekly != null && weekly.week == week) ? weekly.remainingHp : 0;

                var profile = ProfileRules.Sanitize(Json.From<ProfileDto>(await ReadPlayerItem(ctx, api, playerId, ProfileKey)));
                inputs.Add(new PlayerStandingInput
                {
                    PlayerId = playerId,
                    Name = profile.displayName,
                    ColorHex = profile.colorHex,
                    TeamId = profile.teamId,
                    Title = profile.title,
                    Wins = wins,
                    RemainingHp = hp
                });
            }

            var teams = (await ReadTeams(ctx, api)).teams;
            var standings = StandingsCalculator.Compute(week, inputs, teams);
            return Json.To(standings);
        }

        // ---- weekly reset + Dawn Crown (M6.3) ----

        /// <summary>
        /// Crowns the champion of a completed week, once. Idempotent: it records the week in the
        /// champions list and skips if already present. Runs at the start of the first resolve of a
        /// new week, while last week's weekly records are still intact.
        /// </summary>
        private async Task FinalizeWeekIfNeeded(IExecutionContext ctx, IGameApiClient api, int week, List<string> roster)
        {
            if (week < 0) return;

            var champions = await ReadChampions(ctx, api);
            foreach (var c in champions.champions ?? new ChampionDto[0])
                if (c.week == week) return; // already crowned

            // Gather that week's standings from the (still-intact) weekly records.
            var inputs = new List<PlayerStandingInput>();
            foreach (var playerId in roster)
            {
                var weekly = Json.From<WeeklyRecord>(await ReadPlayerItem(ctx, api, playerId, WeeklyKey));
                if (weekly == null || weekly.week != week) continue;

                var profile = ProfileRules.Sanitize(Json.From<ProfileDto>(await ReadPlayerItem(ctx, api, playerId, ProfileKey)));
                inputs.Add(new PlayerStandingInput
                {
                    PlayerId = playerId, Name = profile.displayName, ColorHex = profile.colorHex,
                    TeamId = profile.teamId, Title = profile.title, Wins = weekly.wins, RemainingHp = weekly.remainingHp
                });
            }
            if (inputs.Count == 0) return; // nobody played that week; nothing to crown

            var teams = (await ReadTeams(ctx, api)).teams;
            var standings = StandingsCalculator.Compute(week, inputs, teams);

            var champ = standings.players[0];
            var topTeam = (standings.teams != null && standings.teams.Length > 0) ? standings.teams[0] : null;

            // Grant the dated Dawn Crown to the champion (owned + auto-equipped as their title).
            var crownId = CosmeticCatalog.DawnCrownId(week);
            var wallet = await ReadWallet(ctx, api, champ.playerId);
            if (!CosmeticRules.Owns(wallet, crownId))
            {
                var owned = new List<string>(wallet.owned ?? new string[0]) { crownId };
                wallet.owned = owned.ToArray();
                await WriteWallet(ctx, api, champ.playerId, wallet);
            }
            var champProfile = ProfileRules.Sanitize(Json.From<ProfileDto>(await ReadPlayerItem(ctx, api, champ.playerId, ProfileKey)));
            champProfile.title = crownId;
            await api.CloudSaveData.SetItemAsync(ctx, ctx.ServiceToken, ctx.ProjectId, champ.playerId,
                new SetItemBody(ProfileKey, Json.To(champProfile)));

            // Record the champion in the hall of fame.
            var list = new List<ChampionDto>(champions.champions ?? new ChampionDto[0])
            {
                new ChampionDto
                {
                    week = week, playerId = champ.playerId, name = champ.name, wins = champ.wins,
                    topTeamId = topTeam?.teamId ?? "", topTeamName = topTeam?.name ?? ""
                }
            };
            await WriteChampions(ctx, api, list);

            _logger.LogInformation("Week {Week} crowned: {Name} ({Wins} wins).", week, champ.name, champ.wins);
        }

        [CloudCodeFunction("GetChampions")]
        public async Task<string> GetChampions(IExecutionContext ctx, IGameApiClient api)
        {
            return Json.To(await ReadChampions(ctx, api));
        }

        private async Task<ChampionListDto> ReadChampions(IExecutionContext ctx, IGameApiClient api)
        {
            try
            {
                var res = await api.CloudSaveData.GetCustomItemsAsync(ctx, ctx.ServiceToken, ctx.ProjectId,
                    RosterCustomId, new List<string> { ChampionsKey });
                var raw = res.Data.Results.FirstOrDefault(r => r.Key == ChampionsKey)?.Value?.ToString();
                return Json.From<ChampionListDto>(raw) ?? new ChampionListDto { champions = new ChampionDto[0] };
            }
            catch (ApiException e)
            {
                _logger.LogWarning("Champions read failed (treating as empty): {Err}", e.Message);
                return new ChampionListDto { champions = new ChampionDto[0] };
            }
        }

        private async Task WriteChampions(IExecutionContext ctx, IGameApiClient api, List<ChampionDto> champions)
        {
            var dto = new ChampionListDto { champions = champions.ToArray() };
            await api.CloudSaveData.SetCustomItemAsync(ctx, ctx.ServiceToken, ctx.ProjectId, RosterCustomId,
                new SetItemBody(ChampionsKey, Json.To(dto)));
        }

        // ---- Sparks currency (M6): reward showing up over winning ----

        private async Task AwardLockSparks(IExecutionContext ctx, IGameApiClient api, string playerId, int day)
        {
            var wallet = await ReadWallet(ctx, api, playerId);
            if (wallet.lastLockDay == day) return; // one lock bonus per day
            wallet.sparks += SparksPerLock;
            wallet.lastLockDay = day;
            await WriteWallet(ctx, api, playerId, wallet);
        }

        private async Task AwardWinSparks(IExecutionContext ctx, IGameApiClient api, string playerId, int day, int wins)
        {
            var wallet = await ReadWallet(ctx, api, playerId);
            if (wallet.lastWinDay == day) return; // idempotent per day
            wallet.sparks += SparksPerWin * (wins < 0 ? 0 : wins);
            wallet.lastWinDay = day;
            await WriteWallet(ctx, api, playerId, wallet);
        }

        private async Task<WalletDto> ReadWallet(IExecutionContext ctx, IGameApiClient api, string playerId)
        {
            var w = Json.From<WalletDto>(await ReadPlayerItem(ctx, api, playerId, WalletKey));
            return w ?? new WalletDto { sparks = 0, lastLockDay = -1, lastWinDay = -1 };
        }

        private async Task WriteWallet(IExecutionContext ctx, IGameApiClient api, string playerId, WalletDto wallet)
        {
            await api.CloudSaveData.SetItemAsync(ctx, ctx.ServiceToken, ctx.ProjectId, playerId,
                new SetItemBody(WalletKey, Json.To(wallet)));
        }

        // ---- cosmetic shop (M6) ----

        [CloudCodeFunction("BuyCosmetic")]
        public async Task<ShopResponse> BuyCosmetic(IExecutionContext ctx, IGameApiClient api, string cosmeticId)
        {
            var cosmetic = CosmeticCatalog.Get(cosmeticId);
            var wallet = await ReadWallet(ctx, api, ctx.PlayerId);

            if (!CosmeticRules.CanBuy(wallet, cosmetic, out var reason))
                return new ShopResponse { ok = false, sparks = wallet.sparks, error = reason };

            wallet.sparks -= cosmetic.cost;
            var owned = new List<string>(wallet.owned ?? new string[0]) { cosmetic.id };
            wallet.owned = owned.ToArray();
            await WriteWallet(ctx, api, ctx.PlayerId, wallet);

            return new ShopResponse { ok = true, sparks = wallet.sparks };
        }

        /// <summary>Equip (or clear, with "") a title the player owns. Purely cosmetic.</summary>
        [CloudCodeFunction("EquipTitle")]
        public async Task<ShopResponse> EquipTitle(IExecutionContext ctx, IGameApiClient api, string cosmeticId)
        {
            var wallet = await ReadWallet(ctx, api, ctx.PlayerId);
            if (!string.IsNullOrEmpty(cosmeticId) && !CosmeticRules.Owns(wallet, cosmeticId))
                return new ShopResponse { ok = false, sparks = wallet.sparks, error = "You don't own that." };

            var raw = await ReadPlayerItem(ctx, api, ctx.PlayerId, ProfileKey);
            var profile = ProfileRules.Sanitize(Json.From<ProfileDto>(raw));
            profile.title = cosmeticId ?? "";
            await api.CloudSaveData.SetItemAsync(ctx, ctx.AccessToken, ctx.ProjectId, ctx.PlayerId,
                new SetItemBody(ProfileKey, Json.To(profile)));

            return new ShopResponse { ok = true, sparks = wallet.sparks };
        }

        /// <summary>Dev/testing: grant Sparks so the shop can be exercised without waiting days.</summary>
        [CloudCodeFunction("GrantSparks")]
        public async Task<ShopResponse> GrantSparks(IExecutionContext ctx, IGameApiClient api, int amount)
        {
            var wallet = await ReadWallet(ctx, api, ctx.PlayerId);
            wallet.sparks += (amount < 0 ? 0 : amount);
            await WriteWallet(ctx, api, ctx.PlayerId, wallet);
            return new ShopResponse { ok = true, sparks = wallet.sparks };
        }

        // ---- teams (M5) ----

        private const string TeamsKey = "teams";

        [CloudCodeFunction("CreateTeam")]
        public async Task<TeamActionResponse> CreateTeam(IExecutionContext ctx, IGameApiClient api,
            string name, string colorHex)
        {
            var list = await ReadTeams(ctx, api);
            var teams = new List<TeamDto>(list.teams ?? new TeamDto[0]);

            string baseId = TeamRules.Slug(name);
            string id = baseId;
            for (int n = 2; TeamById(teams, id) != null; n++) id = baseId + "-" + n;

            // A player belongs to one team — pull them out of any existing team first.
            RemoveMemberEverywhere(teams, ctx.PlayerId);
            teams.Add(new TeamDto
            {
                id = id,
                name = TeamRules.SanitizeName(name),
                colorHex = ProfileRules.SanitizeColor(colorHex),
                memberIds = new[] { ctx.PlayerId }
            });

            await WriteTeams(ctx, api, teams);
            await SetOwnTeamId(ctx, api, id);
            return new TeamActionResponse { ok = true, teamId = id };
        }

        [CloudCodeFunction("JoinTeam")]
        public async Task<TeamActionResponse> JoinTeam(IExecutionContext ctx, IGameApiClient api, string teamId)
        {
            var list = await ReadTeams(ctx, api);
            var teams = new List<TeamDto>(list.teams ?? new TeamDto[0]);

            var team = TeamById(teams, teamId);
            if (team == null) return new TeamActionResponse { ok = false, teamId = teamId, error = "No such team." };

            RemoveMemberEverywhere(teams, ctx.PlayerId);
            var members = new List<string>(team.memberIds ?? new string[0]);
            if (!members.Contains(ctx.PlayerId)) members.Add(ctx.PlayerId);
            team.memberIds = members.ToArray();

            await WriteTeams(ctx, api, teams);
            await SetOwnTeamId(ctx, api, teamId);
            return new TeamActionResponse { ok = true, teamId = teamId };
        }

        [CloudCodeFunction("LeaveTeam")]
        public async Task<TeamActionResponse> LeaveTeam(IExecutionContext ctx, IGameApiClient api)
        {
            var list = await ReadTeams(ctx, api);
            var teams = new List<TeamDto>(list.teams ?? new TeamDto[0]);
            RemoveMemberEverywhere(teams, ctx.PlayerId);
            await WriteTeams(ctx, api, teams);
            await SetOwnTeamId(ctx, api, "");
            return new TeamActionResponse { ok = true, teamId = "" };
        }

        // Returns a JSON string (not the DTO) because the field-based TeamListDto doesn't survive
        // the framework's property-based return serialization; the client parses it with JsonUtility.
        [CloudCodeFunction("ListTeams")]
        public async Task<string> ListTeams(IExecutionContext ctx, IGameApiClient api)
        {
            return Json.To(await ReadTeams(ctx, api));
        }

        private static TeamDto TeamById(List<TeamDto> teams, string id)
        {
            foreach (var t in teams) if (t.id == id) return t;
            return null;
        }

        private static void RemoveMemberEverywhere(List<TeamDto> teams, string playerId)
        {
            foreach (var t in teams)
            {
                if (t.memberIds == null) continue;
                var kept = new List<string>();
                foreach (var m in t.memberIds) if (m != playerId) kept.Add(m);
                t.memberIds = kept.ToArray();
            }
            teams.RemoveAll(t => t.memberIds == null || t.memberIds.Length == 0);
        }

        private async Task<TeamListDto> ReadTeams(IExecutionContext ctx, IGameApiClient api)
        {
            try
            {
                var res = await api.CloudSaveData.GetCustomItemsAsync(ctx, ctx.ServiceToken, ctx.ProjectId,
                    RosterCustomId, new List<string> { TeamsKey });
                var raw = res.Data.Results.FirstOrDefault(r => r.Key == TeamsKey)?.Value?.ToString();
                return Json.From<TeamListDto>(raw) ?? new TeamListDto { teams = new TeamDto[0] };
            }
            catch (ApiException e)
            {
                _logger.LogWarning("Teams read failed (treating as empty): {Err}", e.Message);
                return new TeamListDto { teams = new TeamDto[0] };
            }
        }

        private async Task WriteTeams(IExecutionContext ctx, IGameApiClient api, List<TeamDto> teams)
        {
            var dto = new TeamListDto { teams = teams.ToArray() };
            await api.CloudSaveData.SetCustomItemAsync(ctx, ctx.ServiceToken, ctx.ProjectId, RosterCustomId,
                new SetItemBody(TeamsKey, Json.To(dto)));
        }

        private async Task SetOwnTeamId(IExecutionContext ctx, IGameApiClient api, string teamId)
        {
            // Read the caller's own profile (as the player), set teamId, write it back.
            var raw = await ReadPlayerItem(ctx, api, ctx.PlayerId, ProfileKey);
            var profile = ProfileRules.Sanitize(Json.From<ProfileDto>(raw));
            profile.teamId = teamId;
            await api.CloudSaveData.SetItemAsync(ctx, ctx.AccessToken, ctx.ProjectId, ctx.PlayerId,
                new SetItemBody(ProfileKey, Json.To(profile)));
        }

        /// <summary>Testing/admin: wipe the active-player roster (clears accumulated test accounts).</summary>
        [CloudCodeFunction("ResetRoster")]
        public async Task<string> ResetRoster(IExecutionContext ctx, IGameApiClient api)
        {
            await api.CloudSaveData.SetCustomItemAsync(ctx, ctx.ServiceToken, ctx.ProjectId, RosterCustomId,
                new SetItemBody(RosterKey, "[]"));
            return "Roster cleared.";
        }

        // ---- Cloud Save helpers ----

        private async Task<string> ReadPlayerItem(IExecutionContext ctx, IGameApiClient api, string playerId, string key)
        {
            try
            {
                var res = await api.CloudSaveData.GetItemsAsync(ctx, ctx.ServiceToken, ctx.ProjectId, playerId,
                    new List<string> { key });
                return res.Data.Results.FirstOrDefault(r => r.Key == key)?.Value?.ToString();
            }
            catch (ApiException e)
            {
                _logger.LogWarning("Read of '{Key}' for {PlayerId} failed: {Err}", key, playerId, e.Message);
                return null;
            }
        }

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
            return Json.From<SquadDto>(await ReadPlayerItem(ctx, api, playerId, LockedSquadKey));
        }
    }

    /// <summary>Server-only: a player's running win total for one week.</summary>
    public class WeeklyRecord
    {
        public int week;
        public int wins;
        public int remainingHp;         // summed across wins this week (leaderboard tiebreak)
        public int lastDayCounted = -1; // guards against double-counting on repeated resolves
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

    public class TeamActionResponse
    {
        public bool ok { get; set; }
        public string teamId { get; set; }
        public string error { get; set; }
    }

    public class ShopResponse
    {
        public bool ok { get; set; }
        public int sparks { get; set; }
        public string error { get; set; }
    }
}
