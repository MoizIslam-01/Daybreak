using System.Threading.Tasks;
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
        [CloudCodeFunction("SayHello")]
        public HelloWorldResponse SayHello(IExecutionContext ctx, string name)
        {
            return new HelloWorldResponse
            {
                Message = $"Hello, {name}! Daybreak's server module is alive.",
                PlayerId = ctx.PlayerId,
                // Reading the sim here is the real assertion: if Daybreak.Sim had a Unity
                // dependency, this module would not have compiled at all.
                SimUnitCount = UnitConfig.Units.Count
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
