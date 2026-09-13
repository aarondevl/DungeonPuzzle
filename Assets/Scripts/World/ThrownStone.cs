using UnityEngine;

/// <summary>
/// Piedra en vuelo. Dos efectos, los dos visibles:
///   * Si alcanza a un GUARDIA lo aturde unos segundos (estrellas, cono apagado).
///   * Si cae contra un muro u otra superficie sólida hace RUIDO: un anillo muestra
///     hasta dónde llega y los guardias que lo oyen van a mirar con un "?".
/// Después la piedra queda en el suelo y se puede volver a recoger.
///
/// Es pequeña (r = 0.1) y rápida (8 u/s): detección continua e interpolación
/// forzadas en Awake para que no atraviese muros finos. La capa Projectile no
/// choca con Guard en la matriz de físicas, así que el impacto con guardias se
/// comprueba por solape cada paso.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ThrownStone : MonoBehaviour
{
    [SerializeField] float speed = 8f;
    [SerializeField] LayerMask noiseTriggerLayers;
    [Tooltip("Fracción de la velocidad que conserva en cada rebote antes de detenerse.")]
    [SerializeField, Range(0f, 1f)] float bounciness = 0.35f;
    [Tooltip("Segundos de vuelo antes de caer sola si no impacta nada.")]
    [SerializeField] float maxLifetime = 6f;
    [Tooltip("Radio con el que golpea a un guardia.")]
    [SerializeField] float hitRadius = 0.35f;

    Rigidbody2D _rb;
    Collider2D _col;
    float _age;
    bool _spent;
    Stone _item;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _col = GetComponent<Collider2D>();

        gameObject.layer = CollisionLayers.Projectile;
        _rb.gravityScale = 0f;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void Update()
    {
        _age += Time.deltaTime;
        if (_age >= maxLifetime) Land(false);
    }

    void FixedUpdate()
    {
        if (_spent) return;
        var guard = Physics2D.OverlapCircle(_rb.position, hitRadius, CollisionLayers.GuardMask);
        if (guard == null) return;
        var g = guard.GetComponentInParent<GuardBase>();
        if (g == null) return;
        g.Stun();
        Vfx.FloatingText(g.transform.position + Vector3.up * 1.2f, "¡ATURDIDO!", new Color(1f, 0.95f, 0.5f, 1f), 1.2f, 5f);
        Land(false);
    }

    /// <param name="item">La piedra recogible que se lanzó; se deja en el suelo al caer.</param>
    public void Launch(Vector2 direction, Stone item = null)
    {
        _item = item;
        _rb.linearVelocity = direction.normalized * speed;
    }

    /// <summary>Evita que la piedra empuje a quien la lanza al aparecer sobre él.</summary>
    public void IgnoreThrower(Collider2D thrower)
    {
        if (thrower != null) Physics2D.IgnoreCollision(_col, thrower, true);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        int mask = CollisionLayers.Resolve(noiseTriggerLayers, CollisionLayers.NoiseSurfacesMask);
        if (!ShouldTriggerNoise(mask, col.gameObject.layer))
        {
            // Superficie que no genera ruido: rebota y sigue volando.
            _rb.linearVelocity = Bounce(_rb.linearVelocity, col.GetContact(0).normal, bounciness);
            return;
        }
        Land(true);
    }

    /// <summary>La piedra termina su vuelo: ruido (si toca superficie sólida) y vuelve a ser recogible.</summary>
    void Land(bool makeNoise)
    {
        if (_spent) return;
        _spent = true;

        Vector3 restPos = transform.position;
        if (makeNoise)
        {
            SfxLibrary.Play("SFX/stone_land");
            Vfx.Spark(restPos);
            var noise = GetComponent<NoiseSource>();
            if (noise != null) noise.TriggerNoise();       // TriggerNoise destruye este objeto
        }

        // Deja la piedra en el suelo, un poco separada del muro, para recogerla otra vez.
        if (_item != null)
        {
            Vector2 back = -_rb.linearVelocity.normalized * 0.35f;
            _item.transform.position = restPos + (Vector3)back;
            _item.gameObject.SetActive(true);
        }
        if (!makeNoise) Destroy(gameObject);
    }

    public static bool ShouldTriggerNoise(int mask, int layer) =>
        CollisionLayers.Contains(mask, layer);

    /// <summary>Reflexión de la velocidad sobre la normal del contacto, con pérdida de energía.</summary>
    public static Vector2 Bounce(Vector2 velocity, Vector2 normal, float bounciness) =>
        Vector2.Reflect(velocity, normal.normalized) * Mathf.Clamp01(bounciness);
}
