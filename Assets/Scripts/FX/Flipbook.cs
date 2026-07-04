using UnityEngine;

/// <summary>
/// Reproduce una secuencia de <see cref="Sprite"/> sobre un <see cref="SpriteRenderer"/>
/// a una cadencia fija y (por defecto) se autodestruye al terminar. Efecto de una sola
/// pasada pensado para VFX puntuales (chispas, fogonazos), en la línea de <c>PickupPop</c>.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class Flipbook : MonoBehaviour
{
    [SerializeField] Sprite[] frames;
    [SerializeField] float fps = 24f;
    [SerializeField] bool loop = false;
    [SerializeField] bool destroyOnEnd = true;

    SpriteRenderer _sr;
    float _t;
    int _i;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (frames != null && frames.Length > 0) _sr.sprite = frames[0];
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;

        _t += Time.deltaTime;
        float step = 1f / Mathf.Max(1f, fps);
        while (_t >= step) { _t -= step; _i++; }

        if (!loop && _i >= frames.Length)
        {
            if (destroyOnEnd) { Destroy(gameObject); return; }
            _sr.sprite = frames[frames.Length - 1];
            return;
        }
        _sr.sprite = frames[loop ? _i % frames.Length : Mathf.Min(_i, frames.Length - 1)];
    }
}
