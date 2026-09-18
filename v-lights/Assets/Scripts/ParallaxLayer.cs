using UnityEngine;

// Infinite horizontal parallax via A/B sprite recycling.
//
// Attach to a parent GO. Two SpriteRenderer children (named *_A and *_B) are
// placed side-by-side. When either tile scrolls completely past the camera's
// left edge, it is repositioned exactly one sprite-width to the right of the
// other — no UV tricks, no texture wrapping, no mirroring.
//
// scrollFactor = 0 → fully static (safe for initial composition review).
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 2f)] public float scrollFactor = 0f;

    Transform _tileA, _tileB;
    float _spriteWorldWidth;
    Camera _cam;

    void Start()
    {
        _cam = Camera.main;

        // Expect exactly two SpriteRenderer children: A (index 0) and B (index 1)
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        if (renderers.Length < 2)
        {
            Debug.LogError($"[ParallaxLayer] {name}: expected 2 SpriteRenderer children, found {renderers.Length}.");
            enabled = false;
            return;
        }

        _tileA = renderers[0].transform;
        _tileB = renderers[1].transform;

        // Compute world-space width from A's actual rendered bounds (includes scale)
        _spriteWorldWidth = renderers[0].bounds.size.x;

        // Position B exactly one width to the right of A
        // (SceneBuilder also sets this, but we enforce it at runtime to handle any drift)
        _tileB.position = new Vector3(
            _tileA.position.x + _spriteWorldWidth,
            _tileA.position.y,
            _tileA.position.z);
    }

    void Update()
    {
        if (scrollFactor == 0f) return;
        var gm = GameManager.I;
        if (gm == null || !gm.IsPlaying) return;

        float delta = gm.CurrentExpedition.scrollSpeed * scrollFactor * Time.deltaTime;
        _tileA.position -= new Vector3(delta, 0, 0);
        _tileB.position -= new Vector3(delta, 0, 0);

        // Recycle whichever tile has its right edge past the camera's left edge
        float camLeft = _cam.transform.position.x - _cam.orthographicSize * _cam.aspect;
        RecycleIfOffLeft(_tileA, _tileB, camLeft);
        RecycleIfOffLeft(_tileB, _tileA, camLeft);
    }

    // If 'tile' has scrolled fully past camLeft, jump it to the right of 'other'
    void RecycleIfOffLeft(Transform tile, Transform other, float camLeft)
    {
        if (tile.position.x + _spriteWorldWidth * 0.5f < camLeft)
            tile.position = new Vector3(other.position.x + _spriteWorldWidth, tile.position.y, tile.position.z);
    }
}
