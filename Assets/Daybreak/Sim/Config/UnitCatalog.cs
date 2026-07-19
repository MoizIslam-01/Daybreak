using System;
using System.Collections.Generic;

namespace Daybreak.Sim
{
    /// <summary>
    /// The shared unit table. Deliberately holds no parsing logic — the client and the Cloud Code
    /// module each deserialize units.json with their own JSON stack and hand the defs in here, so
    /// the sim assembly stays dependency-free. Both sides must load the identical file.
    /// </summary>
    public sealed class UnitCatalog
    {
        private readonly Dictionary<string, UnitDef> _byId;
        private readonly List<string> _orderedIds;

        public UnitCatalog(IEnumerable<UnitDef> defs)
        {
            if (defs == null) throw new ArgumentNullException(nameof(defs));

            _byId = new Dictionary<string, UnitDef>(StringComparer.Ordinal);
            _orderedIds = new List<string>();

            foreach (var def in defs)
            {
                if (def == null || string.IsNullOrEmpty(def.Id))
                    throw new ArgumentException("Unit definition is missing an id.");
                if (_byId.ContainsKey(def.Id))
                    throw new ArgumentException("Duplicate unit id '" + def.Id + "' in config.");

                _byId.Add(def.Id, def);
                _orderedIds.Add(def.Id);
            }

            // Sort so any iteration over the catalog is order-stable regardless of file order.
            _orderedIds.Sort(StringComparer.Ordinal);
        }

        public int Count => _orderedIds.Count;

        /// <summary>Ids in a fixed sort order. Never iterate the dictionary directly.</summary>
        public IReadOnlyList<string> Ids => _orderedIds;

        public UnitDef Get(string id)
        {
            if (id != null && _byId.TryGetValue(id, out var def)) return def;
            throw new KeyNotFoundException("Unknown unit id '" + id + "'.");
        }

        public bool TryGet(string id, out UnitDef def)
        {
            if (id == null) { def = null; return false; }
            return _byId.TryGetValue(id, out def);
        }
    }
}
