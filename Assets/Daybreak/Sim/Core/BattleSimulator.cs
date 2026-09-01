using System;
using System.Collections.Generic;

namespace Daybreak.Sim
{
    /// <summary>
    /// The authoritative battle resolver: pure, deterministic, and Unity-free so the identical
    /// code runs in the client (practice/preview) and in the Cloud Code module (real resolves).
    ///
    /// Rules of the house — enforce these in review:
    ///   no floats, no System.Random, no DateTime, no unordered collection iteration.
    ///
    /// Event log conventions (what the M2 renderer will read):
    ///   Tick            monotonically increasing across the whole battle; a total order.
    ///   BattleStart     once, first.
    ///   SynergyApplied  Source = side (0/1), Aux = (int)Tag, Value = buff (permille, or flat true dmg).
    ///   ModifierApplied Source = side or -1 for global; Value/Aux carry the modifier id hash (informational).
    ///   RoundStart      Value = round number (1-based).
    ///   Action          Source = acting unit. Marks whose turn it is.
    ///   Attack          Source -> Target. Aux = archetype multiplier (permille).
    ///   Damage          Target, Value = total damage dealt, Aux = archetype multiplier (permille).
    ///   Death           Target = the unit that died.
    ///   BattleEnd       Value = winning side (0/1).
    /// </summary>
    public static class BattleSimulator
    {
        public const int MaxRounds = 30;

        public const int FavorablePermille = 1250;
        public const int NeutralPermille = 1000;
        public const int UnfavorablePermille = 800;

        public static BattleResult Simulate(Squad a, Squad b, WeeklyModifier mod, int seed, UnitCatalog defs)
        {
            if (a == null || b == null) throw new ArgumentNullException();
            if (defs == null) throw new ArgumentNullException(nameof(defs));
            if (mod == null) mod = WeeklyModifier.None;
            if (!a.IsValid(out var ea)) throw new ArgumentException("Side A squad invalid: " + ea);
            if (!b.IsValid(out var eb)) throw new ArgumentException("Side B squad invalid: " + eb);

            var rng = new DeterministicRng(seed);
            var log = new List<BattleEvent>(256);
            int tick = 0;

            // Tactic travels with each squad, indexed by side.
            var sideTactic = new[] { a.Tactic, b.Tactic };

            log.Add(new BattleEvent(tick++, EventType.BattleStart));

            // --- Battle start: synergies, then build instances with effective stats. ---
            var buffsA = SynergyRules.Compute(a, defs, mod.DoubleArcaneTrueDamage);
            var buffsB = SynergyRules.Compute(b, defs, mod.DoubleArcaneTrueDamage);

            EmitSynergy(log, ref tick, BattleResult.SideA, a, defs, mod.DoubleArcaneTrueDamage);
            EmitSynergy(log, ref tick, BattleResult.SideB, b, defs, mod.DoubleArcaneTrueDamage);

            if (!mod.IsNone)
                log.Add(new BattleEvent(tick++, EventType.ModifierApplied, source: -1, value: StableHash(mod.Id)));

            var units = new UnitInstance[10];
            BuildSide(units, 0, a, defs, buffsA, mod);
            BuildSide(units, 5, b, defs, buffsB, mod);

            int arcaneA = buffsA.ArcaneTrueDamage;
            int arcaneB = buffsB.ArcaneTrueDamage;

            // --- Rounds. ---
            int winner = -1;
            for (int round = 1; round <= MaxRounds; round++)
            {
                log.Add(new BattleEvent(tick++, EventType.RoundStart, value: round));

                var order = BuildActionOrder(units);
                for (int i = 0; i < order.Count; i++)
                {
                    var actor = order[i];
                    if (!actor.Alive) continue;

                    var enemies = LivingEnemies(units, actor.Side);
                    if (enemies.Count == 0) { winner = actor.Side; break; }

                    var target = ChooseTarget(actor, enemies, sideTactic[actor.Side]);
                    log.Add(new BattleEvent(tick++, EventType.Action, source: actor.Index));

                    int archMult = ArchetypeMultiplierPermille(actor.Arch, target.Arch);
                    int arcaneTrue = actor.Side == 0 ? arcaneA : arcaneB;
                    int dmg = ComputeDamage(actor, target, archMult, arcaneTrue, mod);

                    log.Add(new BattleEvent(tick++, EventType.Attack, source: actor.Index, target: target.Index, aux: archMult));
                    target.CurHp -= dmg;
                    log.Add(new BattleEvent(tick++, EventType.Damage, source: actor.Index, target: target.Index, value: dmg, aux: archMult));

                    if (!target.Alive)
                    {
                        target.CurHp = 0;
                        log.Add(new BattleEvent(tick++, EventType.Death, target: target.Index));
                    }
                }

                if (winner >= 0) break;

                bool aDead = LivingCount(units, 0) == 0;
                bool bDead = LivingCount(units, 1) == 0;
                if (aDead || bDead)
                {
                    winner = aDead ? BattleResult.SideB : BattleResult.SideA;
                    break;
                }
            }

            // --- Cap reached with both alive: higher remaining HP % wins; exact tie -> coin flip. ---
            if (winner < 0)
                winner = ResolveByHealth(units, ref rng);

            log.Add(new BattleEvent(tick++, EventType.BattleEnd, value: winner));

            return new BattleResult
            {
                WinnerSide = winner,
                Log = log.ToArray(),
                Seed = seed
            };
        }

