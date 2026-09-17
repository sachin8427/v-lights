using UnityEngine;

public enum HazardKind { Chopper, Jet, Balloon }

// Hazards scroll with the world and hurt the ship on contact.
[RequireComponent(typeof(Collider2D))]
public class Hazard : MonoBehaviour
{
    public HazardKind kind;
    public float baseSpeed = 2f;

    float _t;
    float _startY;

    void Start()
    {
        _startY = transform.position.y;
        _t = Random.Range(0f, 10f);
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Update()
    {
        if (!GameManager.I.IsPlaying) return;
        float scroll = GameManager.I.CurrentExpedition.scrollSpeed;
        _t += Time.deltaTime;

        switch (kind)
        {
            case HazardKind.Chopper: // slow patrol + sine hover (+ searchlight visual TODO)
                transform.position += Vector3.left * (scroll * 0.6f + baseSpeed * 0.4f) * Time.deltaTime;
                transform.position = new Vector3(transform.position.x,
                    _startY + Mathf.Sin(_t * 1.6f) * 0.7f, transform.position.z);
                break;
            case HazardKind.Jet: // fast straight pass
                transform.position += Vector3.left * (scroll + 7f) * Time.deltaTime;
                break;
            case HazardKind.Balloon: // drifts with the wind, slowly rises
                transform.position += (Vector3.left * scroll * 0.5f + Vector3.up * 0.35f) * Time.deltaTime;
                transform.position += Vector3.right * Mathf.Sin(_t * 0.8f) * Time.deltaTime * 0.5f;
                break;
        }

        if (transform.position.x < -14f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null)
            GameManager.I.Damage();
    }
}
