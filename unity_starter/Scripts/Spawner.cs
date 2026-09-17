using UnityEngine;

// Drives specimen + hazard spawns from the current ExpeditionData.
// Hazard rate scales with the wanted level (more sampling = more heat).
public class Spawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject specimenPrefab;
    public GameObject chopperPrefab;
    public GameObject jetPrefab;
    public GameObject balloonPrefab;

    [Header("Spawn line")]
    public float spawnX = 11f;
    public float groundY = -3.4f;

    SamplingBeam _beam;
    Sprite[] _specimenSprites; // parallel to catalog order; assign at runtime via Resources
    float _specTimer, _hazTimer;

    void Start()
    {
        _beam = FindObjectOfType<SamplingBeam>();
        // Specimen sprites: Resources/Specimens/<spriteName> (PNG, no extension in name)
        var cat = GameManager.I.Catalog;
        _specimenSprites = new Sprite[cat.specimens.Length];
        for (int i = 0; i < cat.specimens.Length; i++)
            _specimenSprites[i] = Resources.Load<Sprite>("Specimens/" + cat.specimens[i].sprite);
    }

    void Update()
    {
        var gm = GameManager.I;
        if (!gm.IsPlaying) return;
        var exp = gm.CurrentExpedition;

        _specTimer -= Time.deltaTime;
        if (_specTimer <= 0)
        {
            SpawnSpecimen(exp);
            _specTimer = Random.Range(1.2f, 2.6f);
        }

        float wantedScale = 1f - gm.wantedLevel * 0.12f; // wanted 5 -> 40% faster spawns
        _hazTimer -= Time.deltaTime;
        if (_hazTimer <= 0)
        {
            SpawnHazard(exp);
            _hazTimer = exp.hazardInterval * Mathf.Max(0.35f, wantedScale) * Random.Range(0.8f, 1.2f);
        }
    }

    void SpawnSpecimen(ExpeditionData exp)
    {
        var data = PickWeighted(exp);
        if (data == null) return;
        var go = Instantiate(specimenPrefab,
            new Vector3(spawnX + Random.Range(0f, 3f), groundY + Random.Range(-0.2f, 0.4f), 0),
            Quaternion.identity);
        int idx = System.Array.IndexOf(GameManager.I.Catalog.specimens, data);
        go.GetComponent<Specimen>().Setup(data, _specimenSprites[idx], _beam);
        // drift with world scroll
        go.AddComponent<WorldDrift>().speedFactor = 1f;
    }

    void SpawnHazard(ExpeditionData exp)
    {
        string kind = exp.hazards[Random.Range(0, exp.hazards.Length)];
        GameObject prefab = kind switch
        {
            "Jet" => jetPrefab,
            "Balloon" => balloonPrefab,
            _ => chopperPrefab,
        };
        float y = kind == "Balloon" ? Random.Range(-2f, 2f) : Random.Range(0.5f, 3.5f);
        var go = Instantiate(prefab, new Vector3(spawnX + 2f, y, 0), Quaternion.identity);
        go.GetComponent<Hazard>().kind = System.Enum.Parse<HazardKind>(kind);
    }

    SpecimenData PickWeighted(ExpeditionData exp)
    {
        float total = 0;
        foreach (var e in exp.specimens) total += e.weight;
        float r = Random.Range(0, total);
        foreach (var e in exp.specimens)
        {
            r -= e.weight;
            if (r <= 0) return GameManager.I.Catalog.Get(e.specimenId);
        }
        return null;
    }
}

// Moves a spawned object left with the world scroll; destroys off-screen.
public class WorldDrift : MonoBehaviour
{
    public float speedFactor = 1f;
    void Update()
    {
        if (!GameManager.I.IsPlaying) return;
        transform.position += Vector3.left * GameManager.I.CurrentExpedition.scrollSpeed * speedFactor * Time.deltaTime;
        if (transform.position.x < -14f) Destroy(gameObject);
    }
}