        public static int ArchetypeMultiplierPermille(Archetype attacker, Archetype defender)
        {
            if (attacker == defender) return NeutralPermille;
            return Beats(attacker, defender) ? FavorablePermille : UnfavorablePermille;
        }

        private static bool Beats(Archetype attacker, Archetype defender)
        {
            switch (attacker)
            {
                case Archetype.Vanguard: return defender == Archetype.Skirmisher;
                case Archetype.Skirmisher: return defender == Archetype.Marksman;
                case Archetype.Marksman: return defender == Archetype.Vanguard;
                default: return false;
            }
        }

        // ------------------------------------------------------------------ setup

        private static void BuildSide(UnitInstance[] units, int baseIndex, Squad squad,
            UnitCatalog defs, SynergyBuffs buffs, WeeklyModifier mod)
        {
            for (int i = 0; i < Squad.UnitCount; i++)
            {
                var p = squad.Units[i];
                var def = defs.Get(p.UnitId);

                int atk = def.Atk;
                int hp = def.HP;
                int dfn = def.Def;
                int spd = def.Spd;

                // Order is fixed (guide §4.5): synergy -> Berserk -> weekly modifier.
                atk = atk * buffs.AtkPermille / 1000;
                dfn = dfn * buffs.DefPermille / 1000;
                spd = spd * buffs.SpdPermille / 1000;

                if (squad.Tactic == Tactic.Berserk)
                {
                    atk = atk * 1250 / 1000;
                    dfn = dfn * 750 / 1000;
                }

                atk = atk * mod.AtkPermille / 1000;
                dfn = dfn * mod.DefPermille / 1000;
                hp = hp * mod.HpPermille / 1000;
                spd = spd * mod.SpdPermille / 1000;

                if (mod.HasArchetypeBuff && def.Arch == mod.BuffArchetype)
                {
                    int p2 = mod.ArchetypeAllStatsPermille;
                    atk = atk * p2 / 1000;
                    dfn = dfn * p2 / 1000;
                    hp = hp * p2 / 1000;
                    spd = spd * p2 / 1000;
                }

                if (hp < 1) hp = 1;
                if (atk < 0) atk = 0;
                if (dfn < 0) dfn = 0;
                if (spd < 0) spd = 0;

                units[baseIndex + i] = new UnitInstance
                {
                    Index = baseIndex + i,
                    Side = baseIndex == 0 ? 0 : 1,
                    UnitId = def.Id,
                    Arch = def.Arch,
                    Tag = def.Tag,
                    Reach = def.AttackReach,
                    Row = p.Row,
                    Col = p.Col,
                    MaxHp = hp,
                    CurHp = hp,
                    Atk = atk,
                    Def = dfn,
                    Spd = spd
                };
            }
        }

