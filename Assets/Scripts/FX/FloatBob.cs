using UnityEngine;

public class FloatBob : MonoBehaviour
{
    [SerializeField] float amplitude = 0.06f;
    [SerializeField] float frequency = 1.6f;
    [SerializeField] float spinDegPerSec = 0f;

    Vector3 _baseLocal;
    float _phase;

    void Awake()
    {
        _baseLocal = transform.localPosition;
        _phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * frequency * Mathf.PI * 2f + _phase) * amplitude;
        transform.localPosition = _baseLocal + new Vector3(0f, y, 0f);
        if (spinDegPerSec != 0f)
            transform.Rotate(0f, 0f, spinDegPerSec * Time.deltaTime, Space.Self);
    }
}
