using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

// Field Guide panel — grid of all specimens, grey until captured at least once.
// Assigned by SceneBuilder: gridContent (GridLayoutGroup parent).
// Open via HUDController field guide button; close via the X button on the panel.
public class FieldGuideController : MonoBehaviour
{
    public RectTransform gridContent;

    void Start()
    {
        // SceneBuilder onClick listeners aren't serialized — wire close button at runtime
        var closeBtn = transform.Find("CloseButton");
        if (closeBtn != null)
            closeBtn.GetComponent<Button>()?.onClick.AddListener(Close);
        else
            Debug.LogWarning("[FieldGuide] CloseButton not found — re-run SceneBuilder.");
    }

    void OnEnable() => StartCoroutine(BuildAfterLayout());

    IEnumerator BuildAfterLayout()
    {
        // Wait one frame so RectTransform layout is ready before populating cells
        yield return null;
        BuildGrid();
        // Wait another frame then force full rebuild so ContentSizeFitter expands scroll area
        yield return null;
        if (gridContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(gridContent);
            var parent = gridContent.parent as RectTransform;
            if (parent != null) LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        }
    }

    void BuildGrid()
    {
        if (gridContent == null) { Debug.LogError("[FieldGuide] gridContent is null! Re-run SceneBuilder."); return; }

        // Collect children first to avoid modifying collection while iterating
        var toDestroy = new List<GameObject>();
        foreach (Transform child in gridContent) toDestroy.Add(child.gameObject);
        foreach (var go in toDestroy) DestroyImmediate(go);

        var catalog = GameManager.I?.Catalog;
        if (catalog == null) { Debug.LogError("[FieldGuide] Catalog is null! Check GameManager.specimenCatalogJson ref."); return; }

        int count = 0;
        foreach (var data in catalog.specimens)
        {
            if (data.rarity == "Sealed") continue;
            CreateCell(data);
            count++;
        }
        Debug.Log($"[FieldGuide] Built {count} cells. gridContent={gridContent.name}, catalogLen={catalog.specimens.Length}");

        // Explicitly compute and set content height so ContentSizeFitter timing doesn't matter
        if (count > 0)
        {
            var grid = gridContent.GetComponent<GridLayoutGroup>();
            float cellW = grid != null ? grid.cellSize.x : 200f;
            float cellH = grid != null ? grid.cellSize.y : 140f;
            float spacingY = grid != null ? grid.spacing.y : 8f;
            int padH = grid != null ? grid.padding.horizontal : 16;
            int padTop = grid != null ? grid.padding.top : 8;
            int padBot = grid != null ? grid.padding.bottom : 8;
            float spacingX = grid != null ? grid.spacing.x : 8f;

            // Use current rect width; fall back to reference-res estimate if not yet laid out
            float contentW = gridContent.rect.width > 10f ? gridContent.rect.width : 1100f;
            int cols = Mathf.Max(1, Mathf.FloorToInt((contentW - padH) / (cellW + spacingX)));
            int rows = Mathf.CeilToInt((float)count / cols);
            float totalH = padTop + rows * cellH + Mathf.Max(0, rows - 1) * spacingY + padBot;
            gridContent.sizeDelta = new Vector2(gridContent.sizeDelta.x, totalH);
            Debug.Log($"[FieldGuide] Content height set to {totalH} ({rows} rows x {cols} cols)");
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(gridContent);
    }

    void CreateCell(SpecimenData data)
    {
        bool collected = SaveData.IsCollected(data.id);

        // Cell root
        var cell = new GameObject(data.id);
        cell.transform.SetParent(gridContent, false);
        cell.AddComponent<RectTransform>(); // sized by GridLayoutGroup

        var bg = cell.AddComponent<Image>();
        bg.color = collected ? RarityColor(data.rarity) : new Color(0.20f, 0.20f, 0.30f, 1f);

        // Specimen name
        MakeText(cell, "Name",
            collected ? data.name : "???",
            fontSize: 16, bold: true,
            anchor: (new Vector2(0f, 0.62f), Vector2.one),
            color: Color.white, alignment: TextAnchor.UpperCenter);

        // Rarity badge
        MakeText(cell, "Rarity",
            collected ? data.rarity.ToUpper() : "",
            fontSize: 13, bold: false,
            anchor: (new Vector2(0f, 0.42f), new Vector2(1f, 0.62f)),
            color: new Color(1f, 0.88f, 0.45f), alignment: TextAnchor.MiddleCenter);

        // Flavor text (truncated)
        MakeText(cell, "Flavor",
            collected ? Truncate(data.flavor, 70) : "",
            fontSize: 11, bold: false,
            anchor: (Vector2.zero, new Vector2(1f, 0.42f)),
            color: new Color(0.78f, 0.78f, 0.78f), alignment: TextAnchor.UpperCenter);
    }

    static void MakeText(GameObject parent, string name, string text,
        int fontSize, bool bold, (Vector2 min, Vector2 max) anchor,
        Color color, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor.min; rt.anchorMax = anchor.max;
        rt.offsetMin = new Vector2(4f, 2f);
        rt.offsetMax = new Vector2(-4f, -2f);
        var t = go.AddComponent<Text>();
        t.text = text;
        t.fontSize = fontSize;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.color = color;
        t.alignment = alignment;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Truncate;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length > max ? s[..max] + "…" : s;

    static Color RarityColor(string rarity) => rarity switch
    {
        "Common"    => new Color(0.22f, 0.52f, 0.22f, 1f),
        "Uncommon"  => new Color(0.22f, 0.40f, 0.72f, 1f),
        "Rare"      => new Color(0.52f, 0.22f, 0.72f, 1f),
        "Legendary" => new Color(0.72f, 0.55f, 0.08f, 1f),
        _           => new Color(0.25f, 0.25f, 0.30f, 1f),
    };

    public void Close() => gameObject.SetActive(false);
}
