using UnityEngine;

// Drag-to-fly. Ship follows the finger with an offset so it is never
// hidden under it. Works with touch and mouse.
[RequireComponent(typeof(SamplingBeam))]
public class PlayerController : MonoBehaviour
{
    public float followSharpness = 12f;
    public Vector2 fingerOffset = new Vector2(0f, 1.6f); // world units above finger

    Vector2 _target;
    bool _dragging;
    Vector2 _lastPointer;
    Camera _cam;
    float _minX, _maxX, _minY, _maxY;

    void Start()
    {
        _cam = Camera.main;
        _target = transform.position;
        float h = _cam.orthographicSize;
        float w = h * _cam.aspect;
        _minX = -w + 1f; _maxX = w - 1f;
        _minY = -h + 1.5f; _maxY = h - 1f;
    }

    void Update()
    {
        if (!GameManager.I.IsPlaying) return;

        Vector2 pointer;
        bool down = false;

        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            pointer = _cam.ScreenToWorldPoint(t.position);
            down = t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
        }
        else if (Input.GetMouseButton(0))
        {
            pointer = _cam.ScreenToWorldPoint(Input.mousePosition);
            down = true;
        }
        else { _dragging = false; return; }

        if (down && !_dragging)
        {
            _dragging = true;
            _lastPointer = pointer;
        }
        if (_dragging)
        {
            Vector2 delta = pointer - _lastPointer;
            _lastPointer = pointer;
            _target += delta; // 1:1 drag — ship moves with the finger
        }

        _target.x = Mathf.Clamp(_target.x, _minX, _maxX);
        _target.y = Mathf.Clamp(_target.y, _minY, _maxY);
        Vector2 goal = _target + (_dragging ? fingerOffset : Vector2.zero);
        transform.position = Vector2.Lerp(transform.position, goal, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));

        // gentle banking tilt
        float vx = (goal.x - transform.position.x);
        transform.rotation = Quaternion.Euler(0, 0, Mathf.Clamp(-vx * 8f, -18f, 18f));
    }
}
