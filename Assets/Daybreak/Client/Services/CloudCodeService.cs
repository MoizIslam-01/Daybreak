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
