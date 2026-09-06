# Milestone 6 — Rewards & liveops

**Goal (from the design guide):** a full weekly cycle runs itself end to end.

## Decision

- **Currency & ownership in Cloud Save** (not UGS Economy) — same store as weekly wins and standings.
  One consistent, fully-controllable place; avoids another SDK.

## Phases

1. **Weekly modifier rotation + Sparks currency** ✅ — the week's modifier is applied in the resolve;
   Sparks are earned for showing up and winning.
2. **Cosmetic shop + ownership** ✅ — spend Sparks on title cosmetics; equip them.
3. **Weekly reset + Dawn Crown** — a weekly trigger crowns the #1 player and rolls the week over.

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
