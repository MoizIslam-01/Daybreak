using System;
using System.Collections.Generic;
using Daybreak.Sim;
using UnityEngine;

namespace Daybreak.Client
{
    /// <summary>
    /// Loads the shared unit table from Resources/units.json. The Cloud Code module parses the
    /// same file with System.Text.Json — the file is the contract, not this class.
    /// </summary>
    public static class ConfigService
    {
        public const string UnitsResourcePath = "units";

        [Serializable]
        private class UnitJson
        {
            public string id;
            public string name;
            public string arch;
            public string tag;
            public int hp;
            public int atk;
            public int def;
            public int spd;
        }

        [Serializable]
        private class UnitsFile
        {
            public int schemaVersion;
            public UnitJson[] units;
        }

        private static UnitCatalog _cached;

        public static UnitCatalog Units => _cached ??= Load();

        public static UnitCatalog Load()
        {
            var asset = Resources.Load<TextAsset>(UnitsResourcePath);
            if (asset == null)
                throw new InvalidOperationException(
                    "Could not find Resources/" + UnitsResourcePath + ".json. The unit table is required to boot.");

            var file = JsonUtility.FromJson<UnitsFile>(asset.text);
            if (file?.units == null || file.units.Length == 0)
                throw new InvalidOperationException("units.json parsed but contained no units.");

            var defs = new List<UnitDef>(file.units.Length);
            foreach (var u in file.units)
            {
                defs.Add(new UnitDef
                {
                    Id = u.id,
                    Name = u.name,
                    Arch = ParseEnum<Archetype>(u.arch, u.id, "arch"),
                    Tag = ParseEnum<Tag>(u.tag, u.id, "tag"),
                    HP = u.hp,
                    Atk = u.atk,
                    Def = u.def,
                    Spd = u.spd
                });
            }

            return new UnitCatalog(defs);
        }

        private static T ParseEnum<T>(string raw, string unitId, string field) where T : struct
        {
            if (Enum.TryParse<T>(raw, false, out var value)) return value;
            throw new InvalidOperationException(
                "Unit '" + unitId + "' has an unrecognised " + field + " value '" + raw + "'.");
        }
    }
}
