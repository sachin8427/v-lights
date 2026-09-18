using UnityEngine;

// Infinite seamless parallax via material.mainTextureOffset — no tile seams, no teleporting GOs.
//
// Requirements for each background sprite:
//   - Wrap Mode: Repeat (so the UV wraps instead of clamping)
//   - Mesh Type: Full Rect (so UVs go 0→1 across the entire quad)
//   - Filter: Bilinear
//   - Alpha Is Transparency: ON for mid/city layers
//
// How the math works:
//   The sprite quad is W world units wide (= sprite.bounds.size.x * transform.localScale.x).
//   1 UV unit spans W world units. So to scroll at `targetSpeed` world units/sec:
//     uvDelta/sec = targetSpeed / W
//   where targetSpeed = expedition.scrollSpeed × scrollFactor.
//
// Result: the layer appears to scroll at the correct fraction of the game's scroll speed,
//   looping perfectly every W-world-units of travel regardless of frame rate.
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 2f)] public float scrollFactor = 0.3f;

    Material _mat;
    float _uvOffset;
    float _uvPerWorldUnit; // = 1f / worldWidth

    void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        // SpriteRenderer.material returns a per-instance clone — safe to mutate
        _mat = sr.material;

        float worldWidth = sr.sprite != null
            ? sr.sprite.bounds.size.x * transform.localScale.x
            : transform.localScale.x;

        _uvPerWorldUnit = worldWidth > 0.001f ? 1f / worldWidth : 0f;
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || !gm.IsPlaying || scrollFactor == 0f || _mat == null) return;

        _uvOffset += gm.CurrentExpedition.scrollSpeed * scrollFactor * _uvPerWorldUnit * Time.deltaTime;
        // Let it accumulate freely — float wraps gracefully; no modulo needed
        _mat.mainTextureOffset = new Vector2(_uvOffset, 0f);
    }
}
