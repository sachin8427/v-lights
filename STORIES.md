# V-LIGHTS: Phoenix — Story Backlog (v2, post-pivot)

**This replaces the prior 3D open-world backlog.** That direction (real Phoenix geography, OSM/LiDAR, multi-zone world) is parked — branches remain untouched but inactive. This is the active plan: a faithful Unity 2D URP port of `vlights_unity_starter.zip`, itself a port of the working browser concept `Vlightv2_2.html`.

## How to use this file

Each story is independently executable in a fresh Claude Code session with **zero prior conversation context**. Before starting any story:

1. Read `CLAUDE.md` for project context (update it to reflect this pivot if it still describes the old 3D world)
2. Read this story's entry below
3. Create the branch named in the story
4. Implement per scope — respect the out-of-scope list
5. Verify against acceptance criteria independently (run it, don't just narrate it)
6. Log verification evidence in `FEEDBACK.md`
7. Report back for review/merge — do not merge to main yourself

Reference material for every story: `unity_starter/README.md`, `Scripts/*.cs`, `Data/specimen_catalog.json`, `Data/expeditions.json` — all already provided in the starter kit. The scripts are a real, mostly-complete implementation (757 lines, only minor "juice" polish TODOs: camera shake, particle bursts, boost visuals) — this is porting/wiring/completing, not writing from scratch.

**Confirmed decisions from the starter kit (adopted, superseding earlier locks):**
- Sedona is IN scope as Expedition 3 (previously excluded — now allowed)
- IAP is 3 non-consumables: `vlights_rainbow_lights`, `vlights_golden_beam`, `vlights_unlock_all` (replaces the earlier single `disco_saucer` skin)
- Field Guide collection meta-game: kept for v1
- Wanted-level difficulty scaling: kept for v1
- Still out of scope: boss fight ("Mothership" finale), Game Center leaderboards — both listed only as stretch goals in the kit, not being built now

---

## EPIC 1 — Project Setup & Scene Foundation

### Story 1.1 — Unity project setup
**Branch:** `feature/setup-2d-urp-project`

**Scope:**
- New Unity project, 2D (URP) template
- Install In App Purchasing package (`com.unity.purchasing` 4.10+)
- Set Bundle Identifier
- Copy `Scripts/` → `Assets/Scripts/`, `Data/*.json` → `Assets/Data/`, assign both JSON files as TextAssets in the Inspector
- Confirm all 12 scripts compile with no errors (they reference each other — GameManager, PlayerController, SamplingBeam, Specimen, Hazard, Spawner, ParallaxLayer, IAPManager, HUDController, SaveData, SpecimenData, ExpeditionData)

**Out of scope:** Any art, scene hierarchy, or gameplay wiring — this is project scaffolding only.

**Acceptance criteria:**
- [ ] Project opens with zero compile errors
- [ ] Both JSON files load without parse errors (write a quick test call to confirm)

---

### Story 1.2 — Scene hierarchy + placeholder art
**Branch:** `feature/scene-hierarchy`

**Scope:** Build the "Main" scene exactly per README section 2:
- Main Camera (Orthographic, Size 5, z=-10)
- BackgroundFar / BackgroundMid (SpriteRenderer + ParallaxLayer, scrollFactor 0.15 / 0.4)
- Ground (dark strip)
- Player (PlayerController + SamplingBeam child "Beam" + ship SpriteRenderer)
- Spawner, IAP (DontDestroyOnLoad), Canvas (HUDController), Panels (TitlePanel, GameOverPanel, FieldGuidePanel, ShopPanel)
- Specimen prefab: SpriteRenderer + CircleCollider2D(isTrigger) + Specimen script
- Hazard prefabs (Chopper/Jet/Balloon): SpriteRenderer + BoxCollider2D(isTrigger) + Hazard script
- Use placeholder art (colored capsules/boxes) for everything — real art comes later, per README's own guidance

**Out of scope:** Final art, animations, polish.

**Depends on:** 1.1.

**Acceptance criteria:**
- [ ] All GameObjects from README section 2 exist with correct components attached
- [ ] Specimen and Hazard prefabs created and assignable in Spawner's Inspector fields
- [ ] Scene runs in Play mode with no null-reference errors (even if nothing moves yet)

---

## EPIC 2 — Core Loop (README build order 1–3)

### Story 2.1 — Player drag + parallax scroll
**Branch:** `feature/core-player-movement`

**Scope:** `GameManager.StartExpedition(1)` triggers on tap; PlayerController drag-to-fly works; ParallaxLayer scrolls background at the two configured factors.

**Depends on:** 1.2.

**Acceptance criteria:**
- [ ] Tapping starts Expedition 1 and background begins scrolling at `scrollSpeed` from `expeditions.json`
- [ ] Player responds to drag input, moves smoothly
- [ ] Confirmed by actually playing it, not just code review

---

### Story 2.2 — Beam + specimen capture (the core loop)
**Branch:** `feature/core-sampling-beam`

**Scope:** SamplingBeam cone visual + capture zone works; Specimen rise/wobble/capture behavior functions per `beamTime` values in the catalog; score/streak updates on capture.

**Depends on:** 2.1.

**Acceptance criteria:**
- [ ] Beam visually activates on input and can capture a specimen within its `beamTime`
- [ ] Score and streak counters update correctly and visibly on capture
- [ ] At least 2–3 different specimens from `specimen_catalog.json` spawn and are capturable in Expedition 1

---

### Story 2.3 — Hazards + hull + game over
**Branch:** `feature/core-hazards`

**Scope:** Hazard movement patterns (Chopper first — slow patrol + sine hover, per Hazard.cs) function; hull/health decreases on hazard contact; game-over state triggers correctly at zero hull.

**Depends on:** 2.2.

**Acceptance criteria:**
- [ ] Chopper hazard spawns per `hazardInterval` and moves per its defined pattern
- [ ] Player hull decreases on hazard contact (verify visually or via HUD)
- [ ] Game-over panel appears when hull reaches zero, confirmed by actually losing

---

## EPIC 3 — Progression & Meta

### Story 3.1 — Wanted-level scaling
**Branch:** `feature/wanted-level`

**Scope:** Confirm/wire the wanted-level difficulty scaling already present in GameManager/Spawner (per README, "already in GameManager/Spawner" — verify and finish wiring, don't rebuild from scratch).

**Depends on:** 2.3.

**Acceptance criteria:**
- [ ] Difficulty (spawn rate, hazard frequency) visibly escalates the longer a run continues, confirmed by playtest

---

### Story 3.2 — HUD + Title/GameOver panels
**Branch:** `feature/hud-panels`

**Scope:** HUDController labels (specimen/streak/score/hull/wanted) display correctly; Beam + Boost buttons functional; TitlePanel and GameOverPanel show/hide correctly via `GameManager.State`.

**Depends on:** 3.1.

**Acceptance criteria:**
- [ ] All HUD labels update live during play
- [ ] Title → gameplay → game-over → title loop works without stuck states

---

### Story 3.3 — Field Guide panel
**Branch:** `feature/field-guide`

**Scope:** Grid view built from `specimen_catalog.json`; grey silhouette shown until a specimen has been captured at least once (persisted via SaveData).

**Depends on:** 3.2.

**Acceptance criteria:**
- [ ] Field Guide shows all specimens from the catalog, correctly greyed-out vs. revealed based on capture history
- [ ] Capture state persists across app restarts (SaveData/PlayerPrefs)

---

## EPIC 4 — Monetization

### Story 4.1 — IAP wiring
**Branch:** `feature/iap-wiring`

**Scope:** Wire ShopPanel buttons → `IAPManager.Buy(id)` for all 3 non-consumables (`vlights_rainbow_lights`, `vlights_golden_beam`, `vlights_unlock_all`). Rainbow skin tints beam + edge lights per IAPManager's existing hooks. Restore Purchases button included (required for App Store review).

**Depends on:** 3.3 (needs Field Guide + core loop stable first, though technically parallelizable once ShopPanel UI exists).

**Acceptance criteria:**
- [ ] All 3 purchases complete successfully in RevenueCat/App Store sandbox
- [ ] Restore Purchases correctly restores prior purchases
- [ ] Rainbow skin visually changes beam + edge lights when owned

**External dependency (not code, do in parallel):** App Store Connect — create 3 Non-Consumables with the exact IDs above; add a Sandbox tester Apple ID.

---

## EPIC 5 — Expedition Content (parallelizable once core loop works)

Each expedition beyond #1 is mostly data-driven (already defined in `expeditions.json`) plus any unique background art. These can run in parallel once Epic 2 is solid.

### Story 5.1 — Expedition 2: Camelback Foothills
**Branch:** `feature/expedition-2-camelback`
**Depends on:** 2.3.
**Acceptance:** Playable end-to-end, correct specimens/hazards spawn per `expeditions.json` config, quota of 8 achievable.

### Story 5.2 — Expedition 3: Sedona
**Branch:** `feature/expedition-3-sedona`
**Depends on:** 2.3.
**Acceptance:** Same pattern — Jet hazard introduced here for the first time, confirm its movement pattern is distinct from Chopper.

### Story 5.3 — Expedition 4: Deep Desert Night
**Branch:** `feature/expedition-4-deep-desert`
**Depends on:** 2.3.
**Acceptance:** Balloon hazard combined with Chopper + Jet; Legendary specimen "Lost Dutchman's Gold" (low weight 0.25) confirmed spawnable, not just theoretically in the table.

### Story 5.4 — Expedition 5: The Lights Return (finale)
**Branch:** `feature/expedition-5-finale`
**Depends on:** 2.3.
**Acceptance:** Highest difficulty config confirmed working; "The Light" legendary specimen (weight 0.2) capturable, completing the Field Guide's intended narrative arc.

---

## EPIC 6 — Polish & Ship (expand later)

Not yet broken into stories. Covers: iOS build/Xcode signing, TestFlight distribution, App Store listing, audio (beam hum, capture chime — README notes WebAudio logic from the concept ports directly), and any "juice" TODOs left in the scripts (camera shake, particle bursts, boost visuals).

---

## Recommended order

1.1 → 1.2 → 2.1 → 2.2 → 2.3 → [3.1, 3.2, 3.3 roughly in order] → 4.1 (parallel with App Store Connect setup) → [5.1, 5.2, 5.3, 5.4 in any order/interleaved] → Epic 6.
