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

### Key design notes from mockups
- Deep purple/navy night sky ✓ (already in game)
- Rainbow beam = vlights_rainbow_lights IAP skin
- Heart icons for health (not text) — upgrade in HUD polish
- "PHOENIX LIGHTS" branding bottom right
- Game over copy: "SIGNAL LOST" (not "EXPEDITION COMPLETE")
- Title copy: "TAP TO FLY" + "DRAG to fly — HOLD to drop the tractor beam"
