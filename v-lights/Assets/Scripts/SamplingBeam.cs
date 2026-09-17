using UnityEngine;

// The sampling beam: a code-generated trapezoid mesh, amber and additive.
// Hold the BEAM button (or Space) to activate. Specimens inside the cone
// accumulate capture progress based on their beamTime.
public class SamplingBeam : MonoBehaviour
{
    [Header("Beam shape")]
    public float length = 6f;
    public float topWidth = 0.5f;
    public float bottomWidth = 2.2f;

    [Header("Tuning")]
    public Color beamColor = new Color(1f, 0.72f, 0.25f, 0.45f);

    public bool Active { get; private set; }

    MeshFilter _mf;
    MeshRenderer _mr;
    float _flicker;

    // IAP hooks (set by IAPManager / SaveData)
    public float WidthMultiplier => (IAPManager.I != null && IAPManager.I.Owns(IAPManager.GoldenBeamId)) ? 1.5f : 1f;
    public float SpeedMultiplier => (IAPManager.I != null && IAPManager.I.Owns(IAPManager.GoldenBeamId)) ? 1.35f : 1f;

    void Awake()
    {
        _mf = gameObject.AddComponent<MeshFilter>();
        _mr = gameObject.AddComponent<MeshRenderer>();
        _mr.material = new Material(Shader.Find("Sprites/Default"));
        _mr.material.color = beamColor;
        // Hide the beam mesh only — never disable the GameObject itself, since
        // SamplingBeam lives on Player (RequireComponent) and disabling the GO
        // would disable the whole player and break FindObjectOfType in Spawner.
        _mr.enabled = false;
    }

    public void SetBeam(bool on)
    {
        if (!GameManager.I.IsPlaying) on = false;
        Active = on;
        _mr.enabled = on;
        if (on) BuildMesh();
        AudioManager.I?.SetBeamAudio(on);
    }

    void Update()
    {
        if (!Active) return;
        // keyboard fallback
        if (Input.GetKeyUp(KeyCode.Space)) SetBeam(false);
        _flicker += Time.deltaTime * 20f;
        BuildMesh(); // cheap enough; gives the shimmering cone
    }

    void BuildMesh()
    {
        float wTop = topWidth * WidthMultiplier * 0.5f;
        float wBot = bottomWidth * WidthMultiplier * 0.5f * (1f + 0.05f * Mathf.Sin(_flicker));
        var mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(-wTop, 0, 0), new Vector3(wTop, 0, 0),
            new Vector3(-wBot, -length, 0), new Vector3(wBot, -length, 0),
        };
        mesh.triangles = new[] { 0, 2, 1, 1, 2, 3 };
        mesh.uv = new Vector2[] { new(0,1), new(1,1), new(0,0), new(1,0) };
        mesh.RecalculateNormals();
        _mf.mesh = mesh;
    }

    // World-space check: is this point inside the beam cone?
    public bool Contains(Vector2 worldPoint)
    {
        Vector2 origin = transform.position;
        float dy = origin.y - worldPoint.y;
        if (dy < 0 || dy > length) return false;
        float t = dy / length;
        float half = Mathf.Lerp(topWidth, bottomWidth, t) * 0.5f * WidthMultiplier;
        return Mathf.Abs(worldPoint.x - origin.x) <= half;
    }
}
