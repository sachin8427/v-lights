# FEEDBACK.md — V-LIGHTS: Phoenix

Pre-populated with known risk areas. Append one entry per story.

---

## Story 1.1 — Unity project setup
Claim: Unity 2D URP project created, 12 scripts copied, IAP package installed, bundle ID set, both JSON files verified.
Verified: Console clear (zero errors after IAPManager fixes). JsonLoadTest confirmed both JSON files load and parse. IAP 4.12.2 in manifest.json.
Status: done

---

## Story 1.2 — Scene hierarchy + placeholder art
Claim: SceneBuilder editor script builds full Main scene hierarchy programmatically.
Verified: VLights > Build Scene Hierarchy ran cleanly. All GameObjects present in hierarchy. Scene plays with zero null-reference errors (1 UGS/IAP warning only — non-blocking). Specimen and Hazard prefabs created in Assets/Prefabs/.
Status: done

---

## Story 2.1 — Player drag + parallax scroll
Claim: Tap-to-start wired, TitlePanel shows/hides, parallax scrolls, player drag works.
Verified: Tap starts Expedition 1, background scrolls, player drag-to-fly confirmed. Fixed SamplingBeam.Awake() disabling Player GO — was calling gameObject.SetActive(false), now disables only MeshRenderer.
Status: done

---

## Story 2.2 — Beam + specimen capture
Claim: Beam activates visually, specimens pulled to ship, score/streak update, GameOver shows real score.
Verified: Beam amber cone visible on hold. Specimens show as yellow placeholders (runtime-generated sprite — MakeSolidSprite does not serialize into prefabs). Magnet pull toward ship works. Score updates on capture. GameOver panel shows correct score.
Status: done

---

## Story 2.3 — Hazards + hull + game over
Claim: Hazards visible, hull decreases on contact, GameOver triggers at zero hull.
Verified: Red/orange/green placeholder hazards visible (same serialization fix as specimens). Hull hearts decrease on Chopper contact. GameOver panel appears at zero hull. Fixed: Player needed dynamic Rigidbody2D (not kinematic) + CapsuleCollider2D for OnTriggerEnter2D to fire against static hazard triggers.
Status: done

---

## Story 3.1 — Wanted-level scaling
Claim: Difficulty visibly escalates as run continues — both time-based and capture-based.
Verified: Added time-based escalation (wantedLevel +1 every 10s). Increased per-level scaling from 12% to 20%. Tested with quota=20 — escalation clearly visible by 30-40s, hazards swarming by wanted level 3-4. WANTED shields fill correctly in HUD.
Status: done

---

## Story 3.2 — HUD panels + GameOver flow
Claim: TitlePanel, GameOverPanel, GUIDE button, FLY AGAIN button all wired. Score displayed correctly on GameOver.
Verified: TitlePanel shows on launch. GameOver shows "SIGNAL LOST" with real score. FLY AGAIN returns to title. GUIDE button visible on GameOver screen. Fixed: onClick listeners in SceneBuilder are not serialized — all button wiring moved to HUDController.Start().
Status: done

---

## Story 3.3 — Field Guide
Claim: Field Guide panel opens from GUIDE button, shows collected/uncollected specimen grid, closes with X.
Verified: Panel opens on GameOver screen. Grid populates with rarity-colored cells for collected specimens, dark "???" cells for uncollected. X button closes panel. Fixed: (1) Mask component on viewport with alpha=0 Image culled all grid children — replaced with RectMask2D. (2) ContentSizeFitter timing — bypassed with explicit height calculation in FieldGuideController. (3) onClick listeners in SceneBuilder not serialized — close button wired in FieldGuideController.Start().
Status: done

### Deferred cosmetic/design notes for Field Guide polish story:
- **Design decision pending**: Current behavior shows all 21 specimens (Pokédex-style, uncollected = "???"). User preference is to show ONLY collected specimens. Original spec says "grey silhouette until captured" — resolve which direction before polish pass.
- Grid layout and cell design is bare-bones placeholder (colored squares + text). Needs proper card design, specimen icons, scroll indicator.
- In landscape editor view, grid renders 10 columns (cells are tiny). Game is portrait-only on iOS — test on device or in portrait game view.
- Panel background should cover entire screen with no bleed-through from GameOver layer.

---

## Art Asset Inventory (reference — 2026-09-17)

Assets located at: `/Users/sachintayade/Dev/RevenueCat/v-lights/art/`

