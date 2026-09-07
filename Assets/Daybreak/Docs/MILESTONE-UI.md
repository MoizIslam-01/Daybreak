# Productionization — UI pass

Turning the working systems (M0–M6) into a real phone app. Chosen approach: **procedural uGUI**
(built in code, TextMeshPro text), **functional & clean first**. One `AppShell` component replaces
the scattered one-per-screen test scenes.

## Phase A — App shell + navigation ✅ (code done — setup below)

- `UITheme` — colors and sizes in one place.
- `UIBuilder` — procedural uGUI helpers (phone canvas, panels, TMP labels, buttons, layouts, scroll
  views), so the whole UI lives in scripts.
- `AppShell` — builds a portrait Canvas with a top bar (title + Sparks), a bottom nav
  (Home / Squad / Board / Team / Shop / Me), and a content area that swaps panels. Signs in once,
  schedules the daily reminder, shows Home.
- `AppPanel` base + `StubPanel` — panels are built once and shown/hidden on nav. Phase A ships stubs
  so navigation and layout can be verified; Phase B ports the real screens into them.

### Setup

1. **Import TMP essentials** (one-time): `Window → TextMeshPro → Import TMP Essential Resources`.
   Without this, TMP text renders blank.
2. New scene → **Basic 2D** → save as `Assets/Daybreak/Scenes/Main.unity`.
3. Empty GameObject named `App` → **Add Component → AppShell**.
4. Press **Play**. You should get a dark app with a top bar (title + "Sparks N"), six bottom-nav
   buttons that switch panels and highlight the active one, and each panel showing its name.
5. Set the Game view to a **portrait** aspect (e.g. 1080×1920) to preview phone layout.

Once navigation reads well, Phase B replaces each `StubPanel` with the real screen (porting the
logic from the existing IMGUI screens into `UIBuilder` widgets).

## Phase B — Port screens ✅ (done)

All six tabs are real uGUI panels in the one Main scene:

- **Home** — this week's modifier, your record, last Dawn Crown, and battle rows; **Watch** plays the
  replay in-app (world-space renderer overlaid, aspect-fit camera so it isn't clipped in portrait,
  Back button).
- **Squad** — 2×3 grid placement from the 12-unit roster, tactic picker, live synergy preview, and
  **LOCK SQUAD** wired to the real daily `LockSquad` (earns Sparks).
- **Board** — Individual/Teams standings tabs with names, colors, titles, last champion.
- **Team** — your team, create (name + banner color), joinable list, leave.
- **Shop** — buy/equip title cosmetics with Sparks (+ dev Grant).
- **Me** — name, accent color, badge, live preview, save.

Reusable toolkit: `UITheme`, `UIBuilder` (canvas, panels, TMP labels/buttons, scroll list with
`RectMask2D`, input fields, rows), `AppShell` (nav + panel switching + replay overlay), `AppPanel`.

The old one-per-screen test scenes (Practice, Home, Teams, Leaderboard, Shop, Profile, Replay) are
now redundant for players — keep them as dev harnesses or delete later.

## Phase C — Android build + hardening

### Hardening ✅ (code done)
- Dev-only UI is compiled out of release builds via `#if UNITY_EDITOR || DEVELOPMENT_BUILD`:
  Shop's **Grant Sparks**, and the Me panel's **Reset onboarding** + **Wipe all data**. They show in
  the editor and Development Builds, and vanish from a normal release APK.
- **Unique display names** enforced server-side (`SaveProfile` + name registry).
- Notification runtime permission is now requested (Android 13+).
- Still owed (dashboard): **Access Control** on cross-player endpoints (`WipeAll`, `GrantSparks`,
  `ResetRoster`, `ResolveDay`) so only you/the scheduler can call them.

### Clean-slate reset before launching with friends
Run once from the editor (or a Development Build): **Me → WIPE ALL DATA (dev)**. Clears the shared
registries (roster, teams, champions, name reservations) and your own player data. New friends get
fresh anonymous accounts, so the group starts clean.

### Android build steps (your editor work)
1. `File → Build Profiles → Android → Switch Platform` (installs the Android module if needed).
2. **Player Settings → Player:**
   - **Product Name** Daybreak; set an **icon**.
   - **Other Settings → Package Name:** `com.<you>.daybreak`.
   - **Resolution and Presentation → Default Orientation: Portrait**.
   - **Other Settings → Minimum API Level:** Android 8 (API 26) or higher.
   - **Scripting Backend: IL2CPP**, **Target Architectures: ARM64**.
3. **Build Profiles**: leave **Development Build unticked** for the real friend build (dev buttons
   disappear); tick it for a test build that keeps them.
4. Ensure **Main** is the only scene in the Scene List (index 0).
5. **Build** the APK (default debug signing is fine for sideloading to friends).
6. Redeploy the Cloud Code module first if you haven't (it has new `SaveProfile` / `WipeAll`).

**Done when** you can install the APK, onboard, lock a squad, and see results the next day.
