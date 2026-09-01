using System.Text;

namespace Daybreak.Sim
{
    /// <summary>
    /// Turns a BattleResult's event log into human-readable text — the "blow-by-blow" used to
    /// sanity-check battles on paper (guide M1) and handy for debugging the M2 renderer.
    /// Pure string building; no Unity, no IO.
    /// </summary>
    public static class BattleLogFormatter
    {
        public static string Format(Squad a, Squad b, WeeklyModifier mod, BattleResult result, UnitCatalog defs)
        {
            var names = new string[10];
            FillNames(names, 0, a, defs);
            FillNames(names, 5, b, defs);

            var sb = new StringBuilder();
            sb.Append("=== Daybreak battle  A(").Append(a.OwnerId).Append(", ").Append(a.Tactic)
              .Append(")  vs  B(").Append(b.OwnerId).Append(", ").Append(b.Tactic)
              .Append(")   modifier=").Append(mod.Id).Append("  seed=").Append(result.Seed).Append(" ===\n");

            foreach (var e in result.Log)
            {
                switch (e.Type)
                {
                    case EventType.BattleStart:
                        sb.Append("-- battle start\n"); break;
                    case EventType.SynergyApplied:
                        sb.Append("   synergy  side ").Append(Side(e.Source)).Append("  ")
                          .Append((Tag)e.Aux).Append(" -> ").Append(e.Value).Append('\n'); break;
                    case EventType.ModifierApplied:
                        sb.Append("   modifier applied\n"); break;
                    case EventType.RoundStart:
                        sb.Append("-- round ").Append(e.Value).Append('\n'); break;
                    case EventType.Attack:
                        sb.Append("   ").Append(Name(names, e.Source)).Append(" -> ")
                          .Append(Name(names, e.Target)).Append("  (x").Append(Mult(e.Aux)).Append(")\n"); break;
                    case EventType.Damage:
                        sb.Append("      ").Append(Name(names, e.Target)).Append(" takes ")
                          .Append(e.Value).Append('\n'); break;
                    case EventType.Death:
                        sb.Append("      ").Append(Name(names, e.Target)).Append(" dies\n"); break;
                    case EventType.BattleEnd:
                        sb.Append("== winner: side ").Append(Side(e.Value)).Append('\n'); break;
                }
            }
            return sb.ToString();
        }

        private static void FillNames(string[] names, int baseIndex, Squad s, UnitCatalog defs)
        {
            for (int i = 0; i < s.Units.Length; i++)
                names[baseIndex + i] = defs.Get(s.Units[i].UnitId).Name;
        }

        private static string Name(string[] names, int idx) =>
            idx < 0 || idx >= names.Length ? "?" : (names[idx] ?? "?") + "#" + idx;

        private static string Side(int s) => s == 0 ? "A" : "B";
        private static string Mult(int permille) => (permille / 1000) + "." + ((permille % 1000) / 10).ToString("D2");
    }
}
