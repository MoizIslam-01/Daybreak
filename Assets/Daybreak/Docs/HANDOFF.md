# Daybreak — session handoff

Quick-start context for a new Cowork chat. Read this first, then the milestone docs.

## What Daybreak is
Mobile async auto-battler in Unity + Unity Gaming Services. Set a squad once a day, the server
auto-resolves everyone's battles overnight (20:00 UTC), you watch replays and argue in the group
chat. Design source of truth: `Assets/daybreak-game-design-and-build-guide.md`.

## Status: feature-complete + Android build made
- **M0–M6 done** (foundations, deterministic sim, replay renderer, squad builder, server resolve
  loop, social/teams/leaderboards/notifications, rewards/shop/Dawn Crown). See `Docs/MILESTONE-*.md`.
- **UI pass done** — one `Main` scene, procedural uGUI app shell with 6 tabs (Home, Squad, Board,
  Team, Shop, Me) + onboarding. See `Docs/MILESTONE-UI.md`.
- **Installable Android APK built.** Dev buttons compile out of release builds
  (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`).
- ~94 EditMode tests green. Sim is Unity-free (`Daybreak.Sim`), shared by client and Cloud Code.

## Architecture (don't break)
- `Assets/Daybreak/Sim` — pure deterministic sim + rules + DTOs, NO UnityEngine. Unit-tested.
- `Assets/Daybreak/Client` — Unity client: `Services/`, `UI/` (AppShell + panels), Replay, etc.
- `CloudCode/Daybreak` — Cloud Code C# module; compiles the same Sim source; endpoints:
  LockSquad, ResolveDay, GetStandings, GetChampions, SaveProfile, Create/Join/Leave/ListTeams,
  BuyCosmetic, EquipTitle, plus dev: GrantSparks, ResetRoster, WipeAll.
- Storage is Cloud Save (player data + game-data registries), not UGS Economy/Leaderboards service.

## Working style that worked
- No git pushes/commits by the AI — the user commits manually; AI suggests messages.
- Unity MCP connection is flaky ("revoked"/"capacity limit"); default to the user screenshotting.
- Logic in the sim (testable), client/server are thin. Redeploy the Cloud Code module after server
  changes (`Window → Deployment → Deploy`).

## Open / next
- **[TOP PRIORITY] Leaderboard weekly-reset gap bug.** Playtest finding: the morning after a resolve
  the Individual board showed 0W for everyone, while Home still showed yesterday's battles/replays
  (the team board looked correct — unconfirmed, needs checking against actual Cloud Save data).
  Cause: `GetStandings` reads each player's *current-week* `weekly` record (keyed by week number);
  when the week rolls over, those are empty until that night's resolve repopulates them, so the
  board shows an all-zero gap. Home is unaffected (separate per-day `dayResult`).
  Fix intent: standings should fall back to the most recent week that actually has results (don't
  show an all-zero board during the reset window). Look at `GetStandings` in
  `CloudCode/Daybreak/DaybreakModule.cs`, the `weekly` WeeklyRecord logic, and
  `StandingsCalculator` in the sim. Redeploy the module after. Consider verifying the individual-vs-
  team discrepancy by inspecting a player's `weekly` value in Player Management first.
- **Account linking** (Unity Player Accounts or Google Play Games) so accounts survive reinstall —
  anonymous auth loses the account (and leaves its name reserved) on uninstall.
- Dashboard: Access Control on cross-player endpoints (WipeAll, GrantSparks, ResetRoster, ResolveDay).
- Polish: real sprites/UI art, vertical (portrait) replay layout, app-store polish.
- Before friends test: redeploy module, run Me → WIPE ALL DATA (dev), build with Development Build
  OFF.

## Locked decisions
- Resolve time: **20:00 UTC** (= 1 AM the user's local; kept intentionally). ~10-30 players.
  Android first. No Firebase — local notifications. Currency/leaderboards in Cloud Save, not UGS
  Economy/Leaderboards service.

## Known-good playtested behaviour
- Daily auto-resolve fires (at 20:00 UTC), battles + Home replays + records all work.
- Squad panel now loads the existing locked squad on open (fixed).
