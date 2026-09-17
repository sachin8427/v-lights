# CLAUDE.md — V-LIGHTS: Phoenix

This is the persistent context file for this project. Read this first in every session, before touching code. It should always answer "what is this and what are the rules" without needing prior conversation history.

## What this is

A UFO game for iOS, inspired by the real March 13, 1997 Phoenix Lights event in Arizona. The player flies a researcher-themed UFO across Arizona locations, using a **sampling beam** to capture "specimens" (wildlife, vehicles, local oddities — each with deadpan alien-researcher field notes) while dodging escalating hazards, building toward the final specimen: **The Light** itself.

Tone: funny, deadpan, warm — not grim sci-fi horror abduction. The player is a researcher cataloguing a strange, beautiful desert, not a threat.

## Project history — read this to avoid repeating mistakes

This is the **second attempt**. The first attempt (a separate, untouched repo) spent significant time building a real-Phoenix-geography 3D open world — real lat/long coordinates, OpenStreetMap building data, LiDAR height data, purchased 3D terrain assets. That work is technically sound and preserved on parked branches, but it **never produced a playable game** — even a visible player craft wasn't finished before the pivot. The lesson: real-world geographic accuracy was solving a problem nobody asked for, at the cost of actually shipping gameplay.

**This project exists specifically to avoid that failure mode.** If a task starts trending toward "let's make this more geographically/technically accurate" instead of "let's make this playable and fun," stop and flag it rather than continuing.

## Tech stack

- **Unity 2D (URP)** — not 3D. This is a 2D sprite-based game, orthographic camera.
- **In App Purchasing** package (`com.unity.purchasing` 4.10+) for IAP
- **RevenueCat** for entitlement management, **App Store Connect** for iOS distribution
- Target platform: iOS (Android as a possible later stretch, not now)

## Source of truth for design

The original design spec is a working browser prototype: `Vlightv2_2.html` (React + Canvas). This project is a **faithful port** of that concept, using a pre-built starter kit as the implementation base — not a reinterpretation. When in doubt about intended behavior, the starter kit's scripts and data files are the spec.

## Project structure

```
Assets/
  Scripts/          — 12 C# scripts (GameManager, PlayerController, SamplingBeam,
                       Specimen, Hazard, Spawner, ParallaxLayer, IAPManager,
                       HUDController, SaveData, SpecimenData, ExpeditionData)
  Data/              — specimen_catalog.json, expeditions.json (TextAssets)
```

The scripts are a real, largely-complete implementation (~757 lines total) — only minor "juice" polish TODOs remain (camera shake, particle bursts, boost visuals). Treat this as **porting and finishing**, not writing from scratch. Don't rewrite working logic; extend/wire it.

### Script responsibilities
| Script | Job |
|---|---|
| GameManager | state machine, score/streak/hull/wanted, expedition flow |
| PlayerController | drag-to-fly |
| SamplingBeam | beam cone visual + capture zone (golden beam IAP hooks built in) |
| Specimen | rise/wobble/capture, beamTime difficulty |
| Hazard | Chopper/Jet/Balloon movement + damage |
| Spawner | weighted spawns from expedition config, wanted-level scaling |
| ParallaxLayer | infinite scrolling background |
| IAPManager | 3 non-consumables, purchase + restore |
| HUDController | labels, beam/boost buttons |
| SaveData | PlayerPrefs: collection, unlocks, IAP flags, high score |

## Locked design decisions

- **5 Expeditions (levels):** Phoenix Metro → Camelback Foothills → Sedona → Deep Desert Night → The Lights Return (finale). Sedona is explicitly IN scope (a prior draft excluded it — that exclusion no longer applies).
- **Specimen catalog:** rarity tiers (Common/Uncommon/Rare/Legendary/Sealed), points, beamTime, flavor text — see `specimen_catalog.json` for full data, don't invent new specimens without checking there first.
- **Field Guide:** a collection meta-game — grey silhouette until a specimen is captured at least once, persisted via SaveData. In scope for v1.
- **Wanted-level scaling:** difficulty escalates the longer a run continues. In scope for v1.
- **Hazards escalate across expeditions:** Chopper (Expedition 1) → + Balloon (Expedition 2) → + Jet (Expedition 3) → all three combined (Expeditions 4–5).
- **IAP — 3 non-consumables:** `vlights_rainbow_lights`, `vlights_golden_beam`, `vlights_unlock_all`. Rainbow skin tints beam + edge lights when owned.
- **Out of scope for v1:** boss fight ("Mothership" finale hazard), Game Center leaderboards. Both are stretch-goal notes in the starter kit's README — not being built now. Don't add them without an explicit decision to bring them into scope.
- **No hard deadline pressure.** The Shipaton hackathon deadline (Sept 30, 2026) is not binding — this is being built for real, at a sustainable pace, not rushed for submission. Quality and actually shipping something playable matter more than hitting that date.

## Workflow — how work gets done here

Work is broken into small, independently-executable stories in `STORIES.md`. Each story:
- Has its own git branch (`feature/<story-slug>`)
- Is scoped to be completed in a **single fresh session with no prior conversation context** — this file + `STORIES.md` + `FEEDBACK.md` are the persistent memory, not chat history
- Has explicit acceptance criteria that must be independently verified (actually run it — don't narrate success)

**Before starting any story:** read this file, then the specific story in `STORIES.md`. Nothing else should be needed.

**On completion:** log verification evidence in `FEEDBACK.md` (see its format below), then report back for review. Don't merge to main yourself.

## Verification discipline (non-negotiable)

Carried over from hard lessons on a prior project:
- **Never trust your own "done" narration as verification.** Agents (including past sessions of this one) have fabricated completion claims before. Verify independently: run it, check actual values/state, grep for evidence — don't just describe what should be true.
- **Report claim vs. verified state separately** in `FEEDBACK.md` — what you did, and what you actually confirmed by checking, as two distinct things.
- **If something looks contradictory** (e.g. a report says X is fixed but a screenshot/test shows otherwise), say so plainly and investigate — don't paper over it or repeat the same unverified claim.
- **Stay in scope.** Each story has an explicit scope and out-of-scope list. If a task naturally wants to expand beyond it, flag that rather than silently doing more (or less).

## FEEDBACK.md format

Pre-populated with known risk areas; append entries per story in this shape:
```
## Story X.X — <name>
Claim: <what was implemented/attempted>
Verified: <what was actually confirmed by running/checking — be specific>
Status: <done | partial | blocked, with reason>
```

## Known risk areas (pre-flagged, watch for these)

- **IAP sandbox testing** — historically finicky; test on a real device via Xcode, signed into a sandbox Apple ID, before assuming it works.
- **Collision/physics edge cases** — verify hazard and specimen colliders behave correctly, don't assume Inspector setup matches intended behavior without a live test.
- **Weighted spawn tables** — low-weight legendary specimens (e.g. 0.2–0.25 weight) need actual confirmed-spawnable testing, not just "it's in the JSON."
