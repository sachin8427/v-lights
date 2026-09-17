using System;
using System.Collections;
using UnityEngine;

public enum GameState { Title, Playing, GameOver }

// Central orchestrator: state, score, streak, hull, wanted level, expedition progress.
public class GameManager : MonoBehaviour
{
    public static GameManager I { get; private set; }

    public GameState State { get; private set; } = GameState.Title;

    [Header("Run state (read-only in Inspector)")]
    public int score;
    public int streak;
    public int hull = 3;
    public int wantedLevel;          // 0-5 shields; drives hazard spawn rate
    public int specimensThisRun;
    public int specimensThisExpedition;

    public SpecimenCatalog Catalog { get; private set; }
    public ExpeditionData CurrentExpedition { get; private set; }

    public event Action OnHudDirty;
    public event Action<SpecimenData, int> OnSpecimenCaptured; // specimen, points awarded

    [Header("Data assets (assign in Inspector)")]
    public TextAsset specimenCatalogJson;
    public TextAsset expeditionsJson;

    ExpeditionList _expeditions;
    float _invulnTimer;
    float _playTimer;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        Catalog = SpecimenCatalog.Load(specimenCatalogJson);
        _expeditions = ExpeditionList.Load(expeditionsJson);
    }

    public void StartExpedition(int expeditionId)
    {
        CurrentExpedition = _expeditions.Get(expeditionId);
        score = 0; streak = 0; hull = 3;
        wantedLevel = 0; _playTimer = 0f;
        specimensThisRun = 0; specimensThisExpedition = 0;
        State = GameState.Playing;
        OnHudDirty?.Invoke();
    }

    public void CaptureSpecimen(SpecimenData data)
    {
        streak++;
        int mult = 1 + streak / 5;                 // every 5 streak steps, +1x
        int pts = data.points * mult;
        score += pts;
        specimensThisRun++;
        specimensThisExpedition++;
        wantedLevel = Mathf.Min(5, specimensThisRun / 2);  // attention grows as you sample
        SaveData.SetCollected(data.id);
        OnSpecimenCaptured?.Invoke(data, pts);
        OnHudDirty?.Invoke();

        AudioManager.I?.PlayCapture();
        if (streak > 0 && streak % 5 == 0) AudioManager.I?.PlayStreak();

        if (specimensThisExpedition >= CurrentExpedition.quota)
            CompleteExpedition();
    }

    public void BreakStreak() { streak = 0; OnHudDirty?.Invoke(); }

    public void Damage()
    {
        if (_invulnTimer > 0 || State != GameState.Playing) return;
        hull--;
        _invulnTimer = 1.2f;
        BreakStreak();
        AudioManager.I?.PlayDamage();
        StartCoroutine(CameraShake(0.25f, 0.18f));
        if (hull <= 0) GameOver();
        else OnHudDirty?.Invoke();
    }

    IEnumerator CameraShake(float duration, float magnitude)
    {
        var cam = Camera.main;
        if (cam == null) yield break;
        var origin = cam.transform.localPosition;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            float decay = 1f - t / duration;
            cam.transform.localPosition = origin + (Vector3)(UnityEngine.Random.insideUnitCircle * magnitude * decay);
            yield return null;
        }
        cam.transform.localPosition = origin;
    }

    void CompleteExpedition()
    {
        int next = CurrentExpedition.id + 1;
        bool hasMore = next <= _expeditions.expeditions.Length;
        bool unlocked = IAPManager.I != null && IAPManager.I.Owns(IAPManager.UnlockAllId);
        if (hasMore && (unlocked || next <= SaveData.UnlockedExpeditions + 1))
            SaveData.UnlockedExpeditions = Mathf.Max(SaveData.UnlockedExpeditions, next);
        GameOver(); // v1: expedition complete flows through the game-over screen ("Expedition Complete")
    }

    void GameOver()
    {
        State = GameState.GameOver;
        if (score > SaveData.HighScore) SaveData.HighScore = score;
        OnHudDirty?.Invoke();
        AudioManager.I?.PlayGameOver();
        StartCoroutine(CameraShake(0.4f, 0.25f));
    }

    void Update()
    {
        if (_invulnTimer > 0) _invulnTimer -= Time.deltaTime;

        if (State == GameState.Playing)
        {
            _playTimer += Time.deltaTime;
            // Time-based escalation: wanted level rises every 15s regardless of captures,
            // so even short expeditions show visible difficulty increase
            int timeBased = Mathf.Min(5, (int)(_playTimer / 10f));
            if (timeBased > wantedLevel)
            {
                wantedLevel = timeBased;
                OnHudDirty?.Invoke();
            }
        }

        bool tapped = Input.GetMouseButtonDown(0) ||
                      (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);

        if (State == GameState.Title && tapped)
            StartExpedition(1);

        // GameOver → Title is handled by the explicit "FLY AGAIN" button, not tap-anywhere,
        // so other buttons on the GameOver screen (e.g. GUIDE) don't conflict.
    }

    public void ReturnToTitle()
    {
        State = GameState.Title;
        OnHudDirty?.Invoke();
    }

    public bool IsPlaying => State == GameState.Playing;
    public bool IsInvulnerable => _invulnTimer > 0;
}
