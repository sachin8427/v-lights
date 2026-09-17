using UnityEngine;

// Infinite horizontal scroller — dual-tile approach for seamless looping.
// tileWidth must be >= the camera's visible width (world units) to prevent seams.
// Pass it explicitly from SceneBuilder; don't rely on bounds which can be wrong in editor context.
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)] public float scrollFactor = 0.3f;
    public float tileWidth = 18f; // world-space width of one tile — set by SceneBuilder

    float _offset;
    float _baseX;
    Transform _tileB;

    void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        _baseX = transform.position.x;

        var bGO = new GameObject(name + "_B");
        bGO.transform.SetParent(transform.parent);
        bGO.transform.position = new Vector3(_baseX + tileWidth, transform.position.y, transform.position.z);
        var srB = bGO.AddComponent<SpriteRenderer>();
        srB.sprite       = sr.sprite;
        srB.sortingOrder = sr.sortingOrder;
        srB.color        = sr.color;
        _tileB = bGO.transform;
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || !gm.IsPlaying || scrollFactor == 0f) return;

        _offset += gm.CurrentExpedition.scrollSpeed * scrollFactor * Time.deltaTime;

        float pos = _baseX - (_offset % tileWidth);
        transform.position = new Vector3(pos,               transform.position.y, transform.position.z);
        _tileB.position    = new Vector3(pos + tileWidth,   _tileB.position.y,    _tileB.position.z);
    }
}
