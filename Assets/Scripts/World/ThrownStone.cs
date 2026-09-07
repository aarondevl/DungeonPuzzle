using UnityEngine;

/// <summary>
/// Piedra en vuelo. Es el objeto con la respuesta de colisión más delicada del juego:
/// es pequeño (r = 0.1) y rápido (8 u/s), así que con detección discreta atravesaba
/// muros finos entre dos pasos de física ("tunneling").
///
/// Mejoras de la Semana 04:
///   * <see cref="CollisionDetectionMode2D.Continuous"/> + interpolación: se fuerzan
///     en Awake para que no dependan de que el prefab esté bien configurado.
///   * La piedra se lanza desde el propio jugador, así que se ignora explícitamente
///     el collider del lanzador durante el disparo (además de la matriz de capas).
///   * Rebote con pérdida de energía y ruido solo en las superficies "sólidas":
///     el impacto se resuelve con la normal del contacto, no teletransportando.
///   * Vida máxima: una piedra que nunca golpea una superficie válida ya no queda
///     rebotando para siempre en la escena.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ThrownStone : MonoBehaviour
{
    [SerializeField] float speed = 8f;
    [SerializeField] LayerMask noiseTriggerLayers;
    [Tooltip("Fracción de la velocidad que conserva en cada rebote antes de detenerse.")]
    [SerializeField, Range(0f, 1f)] float bounciness = 0.35f;
    [Tooltip("Segundos de vuelo antes de autodestruirse si no impacta nada válido.")]
    [SerializeField] float maxLifetime = 6f;

    Rigidbody2D _rb;
    Collider2D _col;
    float _age;
    bool _spent;

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
        if (_age >= maxLifetime) Destroy(gameObject);
    }

    public void Launch(Vector2 direction) =>
        _rb.linearVelocity = direction.normalized * speed;

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
        if (_spent) return;
        _spent = true;

        SfxLibrary.Play("SFX/stone_land");
        Vfx.Spark(transform.position);
        var noise = GetComponent<NoiseSource>();
        if (noise != null) noise.TriggerNoise();   // TriggerNoise destruye el GameObject
        else Destroy(gameObject);
    }

    public static bool ShouldTriggerNoise(int mask, int layer) =>
        CollisionLayers.Contains(mask, layer);

    /// <summary>Reflexión de la velocidad sobre la normal del contacto, con pérdida de energía.</summary>
    public static Vector2 Bounce(Vector2 velocity, Vector2 normal, float bounciness) =>
        Vector2.Reflect(velocity, normal.normalized) * Mathf.Clamp01(bounciness);
}
