using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [SerializeField] float defaultDuration = 0.35f;
    [SerializeField] float defaultStrength = 0.25f;
    [SerializeField] float frequency = 28f;

    Vector3 _basePos;
    float _trauma;
    float _seedX, _seedY;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        _basePos = transform.localPosition;
        _seedX = Random.value * 1000f;
        _seedY = Random.value * 1000f;
    }

    void OnDestroy() { if (Instance == this) Instance = null; }

    void LateUpdate()
    {
        if (_trauma <= 0f)
        {
            transform.localPosition = _basePos;
            return;
        }
        float shake = _trauma * _trauma;
        float t = Time.unscaledTime * frequency;
        float ox = (Mathf.PerlinNoise(_seedX, t) * 2f - 1f) * shake;
        float oy = (Mathf.PerlinNoise(_seedY, t) * 2f - 1f) * shake;
        transform.localPosition = _basePos + new Vector3(ox, oy, 0f);
        _trauma = Mathf.Max(0f, _trauma - Time.unscaledDeltaTime / Mathf.Max(0.01f, defaultDuration));
    }

    public static void Kick(float strength = -1f)
    {
        if (Instance == null) return;
        float s = strength < 0f ? Instance.defaultStrength : strength;
        Instance._trauma = Mathf.Min(1f, Instance._trauma + s);
    }
}
