using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Movimiento top-down del héroe.
///
/// Cambio de la Semana 04: se pasó de <c>Rigidbody2D.MovePosition</c> a fijar
/// <c>linearVelocity</c>. Con MovePosition el cuerpo se teletransporta cada paso de
/// física y el solver solo puede "empujarlo" hacia fuera después del solape, lo que
/// producía tirones al rozar un muro y esquinas donde el jugador se quedaba pegado.
/// Fijando la velocidad, el solver resuelve el contacto y el jugador DESLIZA a lo
/// largo de la pared, que es la respuesta de colisión esperada en un top-down.
///
/// Se refuerzan además tres ajustes que antes dependían del prefab:
/// interpolación (movimiento suave a 50 Hz de física), rotación congelada (un
/// choque en diagonal no debe hacer girar al héroe) y material sin fricción
/// (sin él, el roce contra un muro frena el deslizamiento).
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

    public Vector2 Facing => _facing;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();

        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        if (_rb.sharedMaterial == null)
            _rb.sharedMaterial = new PhysicsMaterial2D("PlayerSlide") { friction = 0f, bounciness = 0f };
    }

    void FixedUpdate()
    {
        Vector2 target = ReadInput() * speed;
        _velocity = StepVelocity(_velocity, target, acceleration, Time.fixedDeltaTime);
        _rb.linearVelocity = _velocity;

        if (target.sqrMagnitude > 0.0001f) _facing = SnapFacing(target);

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
        return new Vector2(h, v).normalized;
    }

    public static Vector2 StepVelocity(Vector2 current, Vector2 target, float acceleration, float deltaTime)
    {
        float maxDelta = acceleration * deltaTime;
        return Vector2.MoveTowards(current, target, maxDelta);
    }

    /// <summary>Redondea una dirección libre a las 4 direcciones cardinales de la animación.</summary>
    public static Vector2 SnapFacing(Vector2 direction)
    {
        if (direction == Vector2.zero) return Vector2.down;
        return Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0f)
            : new Vector2(0f, Mathf.Sign(direction.y));
    }
}
