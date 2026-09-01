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

## Phase 2 — Serialization + client Cloud Save (next)

- JSON DTOs for a locked squad and a day result (shared shape, client writes / server reads).
- `CloudSaveService` (client): write today's locked squad; read yesterday's result.
- A "Lock for today" action in the client that submits the built squad.

## Phase 3 — Server `ResolveDay` module

- Extend the Cloud Code module: load every active player's locked squad, call
  `DailyResolver.ResolveDay`, write each player's result back, update the Leaderboard. Idempotent
  (triggers can fire more than once).
- Active-player registry: a small game-scoped list each lock appends to, so the resolver knows who
  to include.

## Phase 4 — Scheduler + Leaderboards (dashboard)

- Scheduler cron `0 20 * * *` → Trigger → `ResolveDay`.
- Individual weekly Leaderboard (team leaderboard is M5).
- A manual "resolve now" path for testing without waiting for 20:00 UTC.

## Phase 5 — Home screen

- Show yesterday's record ("You went 4–1") and let you tap a battle to watch the replay —
  regenerated on the client from the stored `(opponent, seed)` through the M2 `ReplayPlayer`.

**Done when** two players lock and wake to correct, watchable results.
