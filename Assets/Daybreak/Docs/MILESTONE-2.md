# Milestone 2 — Replay renderer

**Goal (from the design guide):** you can watch a fight play out and it reads clearly.

All the rendering is generated in code — placeholder art is colored squares, so there are **no
sprites to import and no prefabs to wire**. You add one component to one scene and press Play.

---

## What's in the repo

```
Assets/Daybreak/Client/Replay/
  ReplayArt.cs     Placeholder art: code-generated squares, archetype/tag/hit colors
  ReplayText.cs    Built-in font lookup + floating damage numbers
  UnitView.cs      One unit's visual — body, tag pip, HP bar, name label, lunge/flash/die
  ReplayPlayer.cs  Consumes BattleEvent[], drives the views on two 2x3 grids. No game logic.
  ReplayDemo.cs    The one component you add: builds squads, runs the sim, feeds ReplayPlayer
```

The split matters: `ReplayPlayer` only ever *replays what the sim already decided*. It reads the
event log and moves squares; it never computes an outcome. That's the same boundary the design
depends on — the client renders, the sim (and later the server) decides.

## Setup (about 3 minutes)

1. `File → New Scene` → **Basic 2D** → save as `Assets/Daybreak/Scenes/Replay.unity`.
2. In the Hierarchy: right-click → **Create Empty**, name it `Replay`.
3. With it selected, **Add Component → ReplayDemo**. (A `ReplayPlayer` is added automatically.)
4. Press **Play**.

You should see two teams of colored squares face off, lunge at each other, HP bars drain, floating
damage numbers, and units grey out as they die. Top-left shows the round and, at the end, the
winner. Buttons: **Replay**, and speed **1x / 2x / 4x**.

That's the milestone: a fight you can watch that reads clearly.

## Reading the visuals

- **Body color = archetype:** Vanguard blue, Skirmisher red, Marksman green.
- **Corner pip = synergy tag:** Guardian pale-blue, Pack orange, Arcane purple, Swift yellow.
- **Damage number color = matchup:** orange = favorable (×1.25), grey = unfavorable (×0.80),
  white = neutral.
- **Layout:** each side is a 2×3 grid with the front row nearer the centre, mirrored — exactly the
  grid the squad builder will use in M3.

## Tuning the demo

Select the `Replay` object and edit the `ReplayDemo` fields in the Inspector: the seed, each side's
tactic, and the five unit ids per side (from `Resources/units.json`). Press Play to see a different
fight. Timing is on `ReplayPlayer` (lunge/hit/round durations) if the pacing feels off.

## How M3 and M4 reuse this

`ReplayPlayer.Play(a, b, modifier, result, defs)` is the whole public surface. M3 will call it with
the squad you built and a locally-run practice battle; M4 will call it with a squad snapshot and the
seed the *server* resolved, regenerating the identical log on the client. The renderer doesn't know
or care which — it just animates a `BattleResult`.

## Known placeholder limitations (fine for now)

- HP bars use each unit's **base** HP as the full-bar baseline. Under weekly modifiers that change
  HP (Glass Cannons, Vanguard's Hour) the bar can empty slightly early or late — but the **Death**
  event from the log is authoritative, so deaths always land correctly. The demo uses no modifier,
  so bars are exact there.
- No attack-direction easing or camera shake — deliberately minimal until the loop is fun.
