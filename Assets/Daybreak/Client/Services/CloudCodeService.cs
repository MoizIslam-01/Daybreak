using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if DAYBREAK_UGS_CLOUDCODE
using Unity.Services.CloudCode;
#endif

namespace Daybreak.Client
{
    /// <summary>
    /// Thin wrapper over Cloud Code module calls. Milestone 0 only needs the round trip to prove
    /// out: client → signed-in → server module → response. ResolveDay joins this in Milestone 4.
    /// </summary>
    public static class CloudCodeService
    {
        public const string ModuleName = "Daybreak";
        public const string HelloWorldEndpoint = "SayHello";

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
