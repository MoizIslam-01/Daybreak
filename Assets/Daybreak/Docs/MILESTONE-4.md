# Milestone 4 — Server resolve loop (the game goes live)

**Goal (from the design guide):** you and a friend both lock squads and wake up to correct,
watchable results.

## Decisions (locked with the user)

- **Resolve time:** 20:00 UTC daily. Scheduler cron `0 20 * * *`.
- **Group size:** 10–30 players → round-robin is fine (each player fights N−1 battles/day).
- **Platform:** Android first.
- Squad model (5 units, 2×3 grid, one tactic) — confirmed by playing M3.

## Why this is staged

M4 is the biggest milestone: it spans pure logic, server IO, and dashboard config. The core
*resolution logic* is Unity- and UGS-independent, so it's built and tested first; the UGS plumbing
wraps it.

## Phase 1 — Resolution core ✅ (done, in `Daybreak.Sim`)

- `DailyResolver.ResolveDay(day, players, modifier, defs)` — the round-robin, as a pure function.
  Every player's squad fights every other once; per-battle seed is `deterministicSeed(day, idA, idB)`,
  so results are reproducible anywhere.
- Result types: `PlayerEntry`, `PlayerDayResult` (wins/losses, HP tiebreak, battle records),
  `BattleRecord` (opponentId, won, seed) — the replay is just `(opponent, seed)`.
- `BattleResult.WinnerRemainingHp` added for the weekly HP tiebreak.
- Tests: round-robin correctness, win/loss conservation, pairwise consistency, full determinism,
  and — importantly — that a stored seed regenerates the identical outcome (proving replays work
  from a few bytes).

The Cloud Code module and the client both call this exact function. The server only adds IO.

## Phase 2 — Serialization + client Cloud Save ✅ (done)

- `SquadDto` / `DayResultDto` + `SquadCodec` in the sim — the shared wire format. A test proves a
  squad round-trips and still produces the identical battle.
- `GameCalendar` — one agreed day/week definition with the 20:00 UTC boundary, pure and tested.
- `DataService` (client) — save the locked squad, read your own day result (guarded stub until the
  Cloud Save package is confirmed).

## Phase 3 — Server `ResolveDay` module ✅ (code done — deploy + test below)

- `CloudCode/Daybreak/DaybreakModule.cs` gains two endpoints (plus the M0 `SayHello`):
  - **`LockSquad(squadJson)`** — validates against the shared rules, stores the player's own locked
    squad, and registers them in a game-wide roster.
  - **`ResolveDay()`** — reads the roster, cross-player-reads each locked squad, calls the tested
    `DailyResolver.ResolveDay`, and cross-player-writes each player's result. Only players who
    locked *for today* are included.
- Client hooks: `CloudCodeService.LockSquadAsync` / `ResolveDayAsync`, plus a **SERVER (dev)** panel
  in the Practice builder (Lock to server / Resolve now / My result) to exercise the round trip.

### Deploy & test Phase 3

1. **Enable Cloud Save** on the dashboard: **LiveOps → Cloud Save** (accept the enable prompt).
2. **Redeploy the module** — `Window → Deployment → Deploy` (it now uses the cross-player APIs).
3. In the Practice scene, build a valid squad → **Lock to server**. Expect "Locked for day N".
4. **Resolve now** with only yourself → "1 players, 0 battles" (round-robin needs 2+). That still
   proves the pipe. For a real battle, have a friend (or a second anonymous sign-in) lock too, then
   Resolve now → "2 players, 1 battle", and **My result** shows your record.

> Note: `ResolveDay` and `LockSquad` touch cross-player data. For a friend group this is fine, but
> before any wider release, restrict them with Cloud Code **Access Control** (see the module docs).

## Phase 4 — Scheduler + Leaderboards (dashboard)

- Scheduler cron `0 20 * * *` → Trigger → `ResolveDay`.
- Individual weekly Leaderboard (team leaderboard is M5).
- A manual "resolve now" path for testing without waiting for 20:00 UTC.

## Phase 5 — Home screen

- Show yesterday's record ("You went 4–1") and let you tap a battle to watch the replay —
  regenerated on the client from the stored `(opponent, seed)` through the M2 `ReplayPlayer`.

**Done when** two players lock and wake to correct, watchable results.
