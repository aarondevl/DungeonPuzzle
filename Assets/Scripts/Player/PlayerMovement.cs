using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Movimiento top-down del héroe, expresado con puntos y vectores:
///
///   entrada (teclas)  →  vector dirección unitario
///   dirección · rapidez  →  velocidad deseada (vector)
///   velocidad actual  →  se acerca a la deseada a ritmo de <c>acceleration</c>
///   posición nueva  =  posición + velocidad · Δt   (lo integra el Rigidbody2D)
///
/// Se fija <c>linearVelocity</c> en vez de usar MovePosition para que el solver
/// resuelva el contacto y el héroe DESLICE a lo largo de los muros. Se refuerzan
/// interpolación, rotación congelada y material sin fricción por si el prefab no
/// los trae.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float speed = 4f;
    [SerializeField] float acceleration = 30f;

    Rigidbody2D _rb;
    Animator _animator;
    Vector2 _velocity;
    Vector2 _facing = Vector2.down;

    /// <summary>Dirección cardinal hacia la que mira el héroe (vector unitario).</summary>
    public Vector2 Facing => _facing;

    /// <summary>Punto del mundo donde está el héroe (coordenadas x, y).</summary>
    public Vector2 Position => _rb != null ? _rb.position : (Vector2)transform.position;

    /// <summary>Vector velocidad real del cuerpo (unidades/segundo).</summary>
    public Vector2 Velocity => _rb != null ? _rb.linearVelocity : Vector2.zero;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();
        // Reacciones visibles (rebotes, captura, salida): se añade en caliente si el
        // prefab no lo trae, para no depender de editar la escena.
        if (GetComponent<PlayerFeedback>() == null) gameObject.AddComponent<PlayerFeedback>();

        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        if (_rb.sharedMaterial == null)
            _rb.sharedMaterial = new PhysicsMaterial2D("PlayerSlide") { friction = 0f, bounciness = 0f };
    }

    void FixedUpdate()
    {
        Vector2 direction = ReadInput();                       // vector unitario (o cero)
        Vector2 target = direction * speed;                    // velocidad deseada
        _velocity = StepVelocity(_velocity, target, acceleration, Time.fixedDeltaTime);
        _rb.linearVelocity = _velocity;

        if (direction != Vector2.zero) _facing = SnapFacing(direction);

        if (_animator != null)
        {
            _animator.SetFloat("MoveX", _facing.x);
            _animator.SetFloat("MoveY", _facing.y);
            // La velocidad REAL del cuerpo, no la deseada: al chocar contra un muro
            // el héroe deja de caminar en pantalla aunque el jugador siga pulsando.
            _animator.SetFloat("Speed", _rb.linearVelocity.magnitude);
        }
    }

    Vector2 ReadInput()
    {
        var kb = Keyboard.current;
        if (kb == null) return Vector2.zero;
        float h = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        float v = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
        // Normalizar evita que la diagonal (1,1) sea √2 veces más rápida que un eje.
        return new Vector2(h, v).normalized;
    }

    /// <summary>Acerca la velocidad actual a la deseada sin superar acceleration · Δt.</summary>
    public static Vector2 StepVelocity(Vector2 current, Vector2 target, float acceleration, float deltaTime)
    {
        float maxDelta = acceleration * deltaTime;
        return Vector2.MoveTowards(current, target, maxDelta);
    }

    /// <summary>Redondea una dirección libre a las 4 direcciones cardinales de la animación.</summary>
    public static Vector2 SnapFacing(Vector2 direction) =>
        VectorMath.ToCardinal(direction, Vector2.down);

    void OnDrawGizmosSelected()
    {
        // Vectores visibles en la vista de escena: amarillo = velocidad, cian = facing.
        Vector3 p = transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(p, p + (Vector3)Velocity * 0.25f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(p, p + (Vector3)_facing * 0.6f);
    }
}
