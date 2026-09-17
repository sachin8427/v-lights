using UnityEngine;

// A specimen on the ground. Beams up while inside the sampling cone;
// capture progress scales with data.beamTime (the risk/reward knob).
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class Specimen : MonoBehaviour
{
    public SpecimenData Data { get; private set; }

    float _progress;
    bool _captured;
    Vector3 _groundPos;
    float _bobPhase;
    SamplingBeam _beam;

    public void Setup(SpecimenData data, Sprite sprite, SamplingBeam beam)
    {
        Data = data;
        _beam = beam;
        GetComponent<SpriteRenderer>().sprite = sprite;
        _groundPos = transform.position;
        _bobPhase = Random.Range(0f, 6.28f);
    }

    void Update()
    {
        if (_captured || !GameManager.I.IsPlaying) return;

        bool inBeam = _beam.Active && _beam.Contains(transform.position);
        if (inBeam)
        {
            _progress += Time.deltaTime * _beam.SpeedMultiplier / Data.beamTime;
            // rise + wobble while being sampled
            transform.position += Vector3.up * Time.deltaTime * 1.2f;
            transform.position += Vector3.right * Mathf.Sin(Time.time * 18f) * Time.deltaTime * 0.6f;
            if (_progress >= 1f) Capture();
        }
        else
        {
            _progress = Mathf.Max(0, _progress - Time.deltaTime * 0.8f);
            // drift back down + idle bob
            transform.position = Vector3.MoveTowards(transform.position, _groundPos, Time.deltaTime * 1.5f);
            transform.position = new Vector3(transform.position.x,
                _groundPos.y + Mathf.Sin(Time.time * 2f + _bobPhase) * 0.08f,
                transform.position.z);
        }
    }

    void Capture()
    {
        _captured = true;
        GameManager.I.CaptureSpecimen(Data);
        // TODO: fly-to-ship animation + particle burst (juice)
        Destroy(gameObject);
    }
}
