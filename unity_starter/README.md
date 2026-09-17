# V-LIGHTS: Phoenix — Unity starter kit

Hackathon-speed port of the web demo to Unity 2D + iOS. The web build is the
playable spec; these scripts re-implement it.

## 1. Project setup (15 min)
1. Unity Hub -> New project -> **2D (URP)** template (URP gives you the beam glow later).
2. Package Manager: install **In App Purchasing** (`com.unity.purchasing` 4.10+).
3. Player Settings -> set **Bundle Identifier** (e.g. `com.yourname.vlights`).
4. Copy this kit: `Scripts/` -> `Assets/Scripts/`, `Data/*.json` -> `Assets/Data/`
   (create the folders; assign the two JSON files as TextAssets in the Inspector).
5. Copy your art: backgrounds -> `Assets/Art/Backgrounds/`,
   specimen sprites -> `Assets/Resources/Specimens/` (PNG, names MUST match the
   `sprite` field in specimen_catalog.json, e.g. `specimen_tacotruck.png`).

## 2. Scene hierarchy (one scene: "Main")
- Main Camera (Orthographic, Size 5, position z=-10)
- BackgroundFar (SpriteRenderer + ParallaxLayer, scrollFactor 0.15)
- BackgroundMid (SpriteRenderer + ParallaxLayer, scrollFactor 0.4)
- Ground (dark strip; specimens spawn on it)
- Player (PlayerController + SamplingBeam child "Beam" + ship SpriteRenderer)
- Spawner (Spawner; assign prefabs)
- IAP (IAPManager, DontDestroyOnLoad)
- Canvas (HUDController; specimen/streak/score/hull/wanted labels; Beam + Boost buttons)
- Panels: TitlePanel, GameOverPanel, FieldGuidePanel, ShopPanel (show/hide via GameManager.State)

## 3. Prefabs to make (30 min)
- **Specimen**: SpriteRenderer + CircleCollider2D(isTrigger) + Specimen script.
- **Chopper / Jet / Balloon**: SpriteRenderer + BoxCollider2D(isTrigger) + Hazard script.
  (Placeholder art: colored capsules/boxes are fine for the hackathon. Swap later.)

## 4. Build order (fastest path to playable)
1. Player drag + parallax scroll (GameManager.StartExpedition(1) on tap)
2. Beam + specimen capture + score/streak (the core loop)
3. Hazards + hull + game over
4. Wanted level scaling (already in GameManager/Spawner)
5. HUD + Title/GameOver panels
6. Field Guide panel (grid from catalog; grey silhouette until collected)
7. IAP: wire ShopPanel buttons -> IAPManager.Buy(id); rainbow skin tints beam+edge lights
8. Expedition select (MapKit-style pins are stretch; simple list is fine)

## 5. iOS build checklist
- File -> Build Settings -> iOS -> Build (opens Xcode).
- Xcode: automatic signing with your Apple Developer team.
- **IAP setup (do early!)**: App Store Connect -> your app -> In-App Purchases ->
  create 3 Non-Consumables with EXACT ids: `vlights_rainbow_lights`,
  `vlights_golden_beam`, `vlights_unlock_all`. Add a Sandbox tester Apple ID.
- Test IAP: run on device from Xcode while signed into the sandbox account.
  Include a **Restore Purchases** button (App Store review requires it).
- TestFlight: Xcode -> Product -> Archive -> Distribute -> TestFlight.

## 6. Stretch (if time)
- Boss: finale "Mothership" hazard pattern (the web build has boss mentions)
- MapKit-style expedition select on a night map of Phoenix
- Game Center leaderboards (web build has high scores -> natural fit)
- Audio: beam hum rising with streak, capture chime (WebAudio knowledge ports directly)

## Script map
| Script | Job |
|---|---|
| GameManager | state machine, score/streak/hull/wanted, expedition flow |
| PlayerController | drag-to-fly |
| SamplingBeam | beam cone visual + capture zone (golden beam IAP hooks built in) |
| Specimen | rise/wobble/capture, beamTime difficulty |
| Hazard | chopper/jet/balloon movement + damage |
| Spawner | weighted spawns from expedition config, wanted scaling |
| ParallaxLayer | infinite scrolling background |
| IAPManager | 3 non-consumables, purchase + restore |
| HUDController | labels, beam/boost buttons |
| SaveData | PlayerPrefs: collection, unlocks, IAP flags, high score |