### Background layers
| File | Maps to | Use |
|---|---|---|
| `Codex Image Sep 16 09_45_35 PM.webp` | Expedition 1 (Phoenix Metro) | City skyline, taco truck, Chase Field |
| `Codex Image Sep 16 09_45_45 PM.webp` | Expedition 2/3 (Camelback/Sedona) | Desert red rocks, pool, cacti |
| `Codex Image Sep 16 09_45_18 PM.webp` | Expedition 4 (Deep Desert Night) | Tempe Town Lake, scooters, lit bridge |
| `85101ecd-....png` | BackgroundMid `phoenix_skyline` | Skyline silhouette, transparent bg |
| `a94816ee-....png` | BackgroundFar `desert_panorama` | Desert panorama with pool |
| `ca8119f1-....png` | BackgroundFar alt | Desert city panorama, crescent moon |

### Foreground/ground strips
| File | Content |
|---|---|
| `b61a06a9-....png` | Strip mall — "Filaliens Tacos 24/7", "HURT? CALL 555-ALIEN" |
| `f83808af-....png` | Strip mall — "Desert Suds", "Accident? Don't Fly Solo 833-UFO-WINS" |
| `Codex Image Sep 16 09_53_30 PM.png` | Shopping center — Desert Grounds Coffee, Canyon Pizza |

### Game mockups (target look)
| File | Notes |
|---|---|
| `cfabc56b-....png` | Camelback expedition concept art — full HUD with rainbow beam, flamingo, hearts |
| `Codex Image Sep 16 09_46_27 PM.png` | Full game UI mockup — collected counter, wanted meter, beam/boost buttons |

### Still needed
- UFO/ship sprite
- Specimen sprites (taco truck, pigeon, golf cart, etc.)
- Hazard sprites (chopper, jet, balloon)
- WebP → PNG conversion required before Unity import (`sips -s format png`)

---

## Spike — Background Parallax System (`spike/background-system`)

### What was attempted
Full background parallax rewrite across multiple iterations:
1. **Attempt 1** — `material.mainTextureOffset` on SpriteRenderer (Wrap=Repeat). Failed: sky mirrored/duplicated vertically at UV boundary (Clamp artefact), mountains/city squished into thin strips (non-uniform X/Y scale).
2. **Attempt 2** — A/B dual-sprite recycling with uniform aspect-ratio-correct scaling. Improved: sky correct, mountains and city visible at proper scale. Remaining issue: city bottom edge sat at y=−2 leaving a 3-unit gap to camera bottom (y=−5) through which the sky bled through again.
3. **Attempt 3 (current)** — Same A/B approach, city bottom anchored to camera bottom (y=−5), mountain horizon shifted to 35% from screen bottom (y=−1.5).

### Verified
- Sky fills full viewport, no mirroring, no UV artefacts
- Mountains visible at correct aspect ratio, horizon at ~35% from bottom
- City bottom flush with camera bottom — sky bleed-through gap eliminated (pending visual confirmation in Unity after last push)
- All parallax scroll speeds = 0 (static composition mode)
- Scene compiles and enters Play mode cleanly

### Current state (branch: `spike/background-system`, not merged)
**Pending approval** of static composition before scrolling is re-enabled.
Next steps when resuming:
1. Run VLights > Build Scene Hierarchy in Unity and confirm static composition looks correct (no sky bleed below city)
2. If approved: set scrollFactor sky=0.05, mountains=0.25, city=0.60 in SceneBuilder and re-run
3. If further position tuning needed: adjust `ctY` and `mtY` constants in SceneBuilder background section
4. After scrolling approved: merge spike → main, close spike branch

### Assets in project (`Assets/Art/Backgrounds/`)
| File | Layer | Scale | Notes |
|---|---|---|---|
| `sky_parallax_layer.png` | Background_Sky (z=10, order=-10) | uniform ~1.074, world 22×12.4 | 2048×1152 (16:9) |
| `mountain_parallax_layer.png` | MountainsLayer (z=6, order=-8) | uniform ~1.096, world 30×10 | 2736×912 (3:1) |
| `city_parallax_layer.png` | CityLayer (z=3, order=-5) | uniform ~1.050, world 22×12.4 | 2096×1184 (1.77:1) |

### Known risk areas for next session
- **City image content unknown** — if city image has buildings throughout (not just bottom 25%), vertical position alone may not achieve "25-30% city mass" target. May need to revisit scale or crop.
- **ParallaxLayer.Start()** repositions B tile at runtime. If SceneBuilder places B at a different position than computed, a single-frame pop may be visible on scene load — verify on device.
- **Bloom/URP post-processing** added to SceneBuilder (PostProcessing/DefaultPostProcess.asset) but not verified visually. Camera HDR=true set.

---

### Key design notes from mockups
- Deep purple/navy night sky ✓ (already in game)
- Rainbow beam = vlights_rainbow_lights IAP skin
- Heart icons for health (not text) — upgrade in HUD polish
- "PHOENIX LIGHTS" branding bottom right
- Game over copy: "SIGNAL LOST" (not "EXPEDITION COMPLETE")
- Title copy: "TAP TO FLY" + "DRAG to fly — HOLD to drop the tractor beam"