        private static void EmitSynergy(List<BattleEvent> log, ref int tick, int side,
            Squad squad, UnitCatalog defs, bool doubleArcane)
        {
            var buffs = SynergyRules.Compute(squad, defs, doubleArcane);
            if (buffs.AtkPermille != 1000)
                log.Add(new BattleEvent(tick++, EventType.SynergyApplied, source: side, value: buffs.AtkPermille, aux: (int)Tag.Pack));
            if (buffs.DefPermille != 1000)
                log.Add(new BattleEvent(tick++, EventType.SynergyApplied, source: side, value: buffs.DefPermille, aux: (int)Tag.Guardian));
            if (buffs.SpdPermille != 1000)
                log.Add(new BattleEvent(tick++, EventType.SynergyApplied, source: side, value: buffs.SpdPermille, aux: (int)Tag.Swift));
            if (buffs.ArcaneTrueDamage != 0)
                log.Add(new BattleEvent(tick++, EventType.SynergyApplied, source: side, value: buffs.ArcaneTrueDamage, aux: (int)Tag.Arcane));
        }

        // ------------------------------------------------------------------ turn order & targeting

        private static List<UnitInstance> BuildActionOrder(UnitInstance[] units)
        {
            var living = new List<UnitInstance>(10);
            for (int i = 0; i < units.Length; i++)
                if (units[i].Alive) living.Add(units[i]);

            // Speed descending; ties broken by ascending instance index. Stable and float-free.
            living.Sort((x, y) =>
            {
                if (x.Spd != y.Spd) return y.Spd - x.Spd;
                return x.Index - y.Index;
            });
            return living;
        }

        private static List<UnitInstance> LivingEnemies(UnitInstance[] units, int side)
        {
            var list = new List<UnitInstance>(5);
            for (int i = 0; i < units.Length; i++)
                if (units[i].Side != side && units[i].Alive) list.Add(units[i]); // index order
            return list;
        }

        private static int LivingCount(UnitInstance[] units, int side)
        {
            int n = 0;
            for (int i = 0; i < units.Length; i++)
                if (units[i].Side == side && units[i].Alive) n++;
            return n;
        }

        /// <summary>Reach filter first, then the squad's tactic selects within the reachable set.</summary>
        private static UnitInstance ChooseTarget(UnitInstance attacker, List<UnitInstance> enemies, Tactic tactic)
        {
            var reachable = ReachFilter(attacker, enemies);
            return SelectByTactic(attacker, reachable, tactic);
        }

        private static List<UnitInstance> ReachFilter(UnitInstance attacker, List<UnitInstance> enemies)
        {
            if (attacker.Reach == Reach.Ranged) return enemies; // may hit any row
            // Melee: only the enemy front row while any front-row enemy lives; else the back row.
            var front = new List<UnitInstance>(enemies.Count);
            for (int i = 0; i < enemies.Count; i++)
                if (enemies[i].Row == 0) front.Add(enemies[i]);
            return front.Count > 0 ? front : enemies;
        }

