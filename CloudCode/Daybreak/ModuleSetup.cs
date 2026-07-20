using Unity.Services.CloudCode.Apis.Extensions;
using Unity.Services.CloudCode.Core;

namespace Daybreak.CloudCode
{
    /// <summary>
    /// Registers the services the module can inject. The game API client is what Milestone 4 will
    /// use to read locked squads out of Cloud Save and write the day's results back.
    /// </summary>
    public class ModuleSetup : ICloudCodeSetup
    {
        public void Setup(ICloudCodeConfig config)
        {
            config.AddGameApiClient();
        }
    }
}
