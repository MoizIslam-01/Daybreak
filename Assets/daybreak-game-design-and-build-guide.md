# DAYBREAK — Game Design & Build Guide

> **Codename "DAYBREAK"** (placeholder — rename freely). A mobile, async, team-based
> auto-battler built in Unity for a small friend group. You set your orders once a day,
> and every night the game resolves the battles for you. You wake up to the results.

---

## 0. How to use this document (note for Cowork / the AI build partner)

This is the single source of truth for the game. When helping build it:

- **Treat everything in "Locked Decisions" as fixed** unless the user explicitly changes it. Don't re-litigate settled choices.
- **Drive work milestone by milestone** (see §13). Each milestone is independently shippable to the friend group.
- **Keep the battle simulation Unity-independent.** It lives in its own assembly (`Daybreak.Sim`) with *no* reference to `UnityEngine`. This is non-negotiable — it's what lets the same code run on the server. Details in §8.
- **Write determinism unit tests early** (§8, §12). The whole design depends on the sim being deterministic.
- **Before starting Milestone 4**, get the user to answer the questions in §14 (Open Decisions) — they affect matchmaking, timing, and push setup.
- Prefer **placeholder art** (colored shapes, simple sprites) until the core loop is fun. Do not block on visuals.

The point of the game is not the game — it's keeping a friend group talking after graduation. Every design choice below serves *"is there something worth arguing about in the group chat tomorrow morning?"* When trading off, optimize for that.

---

## 1. Vision & design pillars

**Elevator pitch:** Everyone logs in for ~2 minutes a day, builds a squad and picks a strategy, and locks it in. Overnight, all squads fight each other automatically. You watch the replays, see where you placed on the weekly leaderboard, and talk trash. Weekly reset, cosmetic rewards, no pay-to-win, no grind advantage.

**Design pillars (in priority order):**

1. **Two minutes a day, and forgiving.** If you miss a day, your last squad auto-repeats. The flakiest friend must still be able to participate.
2. **Strategic, not twitchy.** Depth comes from *reading the meta and countering it*, not reflexes. All input is tap-to-configure.
3. **Everyone starts equal, every week.** No power progression. The best *strategist* wins, not the person with the most free time. Rewards are cosmetic.
4. **The daily resolve is a shared moment.** Everyone's results drop at the same time → a reason to open the group chat.
5. **Small and finishable.** This is a first Unity multiplayer project. Scope ruthlessly toward the MVP in §13.

---

## 2. Locked Decisions

These were decided with the user and should be treated as fixed:

| Decision | Choice |
|---|---|
| Genre | Async auto-battler |
| Platform | Mobile, Unity |
| Structure | Team-based, individual play rolls up to team score |
| Cadence | Daily input, auto-resolves once per day; weekly leaderboard + reset |
| Battle presentation | **Watchable animated replay** (not just a result screen) |
| Progression | **Cosmetics only. Equal power baseline** — no power unlocks, no grind advantage |
| Sim authority | **Server runs the deterministic sim** and ships an event log; client animates it |
| Missed days | Auto-repeat the player's last locked squad |
| Backend | Unity Gaming Services (UGS) — see §9 |

---

## 3. The daily loop (player experience)

```
Morning        You open the app. You see:
               - Yesterday's results ("You went 4-1") + replays to watch
               - The current weekly leaderboard (you & your team)
               - This week's active MODIFIER (a rule twist)

Anytime        You build/adjust your squad for tonight:
  before         1. Pick 5 units from the shared roster
  lock time      2. Place them on the 2x3 grid (front row / back row)
                 3. Pick 1 tactic
               Tap LOCK. (If you don't, last squad auto-repeats.)

Lock time      Squads freeze. No more edits for tonight.
  (e.g. 20:00
   local/fixed)

Resolve        The server runs every matchup automatically (§4, §8).
               Each battle produces an event log (the replay).
               Scores update. Push notification: "Your battles resolved!"

Next morning   Repeat. You watch replays, argue in the chat, adjust strategy.
```

A "turn" is one day. Weekly, the leaderboard resets, rewards are handed out, and the modifier rotates.

---

## 4. Core mechanics

### 4.1 Squad & grid

