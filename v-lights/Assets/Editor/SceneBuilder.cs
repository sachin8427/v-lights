// Story 1.2 — Run via Unity menu: VLights > Build Scene Hierarchy
// Clears the current scene and reconstructs the full Main scene from scratch.
// Safe to re-run (destroys existing root objects first).
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.12f); // deep desert night blue
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.AddComponent<AudioListener>();

        // ---- BACKGROUND LAYERS ----
        CreateBgLayer("BackgroundFar", 0.15f, new Color(0.15f, 0.10f, 0.25f), -5f, 0f, 1f);
        CreateBgLayer("BackgroundMid", 0.40f, new Color(0.20f, 0.14f, 0.30f), -4f, -1f, 0f);

        // ---- GROUND ----
        var ground = new GameObject("Ground");
        var groundSr = ground.AddComponent<SpriteRenderer>();
        groundSr.sprite = MakeSolidSprite(new Color(0.08f, 0.05f, 0.10f));
        groundSr.drawMode = SpriteDrawMode.Sliced;
        ground.transform.position = new Vector3(0, -4.5f, 0);
        ground.transform.localScale = new Vector3(30f, 1f, 1f);

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
        scaler.referenceResolution = new Vector2(1170, 2532); // iPhone 14 Pro
        scaler.matchWidthOrHeight = 0.5f;
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
        var titlePanel    = MakePanel(canvasGo, "TitlePanel",     new Color(0f, 0f, 0f, 0.85f), "TAP TO START\n\nV-LIGHTS: Phoenix",     startActive: true);
        var gameOverPanel = MakePanel(canvasGo, "GameOverPanel",  new Color(0f, 0f, 0f, 0.85f), "EXPEDITION COMPLETE\n\nSCORE: 0",        startActive: false);
        MakePanel(canvasGo, "FieldGuidePanel", new Color(0.05f, 0.05f, 0.15f, 0.95f), "FIELD GUIDE\n\n(collection grid — Story 3.3)", startActive: false);
        MakePanel(canvasGo, "ShopPanel",       new Color(0.05f, 0.05f, 0.15f, 0.95f), "SHOP\n\n(IAP — Story 4.1)",                    startActive: false);

        // Wire panel refs into HUD
        hud.titlePanel    = titlePanel;
        hud.gameOverPanel = gameOverPanel;
        hud.gameOverLabel = gameOverPanel.GetComponentInChildren<Text>();

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
