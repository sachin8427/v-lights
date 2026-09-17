using UnityEngine;

// Infinite horizontal scroller for one parallax layer.
// Sprite must be tileable; width is read from the SpriteRenderer bounds.
[RequireComponent(typeof(SpriteRenderer))]
public class ParallaxLayer : MonoBehaviour
{
    [Range(0f, 1f)] public float scrollFactor = 0.3f; // 1 = moves with world

    float _width;
    Vector3 _start;

    void Start()
    {
        _width = GetComponent<SpriteRenderer>().bounds.size.x;
        _start = transform.position;
    }

    void Update()
    {
        var gm = GameManager.I;
        if (gm == null || !gm.IsPlaying) return;
        float speed = gm.CurrentExpedition.scrollSpeed * scrollFactor;
        transform.position += Vector3.left * speed * Time.deltaTime;
        if (_start.x - transform.position.x >= _width)
            transform.position = _start;
    }
}
