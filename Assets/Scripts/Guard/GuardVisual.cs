using UnityEngine;

/// <summary>
/// Visual del guardia: anima idle/walk 4-direcciones y contra-rota para que el
/// cuerpo se vea de pie mientras el Rigidbody2D del padre (y su cono de visión)
/// rotan con MoveRotation. La ROTACIÓN visible del guardia la da el cono.
/// </summary>
[RequireComponent(typeof(Animator))]
public class GuardVisual : MonoBehaviour
{
    [SerializeField] Rigidbody2D body;           // rigidbody del guardia (padre)
    [SerializeField] float walkThreshold = 0.05f;

    Animator _animator;
    Vector2 _lastPos;

    void Awake()
    {
        _animator = GetComponent<Animator>();
        if (body == null) body = GetComponentInParent<Rigidbody2D>();
        _lastPos = body.transform.position;
    }

    void LateUpdate()
    {
        // Contra-rotación: anula la rotación heredada del padre.
        transform.rotation = Quaternion.identity;

        // Posición del TRANSFORM (interpolada por el rigidbody), no rb.position:
        // rb.position solo cambia en FixedUpdate y hacía que Speed cayera a 0
        // entre pasos de física => la animación de caminar no se disparaba.
        Vector2 pos = body.transform.position;
        Vector2 vel = (pos - _lastPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        _lastPos = pos;

        Vector2 facing = ComputeFacing(vel, body.rotation, walkThreshold);
        _animator.SetFloat("MoveX", facing.x);
        _animator.SetFloat("MoveY", facing.y);
        _animator.SetFloat("Speed", vel.magnitude);
    }

    /// <summary>Si se mueve, mira hacia la velocidad; si no, hacia donde apunta el cuerpo.</summary>
    public static Vector2 ComputeFacing(Vector2 velocity, float bodyRotationDeg, float walkThreshold)
    {
        Vector2 dir = velocity.magnitude > walkThreshold
            ? velocity
            : RotationToDirection(bodyRotationDeg);
        if (dir == Vector2.zero) return Vector2.down;
        return Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)
            ? new Vector2(Mathf.Sign(dir.x), 0f)
            : new Vector2(0f, Mathf.Sign(dir.y));
    }

    /// <summary>Inversa de GuardPatrol.FaceDirection: 0° = arriba, -90° = derecha.</summary>
    public static Vector2 RotationToDirection(float rotationDeg)
    {
        float rad = rotationDeg * Mathf.Deg2Rad;
        return new Vector2(-Mathf.Sin(rad), Mathf.Cos(rad));
    }
}
