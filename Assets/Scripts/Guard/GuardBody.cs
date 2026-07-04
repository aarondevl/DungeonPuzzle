using UnityEngine;

/// <summary>
/// Visual de guardia para sprites de PERFIL (side-view): reproduce idle/walk por
/// código (flipbook) y voltea en X según hacia dónde mira, en vez de usar hojas
/// de 4 direcciones. La dirección real la sigue comunicando el cono de visión.
///
/// Reutiliza <see cref="GuardVisual.ComputeFacing"/> para decidir el "facing" a
/// partir de la velocidad o de la rotación del cuerpo, y contrarrota igual que
/// <see cref="GuardVisual"/> para que el cuerpo se vea de pie mientras el
/// Rigidbody2D del padre (y su cono) rotan.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GuardBody : MonoBehaviour
{
    [SerializeField] Rigidbody2D body;
    [SerializeField] Sprite[] idleFrames;
    [SerializeField] Sprite[] walkFrames;
    [SerializeField] float fps = 8f;
    [SerializeField] float walkThreshold = 0.05f;
    [Tooltip("Los sprites miran a la DERECHA de forma nativa. Marca si miran a la izquierda.")]
    [SerializeField] bool spritesFaceLeft = false;

    SpriteRenderer _sr;
    Vector2 _lastPos;
    float _timer;
    int _frame;
    float _lastFacingX = 1f;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        if (body == null) body = GetComponentInParent<Rigidbody2D>();
        _lastPos = body != null ? (Vector2)body.transform.position : (Vector2)transform.position;
    }

    void LateUpdate()
    {
        // Contra-rotación: el cuerpo siempre de pie aunque el padre rote.
        transform.rotation = Quaternion.identity;

        Vector2 pos = body != null ? (Vector2)body.transform.position : (Vector2)transform.position;
        Vector2 vel = (pos - _lastPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        _lastPos = pos;

        bool moving = vel.magnitude > walkThreshold;

        Vector2 facing = GuardVisual.ComputeFacing(vel, body != null ? body.rotation : 0f, walkThreshold);
        if (Mathf.Abs(facing.x) > 0.01f) _lastFacingX = facing.x;
        bool faceLeft = _lastFacingX < 0f;
        _sr.flipX = spritesFaceLeft ? !faceLeft : faceLeft;

        Sprite[] frames = moving && walkFrames != null && walkFrames.Length > 0 ? walkFrames : idleFrames;
        if (frames == null || frames.Length == 0) return;

        _timer += Time.deltaTime;
        float step = 1f / Mathf.Max(1f, fps);
        while (_timer >= step) { _timer -= step; _frame++; }
        _sr.sprite = frames[_frame % frames.Length];
    }
}
