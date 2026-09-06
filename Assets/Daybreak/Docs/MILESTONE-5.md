# Milestone 5 — Social + notifications

**Goal (from the design guide):** the group can play as teams and gets pinged when results drop.

## Decisions (locked with the user)

- **Notifications:** local device notifications (Unity Mobile Notifications), a daily reminder just
  after 20:00 UTC. **No Firebase** — the resolve time is fixed, so a scheduled local notification
  fits and drops the whole push-server dependency.
- **Profiles:** display name **+ accent color + badge glyph**.
- **Teams:** included in M5 (create/join, individual scores roll up to a team score, team board).

## Phases

1. **Profiles** ✅ — set your name/color/badge; names shown in results.
2. **Teams** — create/join a team; team score = sum of members' wins; team leaderboard.
3. **Leaderboards wired** — individual + team boards actually populated (pins the Leaderboards SDK
   version that M4 deferred).
4. **Notifications + auto-repeat** — local daily reminder; no-shows auto-repeat their last squad.

---

## Phase 1 — Profiles ✅ (code done — setup below)

- `ProfileDto` + `ProfileRules` in the sim (name sanitize/clamp, hex-color validation), unit-tested.
- `DataService.SaveProfileAsync` / `LoadProfileAsync` — your profile in your own Cloud Save.
- `ProfileScreen` (client): set display name, pick an accent color and a badge, live preview, save.
- `ResolveDay` now snapshots each player's name + color into the results, so the home screen shows
  "**Sam** went 4-1" and "WON vs **Alex**" in their colors instead of raw ids.

### Setup

1. Recompile Unity; run tests (Profile rules add 5 → ~75 green).
2. **Redeploy the module** (`Window → Deployment → Deploy`) — results now carry names/colors.
3. New scene → **Basic 2D** → `Assets/Daybreak/Scenes/Profile.unity` → empty GameObject → **Add
   Component → ProfileScreen**. Press Play, set your name/color/badge, **Save profile**.
4. To see names in results: in Practice, set a profile (via the Profile scene) for a player, lock;
   New test player, set their profile, lock; **Resolve now**; open **Home** → names appear.

> Profiles are set in their own scene for now; the polished app will fold this into onboarding.

## Phase 2 — Teams ✅ (code done — setup below)

- `TeamDto` + `TeamRules` in the sim (name sanitize, URL-safe slug id), unit-tested.
- Server endpoints: `CreateTeam`, `JoinTeam`, `LeaveTeam`, `ListTeams`. Teams live in game data;
  a player belongs to one team at a time, and their team id mirrors into their profile.
- `TeamScreen` (client): shows your team, create a team (name + banner color), and a joinable list.

### Setup

1. Recompile; run tests (team rules add 3 → ~78 green).
2. **Redeploy the module** (`Window → Deployment → Deploy`) — adds the team endpoints.
3. New scene → **Basic 2D** → `Assets/Daybreak/Scenes/Teams.unity` → empty GameObject → **Add
   Component → TeamScreen**. Press Play.
4. Create a team (name + color). Then **New test player** isn't here — to test joining, use a second
   account: the Practice scene's "New test player" button switches accounts, then come back to the
   Teams scene (same resumed account) and Join. Or just create one team and confirm "Your team"
   updates and the member count shows 1.

> `ListTeams` returns a JSON string (parsed client-side) because the field-based DTOs don't survive
> the Cloud Code framework's property-only return serialization — same reason the Cloud Save values
> are JSON strings.