- A **squad** is exactly **5 units** placed on a **2-row × 3-column grid**.
  - **Row 0 = front row** (closest to the enemy). **Row 1 = back row.**
  - Columns are `0, 1, 2`. One of the six slots is left empty (player's choice).
- Front-row units soak damage; back-row units are protected but deal the damage. Placement is a real decision.

### 4.2 Archetypes — the counter triangle

Every unit is one of three archetypes. They form a rock-paper-scissors:

```
        Vanguard ──beats──► Skirmisher ──beats──► Marksman ──beats──► Vanguard
```

- **Vanguard** — tanky front-liner. High HP/DEF, low ATK, low SPD. Melee reach (hits front row). *Outlasts Skirmishers.*
- **Skirmisher** — fast melee attacker. Medium HP, low DEF, high ATK, high SPD. Melee reach. *Runs down squishy Marksmen.*
- **Marksman** — ranged glass cannon. Low HP/DEF, high ATK, medium SPD. Ranged reach (can hit the back row). *Shreds slow Vanguards.*

The counter is expressed as a **damage multiplier** (see 4.5): favorable matchup ×1.25, unfavorable ×0.80, neutral ×1.00. Reading which archetypes your friends will bring and countering them is the core mind-game.

### 4.3 Synergies (tags)

Each unit also has one **tag**. Fielding multiples of a tag grants a team-wide buff, applied at battle start. Tags cut *across* archetypes, so building a synergy means mixing archetypes.

| Tag | 2 copies | 3 copies |
|---|---|---|
| **Guardian** | +15% team Defense | +30% team Defense |
| **Pack** | +12% team Attack | +25% team Attack |
| **Arcane** | attacks deal +4 *true* damage (ignores Defense) | +9 true damage |
| **Swift** | +15% team Speed | +30% team Speed |

Decision: chase a synergy (commit slots to one tag) vs. spread for flexible counters.

### 4.4 Tactics

The squad picks **one** tactic for the whole team. This shapes targeting and, for Berserk, stats.

- **Balanced** (default) — units attack the enemy in the same column if reachable, else the nearest; ties broken by lowest current HP.
- **Focus Fire** — the whole team piles onto the single lowest-HP reachable enemy. Great for bursting down a key unit; bad against evenly-tanky teams.
- **Protect Backline** — everyone (yours) prioritizes clearing the enemy **front row** first. Defensive/attrition play.
- **Berserk** — units target the highest-ATK reachable enemy; the squad gets **+25% ATK, −25% DEF** (applied at battle start). High risk, high reward.

### 4.5 Combat resolution (fully specified — build the sim to this)

The sim is **round-based** and fully deterministic. Given two squads, the weekly modifier, and a seed, it must always produce the identical outcome and event log.

**Setup (battle start):**
1. Instantiate unit instances from placements. Side A gets instance indices `0–4`, Side B gets `5–9`, assigned in placement order. (These indices are what the event log references.)
2. Compute **effective stats**: apply synergy buffs, tactic modifiers (Berserk), and the weekly modifier, in that fixed order. Round using integer math (see §8).
3. Emit `BattleStart`, then `SynergyApplied` / `ModifierApplied` events for the replay to show.

**Each round (cap at 30 rounds):**
1. Emit `RoundStart`.
2. Build the action order: all **living** units sorted by effective **Speed descending**; ties broken by ascending instance index. (Deterministic, no floats, no dictionary-order reliance.)
3. Each unit in order, if still alive, takes **one action** = attack a target chosen by the reach + tactic rules below. Emit `Action`, `Attack`, `Damage`, and `Death` events as they occur.

**Targeting:**
- **Reach filter first.** Melee (Vanguard, Skirmisher): may only hit the enemy **front row** while any front-row enemy lives; otherwise the back row. Ranged (Marksman): may hit **any** row; by default prefers the enemy **back row** (dives the carries), falling back to front row if the back is empty.
- **Then tactic** selects within the reachable set (see 4.4).

**Damage formula (all integer math):**
```
archMult   = 1250 | 1000 | 800     // permille: favorable | neutral | unfavorable
physical   = max(1, (attacker.ATK * archMult) / 1000 - target.DEF)   // integer division
trueDmg    = arcaneTrueDamage(attacker's team)                       // 0, 4, or 9
total      = physical + trueDmg
target.HP -= total
if target.HP <= 0 → Death event, remove from battle
```

**Win condition:**
- A side with no living units loses.
- If the 30-round cap is reached with both sides alive: the side with the **higher remaining total HP percentage** wins. Exact tie → deterministic coin flip from the seed (avoids draws so the leaderboard stays clean).
- Emit `BattleEnd` with the winning side.

> Deliberately **no active abilities in the MVP** — passives via tags only. Active abilities are a great post-launch addition; the event system already has room for them (`Ability` event type reserved).

---

## 5. Starter roster (12 units)

A clean 3×4 grid: every archetype has access to every synergy tag. That guarantees build variety. Numbers are a starting point — **expect to rebalance in the first few weeks** (that's normal and easy, since every stat lives in one shared sim).

Base stat blocks by archetype (HP / ATK / DEF / SPD):
- Vanguard `120 / 14 / 8 / 4`
- Skirmisher `70 / 22 / 3 / 9`
- Marksman `55 / 26 / 2 / 6`

| Unit | Archetype | Tag | HP | ATK | DEF | SPD |
|---|---|---|---|---|---|---|
| Aegis | Vanguard | Guardian | 130 | 13 | 10 | 4 |
| Warhound Alpha | Vanguard | Pack | 118 | 16 | 8 | 5 |
| Runeshield | Vanguard | Arcane | 120 | 14 | 8 | 4 |
| Charger | Vanguard | Swift | 110 | 15 | 7 | 6 |
| Sentinel Blade | Skirmisher | Guardian | 78 | 20 | 5 | 8 |
| Ripper | Skirmisher | Pack | 70 | 24 | 3 | 9 |
| Hexblade | Skirmisher | Arcane | 70 | 21 | 3 | 9 |
| Flicker | Skirmisher | Swift | 64 | 22 | 2 | 11 |
| Pavise | Marksman | Guardian | 62 | 24 | 4 | 5 |
| Houndmaster | Marksman | Pack | 55 | 28 | 2 | 6 |
| Arcanist | Marksman | Arcane | 55 | 25 | 2 | 6 |
| Windrunner | Marksman | Swift | 50 | 26 | 1 | 8 |

Store these as **data (a config asset / JSON)**, not hardcoded, so the user can tune them without recompiling — and so the server and client read the identical definitions.

---

## 6. Weekly cycle: teams, leaderboards, modifiers, rewards

### 6.1 Matchmaking (daily)
- **Round-robin among all active players** each day: your locked squad fights every other active player's locked squad once. With N friends that's N−1 battles each. Simple, fair, and everyone contributes.
- Each battle is a **ghost battle** (you fight their locked snapshot; nobody needs to be online). Store `{opponentId, winnerSide, seed}` per battle so the replay regenerates from the sim on demand (a few hundred bytes, not a video).
- *If the group ever grows past ~30, switch to brackets.* Not an MVP concern.

### 6.2 Scoring & leaderboards
- **+1 point per win.** Weekly personal score = total wins this week. Tiebreak: total remaining HP across wins.
- **Team score** = sum of members' wins.
- Two leaderboards: **Individual (weekly)** and **Team (weekly)**, via the UGS Leaderboards service.

### 6.3 Weekly modifier (freshness engine)
A config-driven rule twist active for the week, applied in the sim at battle start. Rotates on reset. Examples:
- *Entrenched* — back-row units take 25% less damage.
- *Glass Cannons* — all units +20% ATK, −20% HP.
- *Vanguard's Hour* — Vanguards +15% to all stats.
- *Arcane Surge* — Arcane true-damage thresholds doubled.

This is the cheapest possible longevity lever (just numbers) and the biggest driver of "ugh, they changed it again" chatter. Ship a handful and rotate.

### 6.4 Rewards & economy (cosmetic only)
- **Currency ("Sparks")**, via UGS Economy. Earned mostly for *showing up*: **+10 for locking a squad each day**, **+2 per win**. This deliberately rewards the reliable friend over the merely-best one.
- **Cosmetic shop:** unit skins, team banners, profile frames, victory effects. Purely visual.
- **Weekly champion cosmetic ("Dawn Crown"):** dated, granted only to the #1 individual on reset — the *one* thing you can't just grind for. Top team gets a special banner. This is the bragging-rights carrot with zero power creep.

---

## 7. Anti-snowball & balance philosophy

Equal power makes unit balance load-bearing (you can't hide an overtuned unit behind "you have to unlock it"). Upside: it's very tractable.

- All stats live in **one shared config** consumed by both client and server. Tweak a number, everyone re-fetches, done.
- The **weekly reset** wipes standings — a bad week never compounds.
- Because rewards are cosmetic and currency favors participation, the power gap between the sweatiest and flakiest friend is **always zero**. That's the whole point; protect it.

---

## 8. Technical architecture

### 8.1 The golden rule: one deterministic sim, two homes
The battle simulation is a **pure C# library** (`Daybreak.Sim`) with **no `UnityEngine` dependency**. It is referenced by:
1. the **Unity client** (for local practice/preview battles and — optionally — to render), and
2. a **UGS Cloud Code C# module** on the server (the authoritative resolver).

UGS Cloud Code C# modules run on the open-source .NET runtime and *cannot* use `UnityEngine`, which is exactly why the sim must be Unity-free. Enforce this with an **assembly definition (`.asmdef`)** that references nothing Unity-specific.

### 8.2 Responsibilities

| Concern | Client (Unity, mobile) | Server (UGS) |
|---|---|---|
| Auth | UGS Authentication SDK | Authentication service |
| Squad building UI | ✅ | — |
| Store locked squad | writes to Cloud Save | Cloud Save |
| Run the sim | only for local practice/preview | **authoritative** (Cloud Code module) |
| Daily resolve | — | Scheduler → Trigger → Cloud Code |
| Leaderboards | reads/displays | Leaderboards service (write + reset) |
| Rewards/currency | reads/displays, shop | Economy service |
| Replay | **animates the event log** | produces & stores the event log/seed |
| Push notifications | receives (FCM) | fires on resolve (see §9 note) |

### 8.3 The replay
Because the server runs the sim once and the outcome is deterministic, a replay is just `(squadA, squadB, weeklyModifier, seed)`. Store those; regenerate the full event log by re-running `Simulate` whenever someone taps "watch." The client's `ReplayPlayer` reads the event list and moves sprites / ticks HP bars — it never decides outcomes.

### 8.4 Determinism requirements (critical — put these in code review)
- **No floats.** Integer or fixed-point only. Money-style integer math throughout the damage/stat pipeline.
- **Seeded PRNG only.** Use a small, explicit deterministic RNG (e.g., xorshift/PCG) seeded per battle. Never `System.Random` with a time seed, never `UnityEngine.Random`.
- **No wall-clock / DateTime** inside the sim.
- **Stable ordering.** Never iterate a `Dictionary`/`HashSet` where order affects results; sort by explicit deterministic keys (Speed desc, then instance index).
- **Same config on both sides.** Client and server must load identical unit/modifier definitions.
- **Unit test it:** same inputs + seed → byte-identical event log, asserted in CI (Milestone 1).

---

## 9. Backend on Unity Gaming Services (mapping)

| Need | UGS service |
|---|---|
| Player identity | **Authentication** (anonymous or platform sign-in) |
| Store locked squads, results, cosmetics, team membership | **Cloud Save** |
| Run the authoritative battle sim | **Cloud Code — C# module** (references `Daybreak.Sim`) |
| Fire the daily resolve at a fixed time | **Scheduler** (recurring cron, e.g. `0 20 * * *`) → **Triggers** → Cloud Code |
| Weekly leaderboards (individual + team) | **Leaderboards** |
| Weekly reset → hand out rewards | Leaderboards **reset event** → **Triggers** → Cloud Code |
| Cosmetic currency & ownership | **Economy** |

There's an official UGS use-case sample ("reward top players at end of season") that wires Scheduler → Trigger → Leaderboards → Economy — essentially the weekly cycle. Use it as a template.

**Push notifications:** UGS has no first-party push service. Use **Firebase Cloud Messaging (FCM)** — free, standard for Unity mobile. The daily-resolve Cloud Code module can call out (HTTP) to send pushes, or you trigger sends from a small companion function. Set this up in Milestone 5.

**Daily resolve module — pseudocode:**
```
ResolveDay():
  players     = load all active players (Cloud Save)
  squads      = each player's locked squad (or their last squad if none locked today)
  modifier    = current weekly modifier
  for each unordered pair (A, B):
      seed    = deterministicSeed(day, A.id, B.id)
      result  = Daybreak.Sim.Simulate(A.squad, B.squad, modifier, seed)
      record  {A, B, result.WinnerSide, seed}   // replay = squads + seed
      award wins → tally
  write per-player day results (Cloud Save)
  update Leaderboards (individual + team)
  grant participation/win Sparks (Economy)
  fire push notifications (FCM)
  // Make it idempotent — triggers can fire more than once.
```

---

## 10. Data model

Stored in Cloud Save unless noted. Illustrative shapes:

```
Player {
  id, displayName, teamId,
  currency: int,              // or via Economy
  ownedCosmetics: [id],
  equipped: { skinByUnit, banner, frame, victoryFx }
}

Squad {                       // one per player per day
  ownerId, day,
  units: [ { unitId, row(0|1), col(0..2) } ],   // exactly 5
  tactic,                     // Balanced | FocusFire | ProtectBackline | Berserk
  lockedAt
}

DayResult {                   // one per player per day
  ownerId, day,
  wins, losses,
  battles: [ { opponentId, winnerSide, seed } ]   // replays regenerate from these
}

Team { id, name, memberIds: [id], banner }

WeeklyState { weekNumber, modifierId }   // small, global
```
Individual + team weekly standings live in the **Leaderboards** service, not hand-rolled.

---

## 11. Unity project structure & tech stack

**Tech stack**
- Unity: current **LTS** release, 2D.
- UGS packages: Authentication, Cloud Save, Cloud Code, Leaderboards, Economy (+ the Deployment package for Cloud Code modules).
- Firebase Cloud Messaging for push.
- Placeholder art (colored quads / simple sprites) until the loop is fun.

**Assemblies (enforce the sim boundary)**
```
Daybreak.Sim      (.asmdef, NO UnityEngine ref)  ← battle sim + data types + RNG
Daybreak.Client   (references Daybreak.Sim)       ← Unity: UI, replay renderer, services
CloudCode/        (C# module, references Daybreak.Sim as a library)  ← server resolver
```

**Scenes**
- `Boot` — sign in, load config
- `Home` — yesterday's results, leaderboard snapshot, this week's modifier, replay entry points
- `SquadBuilder` — roster, 2×3 grid placement, tactic picker, LOCK
- `ReplayViewer` — animates an event log
- `Leaderboard` — individual + team tabs
- `Team` — roster, banner
- `Shop` — cosmetics

**Key client systems**
- `AuthService`, `DataService` (Cloud Save wrapper), `ConfigService` (units/modifiers)
- `SquadBuilderController`
- `ReplayPlayer` — consumes `BattleEvent[]`, drives sprites + HP bars (owns *no* game logic)
- `LeaderboardService`, `EconomyService`, `NotificationService` (FCM)

---

## 12. Sim API sketch (target shape for `Daybreak.Sim`)

```csharp
public enum Archetype { Vanguard, Skirmisher, Marksman }
public enum Tag       { Guardian, Pack, Arcane, Swift }
public enum Tactic    { Balanced, FocusFire, ProtectBackline, Berserk }

public sealed class UnitDef {          // loaded from shared config
    public string Id; public string Name;
    public Archetype Arch; public Tag Tag;
    public int HP, Atk, Def, Spd;
}

public struct Placement { public string UnitId; public int Row; public int Col; } // Row 0=front,1=back

public sealed class Squad {
    public string OwnerId;
    public Placement[] Units;          // exactly 5
    public Tactic Tactic;
}

public sealed class WeeklyModifier { public string Id; /* typed params */ }

public enum EventType {
    BattleStart, SynergyApplied, ModifierApplied,
    RoundStart, Action, Attack, Damage, Death, Ability /*reserved*/, BattleEnd
}

public struct BattleEvent {
    public int Tick;                   // ordering for the replay
    public EventType Type;
    public int Source;                 // unit instance index 0..9
    public int Target;                 // unit instance index 0..9
    public int Value;                  // damage / heal / etc.
    public int Aux;                    // spare (e.g. which multiplier applied)
}

public sealed class BattleResult {
    public int WinnerSide;             // 0 = side A, 1 = side B
    public BattleEvent[] Log;
    public int Seed;
}

// Pure, deterministic, no UnityEngine, no floats, no wall-clock.
public static BattleResult Simulate(
    Squad a, Squad b, WeeklyModifier mod, int seed,
    IReadOnlyDictionary<string, UnitDef> defs);
```

**Determinism test (Milestone 1 acceptance):** running `Simulate` twice with identical inputs yields identical `WinnerSide` and an identical `Log` (deep-equal), asserted in CI.

---

## 13. Build roadmap (milestones)

Each milestone is a natural stopping point; from **M4** onward the game is actually live with your friends.

**M0 — Foundations.** Repo + Unity project. Set up the `Daybreak.Sim` / `Daybreak.Client` assembly split. Create the UGS project, get Authentication working, deploy a "hello world" Cloud Code C# module. *Done when:* you can sign in and call a trivial server module from the app.

**M1 — The sim.** Implement `Daybreak.Sim`: the 12 units, archetype triangle, synergies, tactics, round resolution, event log. Integer math + seeded RNG. Determinism unit tests in CI. A console/test harness that prints a battle blow-by-blow. *Done when:* same seed → identical log, and battles feel sensible on paper.

**M2 — Replay renderer.** Unity scene that takes a `BattleEvent[]` and animates it: sprites on the 2×3 grid, movement, attacks, HP bars, deaths. Feed it sim output locally. *Done when:* you can watch a fight play out and it reads clearly.

**M3 — Squad builder + local practice.** Roster UI, grid placement, tactic picker, LOCK. A "practice" button that runs a local battle vs. a dummy squad and opens the replay. *Done when:* the full single-player loop (build → watch) works offline.

**M4 — Server resolve loop.** Locked squads → Cloud Save. `ResolveDay` Cloud Code module runs the round-robin using the shared sim. Scheduler cron triggers it daily; results stored; Leaderboards updated. *Done when:* you and a friend both lock squads and wake up to correct, watchable results. **The game is now live.**

**M5 — Social + notifications.** Teams, individual + team leaderboards, FCM push on resolve, auto-repeat last squad for no-shows. *Done when:* the group can play as teams and gets pinged when results drop.

**M6 — Rewards & liveops.** Economy currency (participation + win), cosmetic shop, weekly modifier rotation, weekly reset trigger awarding the Dawn Crown + team banner. Polish. *Done when:* a full weekly cycle runs itself end to end.

Post-launch ideas (only after M6): active abilities, a second tag per unit (TFT-style), more units/modifiers, seasonal cosmetics.

---

## 14. Open decisions (get these from the user before M4)

1. **Resolve time & time zone.** One fixed time for everyone (recommended: a fixed UTC hour). What hour?
2. **Group size now / expected.** Confirms round-robin is fine (yes if ≤ ~30) vs. needing brackets later.
3. **Platforms.** iOS, Android, or both? (Affects build setup and push certificates.)
4. **Push via Firebase (FCM).** OK to stand up a small Firebase project alongside UGS just for notifications?
5. **Squad size 5 / grid 2×3 / one tactic per squad** — confirm these feel right, or adjust.
6. **Art.** Who makes sprites? Assume placeholder shapes for the MVP unless told otherwise.
7. **Local practice mode** — keep it? (Recommended yes; great for testing and for onboarding new friends.)

---

## 15. Glossary

- **Ghost battle** — your locked squad fights a stored snapshot of another player's squad; nobody has to be online.
- **Event log** — the ordered list of `BattleEvent`s the sim emits; the client animates it as the replay.
- **Modifier** — the weekly rule twist applied inside the sim at battle start.
- **Sparks** — cosmetic currency (placeholder name).
- **Dawn Crown** — the weekly champion cosmetic; the only non-grindable reward.
- **`Daybreak.Sim`** — the Unity-independent C# library holding the deterministic battle simulation, shared by client and server.
