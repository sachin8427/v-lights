// Story 1.2 — Run via Unity menu: VLights > Build Scene Hierarchy
// Clears the current scene and reconstructs the full Main scene from scratch.
// Safe to re-run (destroys existing root objects first).
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SceneBuilder
{
    [MenuItem("VLights/Build Scene Hierarchy")]
    public static void Build()
    {
        // --- Clear existing scene ---
        foreach (var go in Object.FindObjectsOfType<GameObject>())
            if (go.transform.parent == null) Object.DestroyImmediate(go);

        // ---- CAMERA ----
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.039f, 0.055f, 0.122f); // #0A0E1F deep navy
        cam.allowHDR = true;
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.AddComponent<AudioListener>();

        // URP post-processing — global Volume with Bloom
        System.IO.Directory.CreateDirectory(
            System.IO.Path.Combine(Application.dataPath, "PostProcessing"));
        var ppProfilePath = "Assets/PostProcessing/DefaultPostProcess.asset";
        var ppProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ppProfilePath);
        if (ppProfile == null)
        {
            ppProfile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(ppProfile, ppProfilePath);
        }
        if (!ppProfile.Has<Bloom>())
        {
            var bloom = ppProfile.Add<Bloom>(overrides: true);
            bloom.active = true;
            bloom.intensity.Override(1.3f);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.7f);
        }
        AssetDatabase.SaveAssets();
        var volGo = new GameObject("PostProcessVolume");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.profile = ppProfile;

        // ---- BACKGROUND LAYERS ----
        // Infinite scroll via material.mainTextureOffset — no tile seams, no teleporting GOs.
        // Screen at orthoSize=5: 10 units tall. iPhone 19.5:9 → 21.67 wide. Use W=22 for full coverage.
        //
        // sky_parallax_layer.png:    2048×1152 (16:9) → fill full screen at (22 × 12)
        // mountain_parallax_layer.png: 2736×912  (3:1) → thin strip at (22 × 1.5), y=-1
        // city_parallax_layer.png:   2096×1184 (1.77:1) → thin strip at (22 × 1.2), y=-2.5
        //
        // Layer stack (back → front):
        //   Background_Sky       z=10  sortOrder=-10  scroll=0.05
        //   Background_Mountains z=6   sortOrder=-8   scroll=0.25
        //   Background_City      z=3   sortOrder=-5   scroll=0.6

        // Sky — fills entire screen, slowest scroll
        var skySprite = ImportSprite("Assets/Art/Backgrounds/sky_parallax_layer.png", alphaIsTransparency: false);
        if (skySprite != null)
        {
            var sky = new GameObject("Background_Sky");
            var skySr = sky.AddComponent<SpriteRenderer>();
            skySr.sprite = skySprite;
            skySr.sortingOrder = -10;
            sky.transform.localScale = SpriteScale(skySprite, 22f, 12f);
            sky.transform.position = new Vector3(0, 0, 10f);
            var skyPl = sky.AddComponent<ParallaxLayer>();
            skyPl.scrollFactor = 0.05f;
        }

        // Mountains — thin silhouette strip, medium scroll
        var mtSprite = ImportSprite("Assets/Art/Backgrounds/mountain_parallax_layer.png", alphaIsTransparency: true);
        if (mtSprite != null)
        {
            var mt = new GameObject("Background_Mountains");
            var mtSr = mt.AddComponent<SpriteRenderer>();
            mtSr.sprite = mtSprite;
            mtSr.sortingOrder = -8;
            // Scale to 22 wide to cover iPhone 19.5:9; height 1.5 as specified
            mt.transform.localScale = SpriteScale(mtSprite, 22f, 1.5f);
            mt.transform.position = new Vector3(0, -1f, 6f);
            var mtPl = mt.AddComponent<ParallaxLayer>();
            mtPl.scrollFactor = 0.25f;
        }

        // City — thin strip at bottom, fastest foreground scroll
        var ctSprite = ImportSprite("Assets/Art/Backgrounds/city_parallax_layer.png", alphaIsTransparency: true);
        if (ctSprite != null)
        {
            var ct = new GameObject("Background_City");
            var ctSr = ct.AddComponent<SpriteRenderer>();
            ctSr.sprite = ctSprite;
            ctSr.sortingOrder = -5;
            // Scale to 22 wide; height 1.2 as specified
            ct.transform.localScale = SpriteScale(ctSprite, 22f, 1.2f);
            ct.transform.position = new Vector3(0, -2.5f, 3f);
            var ctPl = ct.AddComponent<ParallaxLayer>();
            ctPl.scrollFactor = 0.6f;
        }

        // Ground marker — empty transform, no visual
        var ground = new GameObject("Ground");
        ground.transform.position = new Vector3(0, -3.4f, 0);

        // ---- GAME MANAGER ----
        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        gm.specimenCatalogJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/specimen_catalog.json");
        gm.expeditionsJson     = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Data/expeditions.json");

        // ---- PLAYER ----
        // Player root: PlayerController + SamplingBeam (auto-added via [RequireComponent]).
        // SpriteRenderer must be on a child "Ship" — MeshRenderer (added by SamplingBeam.Awake)
        // and SpriteRenderer cannot coexist on the same GameObject.
        var player = new GameObject("Player");
        player.transform.position = new Vector3(-3f, 0f, 0);
        var pc = player.AddComponent<PlayerController>(); // SamplingBeam auto-added here
        pc.followSharpness = 12f;
        pc.fingerOffset = new Vector2(0f, 1.6f);

        // Ship visual on a child so it doesn't conflict with SamplingBeam's MeshRenderer
        var shipGo = new GameObject("Ship");
        shipGo.transform.SetParent(player.transform, false);
        var playerSr = shipGo.AddComponent<SpriteRenderer>();
        playerSr.sprite = MakeSolidSprite(new Color(0.6f, 0.8f, 1.0f)); // placeholder: light blue
        playerSr.sortingOrder = 10;
        shipGo.transform.localScale = new Vector3(1.2f, 0.5f, 1f);

        // Collider + Rigidbody required for hazard OnTriggerEnter2D to fire
        var playerCol = player.AddComponent<CapsuleCollider2D>();
        playerCol.size = new Vector2(1.0f, 0.5f);
        var playerRb = player.AddComponent<Rigidbody2D>();
        playerRb.gravityScale = 0f;
        playerRb.freezeRotation = true;
        // Dynamic (not kinematic) so OnTriggerEnter2D fires against hazard triggers.
        // PlayerController moves via transform.position; gravity=0 keeps it stable.

        // ---- SPAWNER ----
        var spawnerGo = new GameObject("Spawner");
        spawnerGo.AddComponent<Spawner>(); // prefabs wired after prefab creation below

        // ---- AUDIO MANAGER ----
        var audioGo = new GameObject("AudioManager");
        audioGo.AddComponent<AudioManager>();

        // ---- IAP MANAGER ----
        var iapGo = new GameObject("IAP");
        iapGo.AddComponent<IAPManager>();

        // ---- CANVAS + HUD ----
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(2532, 1170); // iPhone 14 Pro landscape
        scaler.matchWidthOrHeight = 1f; // match height — the fixed axis in landscape
        canvasGo.AddComponent<GraphicRaycaster>();
        var hud = canvasGo.AddComponent<HUDController>();

        // HUD labels
        var specimenLabel = MakeLabel(canvasGo, "SpecimenLabel", "SPECIMENS 0/21",
            new Vector2(0.5f, 0.97f), new Vector2(600, 50), 28);
        var streakLabel   = MakeLabel(canvasGo, "StreakLabel", "",
            new Vector2(0.5f, 0.91f), new Vector2(600, 50), 28);
        var scoreLabel    = MakeLabel(canvasGo, "ScoreLabel", "0",
            new Vector2(0.5f, 0.85f), new Vector2(300, 50), 36);
        var hullLabel     = MakeLabel(canvasGo, "HullLabel", "\u2665\u2665\u2665",
            new Vector2(0.15f, 0.97f), new Vector2(200, 50), 28);
        var wantedLabel   = MakeLabel(canvasGo, "WantedLabel", "WANTED \u25A1\u25A1\u25A1\u25A1\u25A1",
            new Vector2(0.85f, 0.97f), new Vector2(300, 50), 22);

        // Buttons — anchored to bottom of screen
        var beamButton  = MakeButton(canvasGo, "BeamButton",  "BEAM",  new Vector2(0.25f, 0.08f));
        var boostButton = MakeButton(canvasGo, "BoostButton", "BOOST", new Vector2(0.75f, 0.08f));

        // Wire HUD references
        hud.specimenLabel = specimenLabel.GetComponent<Text>();
        hud.streakLabel   = streakLabel.GetComponent<Text>();
        hud.scoreLabel    = scoreLabel.GetComponent<Text>();
        hud.hullLabel     = hullLabel.GetComponent<Text>();
        hud.wantedLabel   = wantedLabel.GetComponent<Text>();
        hud.beamButton    = beamButton;
        hud.boostButton   = boostButton;

        // ---- PANELS ----
        var titlePanel    = MakePanel(canvasGo, "TitlePanel",     new Color(0f, 0f, 0f, 0.85f), "V-LIGHTS: PHOENIX\n\nTAP TO FLY\n\nDRAG to fly  —  HOLD to beam",  startActive: true);
        var gameOverPanel = MakePanel(canvasGo, "GameOverPanel",  new Color(0f, 0f, 0f, 0.85f), "SIGNAL LOST\n\nSCORE: 0\n\nTAP TO FLY AGAIN",                        startActive: false);
        // Add FLY AGAIN button to GameOverPanel (onClick wired in HUDController.Start)
        var flyAgainBtn = MakeButton(gameOverPanel, "FlyAgainButton", "FLY AGAIN", new Vector2(0.5f, 0.22f));
        flyAgainBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 80);

        var fieldGuidePanel = MakeFieldGuidePanel(canvasGo);
        MakePanel(canvasGo, "ShopPanel", new Color(0.05f, 0.05f, 0.15f, 0.95f), "SHOP\n\n(IAP — Story 4.1)", startActive: false);
        var fgController = fieldGuidePanel.GetComponent<FieldGuideController>();

        // GUIDE button lives AFTER panels in canvas order so it renders on top.
        // Only functional on GameOver screen (natural between-runs moment).
        var fgButton = MakeButton(canvasGo, "FieldGuideButton", "GUIDE", new Vector2(0.15f, 0.35f));
        fgButton.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 70);
        fgButton.GetComponent<Image>().color = new Color(0.22f, 0.40f, 0.72f, 0.9f);
        fgButton.GetComponent<Button>().onClick.AddListener(() => fieldGuidePanel.SetActive(true));
        fgButton.SetActive(false); // shown by HUD only on GameOver

        // Wire panel refs into HUD
        hud.titlePanel       = titlePanel;
        hud.gameOverPanel    = gameOverPanel;
        hud.gameOverLabel    = gameOverPanel.GetComponentInChildren<Text>();
        hud.fieldGuide       = fgController;
        hud.fieldGuideButton = fgButton;
        hud.flyAgainButton   = flyAgainBtn.GetComponent<Button>();

        // ---- PREFABS ----
        CreateSpecimenPrefab();
        CreateHazardPrefab("Chopper", new Color(0.8f, 0.2f, 0.2f), HazardKind.Chopper, new Vector2(1.8f, 0.5f));
        CreateHazardPrefab("Jet",     new Color(0.9f, 0.6f, 0.1f), HazardKind.Jet,     new Vector2(2.4f, 0.4f));
        CreateHazardPrefab("Balloon", new Color(0.3f, 0.8f, 0.3f), HazardKind.Balloon, new Vector2(0.6f, 1.2f));

        // Wire Spawner prefabs
        var spawner = spawnerGo.GetComponent<Spawner>();
        spawner.specimenPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Specimen.prefab");
        spawner.chopperPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Chopper.prefab");
        spawner.jetPrefab      = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Jet.prefab");
        spawner.balloonPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Balloon.prefab");

        // Save scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SceneBuilder] Scene hierarchy built successfully.");
        EditorUtility.DisplayDialog("VLights Scene Builder", "Scene hierarchy built!\n\nCheck Console for any warnings.\nSave the scene (Ctrl+S) if prompted.", "OK");
    }

    // ---- Helpers ----

    // Import a PNG as a Sprite with background-layer settings.
    //   alphaIsTransparency: true for layers with transparent alpha channel
    //   wrapRepeat: true (default) for parallax layers — required for mainTextureOffset looping
    static Sprite ImportSprite(string assetPath, bool alphaIsTransparency = false, bool wrapRepeat = true)
    {
        if (!System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath, "..", assetPath)))
            return null;
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.Default);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            var wrap = wrapRepeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            if (importer.textureType != TextureImporterType.Sprite)      { importer.textureType = TextureImporterType.Sprite; changed = true; }
            if (importer.spriteImportMode != SpriteImportMode.Single)    { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (importer.spriteMeshType != SpriteMeshType.FullRect)      { importer.spriteMeshType = SpriteMeshType.FullRect; changed = true; }
            if (importer.alphaIsTransparency != alphaIsTransparency)     { importer.alphaIsTransparency = alphaIsTransparency; changed = true; }
            if (importer.mipmapEnabled)                                   { importer.mipmapEnabled = false; changed = true; }
            if (importer.spritePixelsPerUnit != 100)                     { importer.spritePixelsPerUnit = 100; changed = true; }
            if (importer.filterMode != FilterMode.Bilinear)              { importer.filterMode = FilterMode.Bilinear; changed = true; }
            if (importer.wrapMode != wrap)                               { importer.wrapMode = wrap; changed = true; }

            // Default platform: uncompressed for editor accuracy
            var defSettings = importer.GetDefaultPlatformTextureSettings();
            if (defSettings.maxTextureSize != 2048 || defSettings.textureCompression != TextureImporterCompression.Uncompressed)
            {
                defSettings.maxTextureSize = 2048;
                defSettings.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SetPlatformTextureSettings(defSettings);
                changed = true;
            }

            // iOS: ASTC 6x6 — best quality/size for Metal on iPhone
            var iosSettings = importer.GetPlatformTextureSettings("iPhone");
            if (!iosSettings.overridden || iosSettings.maxTextureSize != 2048 || iosSettings.format != TextureImporterFormat.ASTC_6x6)
            {
                iosSettings.overridden = true;
                iosSettings.maxTextureSize = 2048;
                iosSettings.format = TextureImporterFormat.ASTC_6x6;
                importer.SetPlatformTextureSettings(iosSettings);
                changed = true;
            }

            if (changed) AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    // Returns localScale to display a sprite at the given world dimensions (worldW x worldH units).
    static Vector3 SpriteScale(Sprite sprite, float worldW, float worldH) =>
        new Vector3(worldW / sprite.bounds.size.x, worldH / sprite.bounds.size.y, 1f);

    static void CreateBgLayer(string name, float scrollFactor, Color color, float z, float y, float sortOrder)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSolidSprite(color);
        sr.drawMode = SpriteDrawMode.Sliced;
        sr.sortingOrder = (int)sortOrder - 10;
        go.transform.position = new Vector3(0, y, z);
        go.transform.localScale = new Vector3(24f, 12f, 1f);
        var pl = go.AddComponent<ParallaxLayer>();
        pl.scrollFactor = scrollFactor;
    }

    static Sprite MakeSolidSprite(Color color)
    {
        var tex = new Texture2D(4, 4);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
    }

    static GameObject MakeLabel(GameObject parent, string name, string text,
        Vector2 anchorCenter, Vector2 size, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchorCenter;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        var t = go.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return go;
    }

    static GameObject MakeButton(GameObject parent, string name, string label, Vector2 anchorCenter)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchorCenter;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200, 100);
        rt.anchoredPosition = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 0.72f, 0.25f, 0.7f);
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.pressedColor = new Color(1f, 0.5f, 0.1f);
        btn.colors = colors;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.sizeDelta = Vector2.zero;
        var t = textGo.AddComponent<Text>();
        t.text = label;
        t.fontSize = 32;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.black;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        return go;
    }

    static GameObject MakeFieldGuidePanel(GameObject parent)
    {
        var panel = new GameObject("FieldGuidePanel");
        panel.transform.SetParent(parent.transform, false);
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;
        var panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0.04f, 0.04f, 0.12f, 1f);

        // Header
        var header = new GameObject("Header");
        header.transform.SetParent(panel.transform, false);
        var headerRt = header.AddComponent<RectTransform>();
        headerRt.anchorMin = new Vector2(0, 0.92f); headerRt.anchorMax = Vector2.one;
        headerRt.sizeDelta = Vector2.zero;
        var headerText = header.AddComponent<Text>();
        headerText.text = "FIELD GUIDE";
        headerText.fontSize = 36; headerText.fontStyle = FontStyle.Bold;
        headerText.color = new Color(1f, 0.88f, 0.45f);
        headerText.alignment = TextAnchor.MiddleCenter;
        headerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Close button
        var closeBtn = MakeButton(panel, "CloseButton", "X", new Vector2(0.95f, 0.96f));
        closeBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(60, 60);
        closeBtn.GetComponent<Image>().color = new Color(0.6f, 0.1f, 0.1f, 0.8f);

        // ScrollView
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(panel.transform, false);
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.02f, 0.02f);
        scrollRt.anchorMax = new Vector2(0.98f, 0.91f);
        scrollRt.sizeDelta = Vector2.zero;
        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        var scrollBg = scrollGo.AddComponent<Image>();
        scrollBg.color = new Color(0, 0, 0, 0.1f);

        // Viewport — use RectMask2D (clips by rect geometry, not alpha, so no transparent-image gotcha)
        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollGo.transform, false);
        var viewportRt = viewport.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero; viewportRt.anchorMax = Vector2.one;
        viewportRt.sizeDelta = Vector2.zero;
        viewport.AddComponent<RectMask2D>();
        scrollRect.viewport = viewportRt;

        // Content with GridLayoutGroup
        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRt = content.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = new Vector2(0, 3000f); // tall default; FieldGuideController sizes this at runtime
        var grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(200, 140);
        grid.spacing = new Vector2(8, 8);
        grid.padding = new RectOffset(8, 8, 8, 8);
        grid.constraint = GridLayoutGroup.Constraint.Flexible;
        scrollRect.content = contentRt;

        // Wire FieldGuideController
        var fg = panel.AddComponent<FieldGuideController>();
        fg.gridContent = contentRt;

        // Wire close button
        closeBtn.GetComponent<Button>().onClick.AddListener(() => panel.SetActive(false));

        panel.SetActive(false);
        return panel;
    }

    static GameObject MakePanel(GameObject parent, string name, Color bgColor, string labelText, bool startActive = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var textGo = new GameObject("Label");
        textGo.transform.SetParent(go.transform, false);
        var trt = textGo.AddComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.1f, 0.3f); trt.anchorMax = new Vector2(0.9f, 0.7f);
        trt.sizeDelta = Vector2.zero;
        var t = textGo.AddComponent<Text>();
        t.text = labelText;
        t.fontSize = 40;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        go.SetActive(startActive);
        return go;
    }

    static void CreateSpecimenPrefab()
    {
        var go = new GameObject("Specimen");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSolidSprite(new Color(0.9f, 0.85f, 0.2f)); // placeholder: yellow
        sr.sortingOrder = 5;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;
        go.AddComponent<Specimen>();
        SavePrefab(go, "Specimen");
        Object.DestroyImmediate(go);
    }

    static void CreateHazardPrefab(string name, Color color, HazardKind kind, Vector2 size)
    {
        var go = new GameObject(name);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MakeSolidSprite(color);
        sr.sortingOrder = 8;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        var hazard = go.AddComponent<Hazard>();
        hazard.kind = kind;
        SavePrefab(go, name);
        Object.DestroyImmediate(go);
    }

    static void SavePrefab(GameObject go, string name)
    {
        string path = $"Assets/Prefabs/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        AssetDatabase.Refresh();
    }
}
#endif
