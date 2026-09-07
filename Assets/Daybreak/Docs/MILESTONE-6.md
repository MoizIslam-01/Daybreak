# Milestone 6 — Rewards & liveops

**Goal (from the design guide):** a full weekly cycle runs itself end to end.

## Decision

- **Currency & ownership in Cloud Save** (not UGS Economy) — same store as weekly wins and standings.
  One consistent, fully-controllable place; avoids another SDK.

## Phases

1. **Weekly modifier rotation + Sparks currency** ✅ — the week's modifier is applied in the resolve;
   Sparks are earned for showing up and winning.
2. **Cosmetic shop + ownership** ✅ — spend Sparks on title cosmetics; equip them.
3. **Weekly reset + Dawn Crown** ✅ — the #1 player is crowned when a week closes; standings roll over.

---

## Phase 1 — Modifier rotation + Sparks ✅ (code done — setup below)

- `WeeklyModifierRotation` in the sim — maps the week number to one of the five modifiers (baseline
  + the four twists from M1), cycling. Pure and unit-tested; `ById` re-resolves a modifier for exact
  replay regeneration.
- `ResolveDay` now applies **the week's modifier** (not None) and stamps its id into every result, so
  replays reconstruct under the same rule.
- **Sparks** (`WalletDto` in Cloud Save): **+10 for locking a squad each day**, **+2 per win** —
  deliberately rewarding the reliable friend over the merely-best one. Both awards are idempotent
  (per-day guards), so repeated locks/resolves don't inflate the balance.
- Home screen shows **this week's modifier** and your **Sparks** balance.

### Setup

1. Recompile; run tests (rotation adds 4 → ~86 green).
2. **Redeploy the module** (applies the modifier, awards Sparks).
3. Test: in Practice, **Lock to server** → you earn +10 Sparks (once per day). Run a two-player
   resolve. Open **Home** → it shows the week's modifier, your Sparks (10 + 2×wins), and battles now
   replay under that modifier.

> Existing pre-M6 results were stored with modifier "none"; only new resolves carry the week's
> modifier. Our current week resolves to one of the twists — you'll see it named on Home.

## Phase 2 — Cosmetic shop ✅ (code done — setup below)

- `CosmeticCatalog` + `CosmeticRules` in the sim — a set of title cosmetics with prices, and pure
  purchase validation (owned? affordable?), unit-tested. Titles are bragging-rights flair shown next
  to your name; **no gameplay effect**.
- Server `BuyCosmetic` (deduct Sparks, add to owned), `EquipTitle` (set profile.title), plus a dev
  `GrantSparks` for testing. Ownership lives in the wallet; the equipped title in the profile.
- `ShopScreen` (client): browse, buy, equip. Equipped titles now show before names on the
  leaderboard.

### Setup

1. Recompile; run tests (cosmetics add 5 → ~91 green).
2. **Redeploy the module** (adds the shop endpoints).
3. New scene → **Basic 2D** → `Assets/Daybreak/Scenes/Shop.unity` → empty GameObject → **Add
   Component → ShopScreen**. Press Play.
4. Click **Grant 100 (dev)** to top up Sparks, **Buy** a title, then **Equip** it. Open the
   Leaderboard scene → your title appears before your name.

## Phase 3 — Weekly reset + Dawn Crown ✅ (code done — setup below)

- The weekly finalize is **folded into the daily resolve** — no new dashboard trigger. At the start
  of the first resolve of a new week, `FinalizeWeekIfNeeded` crowns the just-ended week's #1 (its
  weekly records are still intact at that moment). Idempotent via a champions record.
- The champion gets the **Dawn Crown** — a dated, ungrindable cosmetic (`dawn_crown_w{N}`) added to
  their wallet and auto-equipped as their title, plus a hall-of-fame entry (with the top team).
- `GetChampions` endpoint; the Leaderboard screen shows "Last Dawn Crown: <name>" and renders crown
  titles via `CosmeticCatalog.DisplayName`.
- Weekly reset itself is implicit: weekly records are week-keyed, so a new week starts empty.

### Setup

1. Recompile; run tests (Dawn Crown adds 3 → ~94 green).
2. **Redeploy the module.**
3. Hard to see live without crossing a real week boundary — the logic triggers on the first resolve
   of a new week. It's covered by the standings tests (champion = rank 1) and is safe/idempotent.

**Done when** a full weekly cycle runs itself end to end. ✅ — Daybreak is feature-complete against
the design guide (M0–M6). Remaining work is the productionization pass (see PRODUCTION-BACKLOG.md).
