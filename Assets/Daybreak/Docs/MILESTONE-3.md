# Milestone 3 — Squad builder + local practice

**Goal (from the design guide):** the full single-player loop — build → watch — works offline.

One component, one scene. The builder UI is IMGUI (no Canvas, no prefabs), and Practice hands off
to the M2 renderer, so there's nothing to wire.

---

## What's in the repo

```
Assets/Daybreak/Sim/Core/SquadDraft.cs      Pure 2x3 draft state + placement rules (unit-tested)
Assets/Daybreak/Client/Practice/
  DummyOpponents.cs   Four preset opponent squads to practice against
  PracticeMode.cs     The builder UI + the local battle flow (the one component you add)
```

`SquadDraft` lives in the sim because "what makes a legal squad" is a game rule, not a UI detail —
so it's tested alongside the rest of the sim, and the builder is just a view onto it.

## Setup (about 2 minutes)

1. `File → New Scene` → **Basic 2D** → save as `Assets/Daybreak/Scenes/Practice.unity`.
2. Create an empty GameObject, name it `Practice`.
3. **Add Component → PracticeMode** (a `ReplayPlayer` is added automatically).
4. Press **Play**.

## Using it

- **Roster (left):** click a unit to add it; click it again to remove it. Placed units are green.
- **Grid (middle):** front row takes hits, back row is protected. Click an empty cell to target it,
  then click a unit to drop it there; click a filled cell to clear it. If you don't pick a cell
  first, units fill the next empty slot.
- **Tactic:** pick one of the four.
- **Synergies (right):** updates live as you build — shows which tag bonuses are active.
- **Opponent:** cycle through four preset enemy squads with **Change**.
- **PRACTICE BATTLE:** enabled once five units are placed. Runs the sim locally and drops you into
  the replay. Use the speed buttons; **< Back to builder** returns you to tweak and try again.

That's the milestone: build a squad, watch it fight, adjust, repeat — all offline, no server.

## How M4 reuses this

The builder produces a validated `Squad` via `SquadDraft.TryBuild`. M4 will take that same squad,
write it to Cloud Save as your locked entry for the day, and the server will fight it against
everyone else's — regenerating replays through the very same `ReplayPlayer`. Practice mode stays as
the offline sandbox (and the onboarding for new friends).

## Notes

- The opponent seed varies each Practice, so repeated fights differ — but any single result is still
  fully deterministic (same squads + seed → same battle), which is what makes the stored replay work.
- Still placeholder art and IMGUI — the loop comes first; polish comes after it's fun.
