using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Daybreak.Sim;

namespace Daybreak.CloudCode
{
    /// <summary>
    /// Server-side loader for the shared unit table. Reads the identical units.json the Unity
    /// client reads (embedded at build time), so client previews and server resolves cannot drift.
    /// </summary>
    public static class UnitConfig
    {
        private static readonly Lazy<UnitCatalog> _catalog = new Lazy<UnitCatalog>(Load);

        public static UnitCatalog Units => _catalog.Value;

        private sealed class UnitJson
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Arch { get; set; }
            public string Tag { get; set; }
            public int Hp { get; set; }
            public int Atk { get; set; }
            public int Def { get; set; }
            public int Spd { get; set; }
        }

        private sealed class UnitsFile
        {
            public int SchemaVersion { get; set; }
            public List<UnitJson> Units { get; set; }
        }

        private static UnitCatalog Load()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("units.json");
            if (stream == null)
                throw new InvalidOperationException("Embedded units.json is missing from the Cloud Code module.");

            using var reader = new StreamReader(stream);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var file = JsonSerializer.Deserialize<UnitsFile>(reader.ReadToEnd(), options);

            if (file?.Units == null || file.Units.Count == 0)
                throw new InvalidOperationException("units.json parsed but contained no units.");

            var defs = new List<UnitDef>(file.Units.Count);
            foreach (var u in file.Units)
            {
                defs.Add(new UnitDef
                {
                    Id = u.Id,
                    Name = u.Name,
                    Arch = Enum.Parse<Archetype>(u.Arch),
                    Tag = Enum.Parse<Tag>(u.Tag),
                    HP = u.Hp,
                    Atk = u.Atk,
                    Def = u.Def,
                    Spd = u.Spd
                });
            }

            return new UnitCatalog(defs);
        }
    }
}
