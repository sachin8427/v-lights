using UnityEngine;

// Procedural audio — no audio files needed.
// All sounds are synthesized from oscillator sweeps, ported from the HTML prototype's
// WebAudio T(freq1, freq2, duration, waveType, volume) pattern.
public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    // Auto-create before any scene loads so SceneBuilder doesn't need to wire this
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (I != null) return;
        var go = new GameObject("AudioManager");
        go.AddComponent<AudioManager>();
        DontDestroyOnLoad(go);
    }

    AudioSource _sfxSource;
    AudioSource _beamSource;

    AudioClip _captureClip;
    AudioClip _damageClip;
    AudioClip _gameOverClip;
    AudioClip _streakClip;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;

        _sfxSource = gameObject.AddComponent<AudioSource>();
        _sfxSource.playOnAwake = false;
        _sfxSource.volume = 1f;

        _beamSource = gameObject.AddComponent<AudioSource>();
        _beamSource.playOnAwake = false;
        _beamSource.loop = true;
        _beamSource.volume = 0.55f;

        // Volumes are significantly higher than the HTML prototype values because
        // Unity's mixer sits at a lower default gain than WebAudio's destination node
        _captureClip  = MakeClip(320f,  980f,  0.30f, Wave.Sine,     0.70f);
        _damageClip   = MakeClip(160f,  40f,   0.35f, Wave.Sawtooth, 0.80f);
        _gameOverClip = MakeClip(90f,   30f,   0.50f, Wave.Square,   0.60f);
        _streakClip   = MakeClip(660f,  1320f, 0.25f, Wave.Triangle, 0.55f);

        // Beam hum: two detuned triangle oscillators for a beating effect,
        // with cross-fade edges so the loop is seamless
        _beamSource.clip = MakeBeamClip(280f, 284f, 1.0f);
    }

    public void PlayCapture()  => _sfxSource.PlayOneShot(_captureClip);
    public void PlayDamage()   => _sfxSource.PlayOneShot(_damageClip);
    public void PlayGameOver() => _sfxSource.PlayOneShot(_gameOverClip);
    public void PlayStreak()   => _sfxSource.PlayOneShot(_streakClip);

    public void SetBeamAudio(bool on)
    {
        if (on && !_beamSource.isPlaying) _beamSource.Play();
        else if (!on && _beamSource.isPlaying) _beamSource.Stop();
    }

    // --- Synthesis ---

    enum Wave { Sine, Triangle, Sawtooth, Square }

    // Sustained looping beam hum: two slightly detuned triangle waves.
    // Cross-fades at edges so the loop clicks-free.
    static AudioClip MakeBeamClip(float freq1, float freq2, float duration)
    {
        const int sampleRate = 44100;
        int n = (int)(sampleRate * duration);
        float[] data = new float[n];
        float p1 = 0f, p2 = 0f;
        float fade = 0.04f; // 40ms fade in/out for seamless loop

        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sampleRate;
            p1 += 2f * Mathf.PI * freq1 / sampleRate;
            p2 += 2f * Mathf.PI * freq2 / sampleRate;

            float s1 = Triangle(p1);
            float s2 = Triangle(p2);

            float env = 1f;
            if (t < fade)              env = t / fade;
            else if (t > duration - fade) env = (duration - t) / fade;

            data[i] = (s1 + s2) * 0.5f * 0.75f * env;
        }

        var clip = AudioClip.Create("beam_hum", n, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    // Sweep from freq1 to freq2 with a punchy attack and full decay
    static AudioClip MakeClip(float freq1, float freq2, float duration, Wave wave, float volume)
    {
        const int sampleRate = 44100;
        int n = Mathf.Max(1, (int)(sampleRate * duration));
        float[] data = new float[n];
        float phase = 0f;
        const float attackSec = 0.008f;

        for (int i = 0; i < n; i++)
        {
            float t        = (float)i / sampleRate;
            float progress = (float)i / n;

            float freq = freq1 * Mathf.Pow(Mathf.Max(0.001f, freq2 / freq1), progress);
            phase += 2f * Mathf.PI * freq / sampleRate;

            float s = wave switch
            {
                Wave.Sine     => Mathf.Sin(phase),
                Wave.Triangle => Triangle(phase),
                Wave.Sawtooth => Sawtooth(phase),
                Wave.Square   => Mathf.Sin(phase) >= 0f ? 1f : -1f,
                _             => 0f,
            };

            // Sharp attack, hold briefly, then decay
            float sustainEnd = duration * 0.15f;
            float env;
            if (t < attackSec)        env = t / attackSec;
            else if (t < sustainEnd)  env = 1f;
            else                      env = Mathf.Max(0f, 1f - (t - sustainEnd) / (duration - sustainEnd));

            data[i] = s * volume * env;
        }

        var clip = AudioClip.Create("sfx", n, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float Triangle(float phase) =>
        1f - 4f * Mathf.Abs(Mathf.Round(phase / (2f * Mathf.PI)) - phase / (2f * Mathf.PI));

    static float Sawtooth(float phase) =>
        2f * (phase / (2f * Mathf.PI) - Mathf.Floor(0.5f + phase / (2f * Mathf.PI)));
}
