# Build-readiness backlog (next session)

The game's systems (M0–M6.2) are complete and verified, but everything currently lives in separate
one-component IMGUI test scenes. This pass turns it into a real app you can put on a phone.

## 1. Navigation flow
- A single app shell with a bottom nav or hub: **Home ▸ Squad ▸ Leaderboard ▸ Team ▸ Shop ▸ Profile**.
- One persistent Boot → sign-in → main flow, instead of each scene signing in on its own.
- Back-button handling (Android hardware back).

## 2. Real UI (replace IMGUI)
- Rebuild the screens in uGUI or UI Toolkit with proper layout, fonts (TextMeshPro), and touch targets.
- The logic is already separated from the IMGUI drawing (services + sim), so this is a view-layer swap:
  `ProfileScreen`, `TeamScreen`, `LeaderboardScreen`, `ShopScreen`, `HomeScreen`, `PracticeMode`.
- Real cosmetics rendering (color emoji, banners, frames) once on TMP.

## 3. Scene hierarchy
- Consolidate the test scenes into: **Boot**, **Main** (hub + panels), and keep **Replay** as an
  overlay/child driven by `ReplayPlayer`.
- Move shared services (Auth, Data, Config) onto a persistent bootstrap object.

## 4. Squad builder → daily lock
- Wire the M3 builder to the real **LockSquad** flow (it currently locks via the dev panel).
- Show lock deadline (20:00 UTC) and "locked / auto-repeat" state on Home.

## 5. Onboarding
- First-run: set profile (name/color), optional team join, a practice battle. Fold `ProfileScreen`
  into this instead of a standalone scene.

## 6. Android build setup
- Player settings: package name, icon, min SDK, portrait orientation.
- Notification permission prompt (Android 13+) wired to `NotificationService`.
- A signed build config for sharing with friends.

## 7. Polish / hardening
- Loading + error states on every network call (currently status strings).
- Access Control on the cross-player Cloud Code endpoints (ResolveDay, GrantSparks is dev-only).
- Remove/guard dev buttons (New test player, Reset roster, Grant Sparks) for release builds.

## Still owed in the roadmap
- **M6 Phase 3** — weekly reset + Dawn Crown (weekly trigger; crown #1, top-team banner, roll week).

None of this touches `Daybreak.Sim` — the tested core stays as-is; this is all client shell + build config.
