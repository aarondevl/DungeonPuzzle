using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class GlowPulse : MonoBehaviour
{
    [SerializeField] Color baseColor = Color.white;
    [SerializeField] Color peakColor = new Color(1.4f, 1.2f, 0.6f, 1f);
    [SerializeField] float frequency = 1.4f;

    SpriteRenderer _sr;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        baseColor = _sr.color;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * frequency * Mathf.PI * 2f) + 1f) * 0.5f;
        _sr.color = Color.Lerp(baseColor, peakColor, t);
    }
}
