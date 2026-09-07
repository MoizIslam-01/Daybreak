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

## Phase B — Port screens (next)
Home, Squad builder (→ real daily lock), Leaderboard, Team, Shop, Profile/onboarding.

## Phase C — Android build (later)
Player settings (package name, icon, portrait), notification permission, guard/remove dev buttons.
