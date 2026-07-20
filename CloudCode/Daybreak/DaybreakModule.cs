using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Core;

namespace Daybreak.CloudCode
{
    /// <summary>
    /// The Daybreak server module. Milestone 0 ships one endpoint whose only job is to prove the
    /// pipe works end to end — and, usefully, that the server can load and read Daybreak.Sim.
    /// ResolveDay (the nightly round-robin) lands here in Milestone 4.
    /// </summary>
    public class DaybreakModule
    {
        private readonly ILogger<DaybreakModule> _logger;

        public DaybreakModule(ILogger<DaybreakModule> logger)
        {
            _logger = logger;
        }

        [CloudCodeFunction("SayHello")]
        public HelloWorldResponse SayHello(IExecutionContext context, string name)
        {
            // Reading the sim here is the real assertion: if Daybreak.Sim had picked up a Unity
            // dependency, this module would not have compiled at all.
            var unitCount = UnitConfig.Units.Count;

            _logger.LogInformation("SayHello called by {PlayerId}; sim exposes {UnitCount} units.",
                context.PlayerId, unitCount);

            return new HelloWorldResponse
            {
                Message = $"Hello, {name}! Daybreak's server module is alive.",
                PlayerId = context.PlayerId,
                SimUnitCount = unitCount
            };
        }
    }

    public class HelloWorldResponse
    {
        public string Message { get; set; }
        public string PlayerId { get; set; }
        public int SimUnitCount { get; set; }
    }
}