        private static UnitInstance SelectByTactic(UnitInstance attacker, List<UnitInstance> reachable, Tactic tactic)
        {
            switch (tactic)
            {
                case Tactic.FocusFire:
                    // Whole team piles on the single lowest-HP reachable enemy.
                    return PickMin(reachable, u => (u.CurHp, u.Index, 0L));

                case Tactic.ProtectBackline:
                {
                    // Prioritise clearing the enemy FRONT row first.
                    var pool = RowSubsetOrAll(reachable, 0);
                    return PickMin(pool, u => (u.CurHp, u.Index, 0L));
                }

                case Tactic.Berserk:
                    // Target the highest-ATK reachable enemy (negate ATK to reuse PickMin).
                    return PickMin(reachable, u => ((long)-u.Atk, u.CurHp, u.Index));

                case Tactic.Balanced:
                default:
                {
                    // Ranged dives the enemy back row by default; melee is already one row.
                    var pool = attacker.Reach == Reach.Ranged ? RowSubsetOrAll(reachable, 1) : reachable;
                    // Same column if reachable, else nearest; ties by lowest current HP, then index.
                    return PickMin(pool, u => ((long)ColDist(attacker, u), u.CurHp, u.Index));
                }
            }
        }

        private static List<UnitInstance> RowSubsetOrAll(List<UnitInstance> pool, int row)
        {
            var sub = new List<UnitInstance>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
                if (pool[i].Row == row) sub.Add(pool[i]);
            return sub.Count > 0 ? sub : pool;
        }

        private static int ColDist(UnitInstance a, UnitInstance b)
        {
            int d = a.Col - b.Col;
            return d < 0 ? -d : d;
        }

        private static UnitInstance PickMin(List<UnitInstance> pool, Func<UnitInstance, (long, long, long)> key)
        {
            UnitInstance best = null;
            (long, long, long) bestKey = default;
            for (int i = 0; i < pool.Count; i++)
            {
                var k = key(pool[i]);
                if (best == null || k.CompareTo(bestKey) < 0)
                {
                    best = pool[i];
                    bestKey = k;
                }
            }
            return best;
        }

        // ------------------------------------------------------------------ damage & resolution

        /// <summary>
        /// Physical damage before true damage (guide §4.5): max(1, ATK*archMult/1000 - DEF).
        /// Pure and public so the core formula can be unit-tested directly.
        /// </summary>
        public static int PhysicalDamage(int atk, int archMultPermille, int def)
        {
            int physical = atk * archMultPermille / 1000 - def;
            return physical < 1 ? 1 : physical;
        }

        /// <summary>
        /// Round-cap resolution: whichever side has the higher remaining-HP percentage wins.
        /// Compared without floats via cross-multiplication. An exact tie is broken by a
        /// deterministic coin flip so the leaderboard never has to record a draw.
        /// Pure and public for direct testing.
        /// </summary>
        public static int ResolveByHealthPercent(long aRem, long aMax, long bRem, long bMax, ref DeterministicRng rng)
        {
            long left = aRem * bMax;
            long right = bRem * aMax;
            if (left > right) return BattleResult.SideA;
            if (right > left) return BattleResult.SideB;
            return rng.NextBool() ? BattleResult.SideB : BattleResult.SideA;
        }

        private static int ComputeDamage(UnitInstance attacker, UnitInstance target,
            int archMult, int arcaneTrue, WeeklyModifier mod)
        {
            int total = PhysicalDamage(attacker.Atk, archMult, target.Def) + arcaneTrue;

            if (mod.BackRowDamageTakenPermille != 1000 && target.Row == 1)
            {
                total = total * mod.BackRowDamageTakenPermille / 1000;
                if (total < 1) total = 1;
            }

            return total;
        }

        private static int ResolveByHealth(UnitInstance[] units, ref DeterministicRng rng)
        {
            long aRem = 0, aMax = 0, bRem = 0, bMax = 0;
            for (int i = 0; i < units.Length; i++)
            {
                var u = units[i];
                if (u.Side == 0) { aRem += u.CurHp; aMax += u.MaxHp; }
                else { bRem += u.CurHp; bMax += u.MaxHp; }
            }
            return ResolveByHealthPercent(aRem, aMax, bRem, bMax, ref rng);
        }

        private static int StableHash(string s)
        {
            unchecked
            {
                uint h = 2166136261u;
                if (s != null)
                    for (int i = 0; i < s.Length; i++) { h ^= s[i]; h *= 16777619u; }
                return (int)h;
            }
        }
    }
}
