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

## Phase C — Android build + hardening (next)
- Player settings: package name, icon, **portrait** orientation, min SDK.
- Notification permission prompt (Android 13+) wired to `NotificationService`.
- Guard/remove dev-only affordances for release: Shop's **Grant Sparks**, the `GrantSparks` and
  `ResetRoster` Cloud Code endpoints, the `New test player` path, and the legacy test scenes.
- Access Control on cross-player Cloud Code endpoints.
- Build an APK and install on a device.
