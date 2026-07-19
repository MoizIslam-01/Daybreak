using System.Linq;
using System.Reflection;
using Daybreak.Sim;
using NUnit.Framework;

namespace Daybreak.Tests
{
    /// <summary>
    /// The load-bearing test of the whole architecture: if Daybreak.Sim ever picks up a Unity
    /// dependency, it stops running inside a Cloud Code C# module and the server can no longer
    /// resolve battles. Fail loudly and early.
    /// </summary>
    public class SimBoundaryTests
    {
        [Test]
        public void SimAssembly_ReferencesNothingUnity()
        {
            var simAssembly = typeof(BattleSimulator).Assembly;

            var offenders = simAssembly
                .GetReferencedAssemblies()
                .Select(a => a.Name)
                .Where(n => n.StartsWith("UnityEngine") || n.StartsWith("UnityEditor"))
                .ToArray();

            Assert.IsEmpty(offenders,
                "Daybreak.Sim must stay Unity-free. Offending references: " + string.Join(", ", offenders));
        }

        [Test]
        public void SimAssembly_UsesNoFloatingPointInPublicApi()
        {
            var simAssembly = typeof(BattleSimulator).Assembly;

            var offenders = simAssembly.GetTypes()
                .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                .Where(f => f.FieldType == typeof(float) || f.FieldType == typeof(double) || f.FieldType == typeof(decimal))
                .Select(f => f.DeclaringType.Name + "." + f.Name)
                .ToArray();

            Assert.IsEmpty(offenders,
                "The sim uses integer math only. Floating-point fields found: " + string.Join(", ", offenders));
        }
    }
}
