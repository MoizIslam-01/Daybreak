using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Daybreak.Sim;
using UnityEngine;
#if DAYBREAK_UGS_CLOUDCODE
using Unity.Services.CloudCode;
#endif

namespace Daybreak.Client
{
    /// <summary>
    /// Thin wrapper over Cloud Code module calls: the M0 smoke test plus the M4 lock/resolve
    /// endpoints. Response field names are camelCase; the SDK matches the server's PascalCase
    /// properties case-insensitively (as proven by the M0 round trip).
    /// </summary>
    public static class CloudCodeService
    {
        public const string ModuleName = "Daybreak";
        public const string HelloWorldEndpoint = "SayHello";
        public const string LockSquadEndpoint = "LockSquad";
        public const string ResolveDayEndpoint = "ResolveDay";
        public const string ResetRosterEndpoint = "ResetRoster";
        public const string CreateTeamEndpoint = "CreateTeam";
        public const string JoinTeamEndpoint = "JoinTeam";
        public const string LeaveTeamEndpoint = "LeaveTeam";
        public const string ListTeamsEndpoint = "ListTeams";

        [Serializable]
        public class TeamActionResult { public bool ok; public string teamId; public string error; }

        [Serializable]
        public class LockResult { public bool ok; public int day; }

        [Serializable]
        public class ResolveResult { public int day; public int playersResolved; public int battlesRun; }

        /// <summary>Lock a squad on the server for today's resolve (validates + registers).</summary>
        public static async Task<LockResult> LockSquadAsync(SquadDto dto)
        {
#if DAYBREAK_UGS_CLOUDCODE
            var args = new Dictionary<string, object> { { "squadJson", JsonUtility.ToJson(dto) } };
            return await Unity.Services.CloudCode.CloudCodeService.Instance
                .CallModuleEndpointAsync<LockResult>(ModuleName, LockSquadEndpoint, args);
#else
            await Task.Yield();
            Debug.LogWarning("[Daybreak] Cloud Code not installed — lock is a no-op stub.");
            return new LockResult { ok = false, day = dto.day };
#endif
        }

        /// <summary>Trigger the daily resolve manually (for testing; normally the scheduler fires it).</summary>
        public static async Task<ResolveResult> ResolveDayAsync()
        {
#if DAYBREAK_UGS_CLOUDCODE
            return await Unity.Services.CloudCode.CloudCodeService.Instance
                .CallModuleEndpointAsync<ResolveResult>(ModuleName, ResolveDayEndpoint, new Dictionary<string, object>());
#else
            await Task.Yield();
            return new ResolveResult { day = 0, playersResolved = 0, battlesRun = 0 };
#endif
        }

        /// <summary>Dev/admin: clear the active-player roster (removes accumulated test accounts).</summary>
        public static async Task<string> ResetRosterAsync()
        {
#if DAYBREAK_UGS_CLOUDCODE
            return await Unity.Services.CloudCode.CloudCodeService.Instance
                .CallModuleEndpointAsync<string>(ModuleName, ResetRosterEndpoint, new Dictionary<string, object>());
#else
            await Task.Yield();
            return "stub";
#endif
        }

        public static async Task<TeamActionResult> CreateTeamAsync(string name, string colorHex)
        {
#if DAYBREAK_UGS_CLOUDCODE
            var args = new Dictionary<string, object> { { "name", name }, { "colorHex", colorHex } };
            return await Unity.Services.CloudCode.CloudCodeService.Instance
                .CallModuleEndpointAsync<TeamActionResult>(ModuleName, CreateTeamEndpoint, args);
#else
            await Task.Yield();
            return new TeamActionResult { ok = false, error = "no backend" };
#endif
        }

        public static async Task<TeamActionResult> JoinTeamAsync(string teamId)
        {
#if DAYBREAK_UGS_CLOUDCODE
            var args = new Dictionary<string, object> { { "teamId", teamId } };
            return await Unity.Services.CloudCode.CloudCodeService.Instance
                .CallModuleEndpointAsync<TeamActionResult>(ModuleName, JoinTeamEndpoint, args);
#else
            await Task.Yield();
            return new TeamActionResult { ok = false, error = "no backend" };
#endif
        }

        public static async Task<TeamActionResult> LeaveTeamAsync()
        {
#if DAYBREAK_UGS_CLOUDCODE
            return await Unity.Services.CloudCode.CloudCodeService.Instance
                .CallModuleEndpointAsync<TeamActionResult>(ModuleName, LeaveTeamEndpoint, new Dictionary<string, object>());
#else
            await Task.Yield();
            return new TeamActionResult { ok = false, error = "no backend" };
#endif
        }

        public static async Task<TeamListDto> ListTeamsAsync()
        {
#if DAYBREAK_UGS_CLOUDCODE
            // Server returns a JSON string; parse with JsonUtility (matches the field-based DTOs).
            var json = await Unity.Services.CloudCode.CloudCodeService.Instance
                .CallModuleEndpointAsync<string>(ModuleName, ListTeamsEndpoint, new Dictionary<string, object>());
            var list = string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<TeamListDto>(json);
            return list ?? new TeamListDto { teams = new TeamDto[0] };
#else
            await Task.Yield();
            return new TeamListDto { teams = new TeamDto[0] };
#endif
        }

        [Serializable]
        public class HelloWorldResponse
        {
            public string message;
            public string playerId;
            public int simUnitCount;
        }

        public static async Task<HelloWorldResponse> SayHelloAsync(string name)
        {
#if DAYBREAK_UGS_CLOUDCODE
            var args = new Dictionary<string, object> { { "name", name } };
            var response = await CloudCodeService_ModuleCall(args);
            return response;
#else
            await Task.Yield();
            Debug.LogWarning("[Daybreak] Cloud Code package not installed — returning a local stub response.");
            return new HelloWorldResponse
            {
                message = "Hello, " + name + " (local stub — no server was contacted).",
                playerId = AuthService.PlayerId,
                simUnitCount = 0
            };
#endif
        }

#if DAYBREAK_UGS_CLOUDCODE
        private static async Task<HelloWorldResponse> CloudCodeService_ModuleCall(Dictionary<string, object> args)
        {
            return await Unity.Services.CloudCode.CloudCodeService.Instance
                .CallModuleEndpointAsync<HelloWorldResponse>(ModuleName, HelloWorldEndpoint, args);
        }
#endif
    }
}
