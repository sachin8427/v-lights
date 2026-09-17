using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Wires HUD: specimen counter, streak, hearts, wanted shields, beam/boost buttons.
public class HUDController : MonoBehaviour
{
    [Header("Labels")]
    public Text specimenLabel;   // "SPECIMENS 3/21"
    public Text streakLabel;     // "RESEARCH STREAK x4"
    public Text scoreLabel;
    public Text hullLabel;       // hearts as text, or swap to images later
    public Text wantedLabel;     // shields as text, or swap to images later

    [Header("Buttons")]
    public GameObject beamButton;  // hold to sample
    public GameObject boostButton; // tap for burst

    [Header("Panels")]
    public GameObject titlePanel;
    public GameObject gameOverPanel;

    SamplingBeam _beam;
    PlayerController _player;
    float _boostCooldown;

    void Start()
    {
        _beam = FindObjectOfType<SamplingBeam>();
        _player = FindObjectOfType<PlayerController>();
        GameManager.I.OnHudDirty += Refresh;
        AddHold(beamButton, on => _beam.SetBeam(on));
        beamButton.GetComponent<Button>().onClick.AddListener(() => { }); // hold handled by triggers
        boostButton.GetComponent<Button>().onClick.AddListener(DoBoost);
        Refresh();
    }

    void AddHold(GameObject go, System.Action<bool> fn)
    {
        var trig = go.AddComponent<EventTrigger>();
        var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        down.callback.AddListener(_ => fn(true));
        var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        up.callback.AddListener(_ => fn(false));
        trig.triggers.Add(down); trig.triggers.Add(up);
    }

    void DoBoost()
    {
        if (_boostCooldown > 0 || !GameManager.I.IsPlaying) return;
        _boostCooldown = 6f;
        // TODO: 1.5s burst — temporarily raise scroll speed + ship speed (juice)
    }

    void Update()
    {
        if (_boostCooldown > 0) _boostCooldown -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.Space)) _beam.SetBeam(true);
        if (Input.GetKeyUp(KeyCode.Space)) _beam.SetBeam(false);
    }

    void Refresh()
    {
        var gm = GameManager.I;
        int total = gm.Catalog.specimens.Length - 1; // minus sealed record
        specimenLabel.text = $"SPECIMENS {SaveData.CollectedCount(gm.Catalog.specimens)}/{total}";
        streakLabel.text = gm.streak > 1 ? $"RESEARCH STREAK x{gm.streak}" : "";
        scoreLabel.text = gm.score.ToString("N0");
        hullLabel.text = new string('\u2665', Mathf.Max(0, gm.hull)); // hearts
        wantedLabel.text = "WANTED " + new string('\u25A0', gm.wantedLevel) + new string('\u25A1', 5 - gm.wantedLevel);

        if (titlePanel)    titlePanel.SetActive(gm.State == GameState.Title);
        if (gameOverPanel) gameOverPanel.SetActive(gm.State == GameState.GameOver);
    }
}
