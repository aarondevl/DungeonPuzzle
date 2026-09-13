using UnityEngine;

/// <summary>
/// Guardia fijo que barre con el cono a izquierda y derecha de su ángulo base.
/// Al oír un ruido o ver fugazmente al héroe, gira el cono hacia ese punto
/// (ángulo del vector guardia → punto) y, al calmarse, vuelve suavemente al
/// barrido en lugar de saltar de golpe.
/// </summary>
public class GuardStatic : GuardBase
{
    [SerializeField] float rotationSpeed = 30f;
    [SerializeField] float maxAngle = 45f;
    [Tooltip("Grados por segundo al girar hacia un ruido o al volver al barrido.")]
    [SerializeField] float turnSpeed = 180f;

    float _baseAngle;
    float _time;
    bool _hasLookTarget;
    Vector2 _lookTarget;
    bool _resuming;

    protected override void Awake()
    {
        base.Awake();
        _baseAngle = transform.eulerAngles.z;
    }

    void FixedUpdate()
    {
        float maxTurn = turnSpeed * Time.fixedDeltaTime;

        if (State == GuardState.Alerted)
        {
            if (!_hasLookTarget) return;
            Vector2 toTarget = _lookTarget - Rb.position;               // vector hacia el ruido
            if (toTarget.sqrMagnitude < 1e-6f) return;
            float target = VectorMath.DirectionToAngle(toTarget);
            Rb.MoveRotation(Mathf.MoveTowardsAngle(Rb.rotation, target, maxTurn));
            return;
        }

        float sweep = SweepAngle(_baseAngle, _time, maxAngle);
        if (_resuming)
        {
            // Volver al barrido girando, no saltando.
            float next = Mathf.MoveTowardsAngle(Rb.rotation, sweep, maxTurn);
            Rb.MoveRotation(next);
            if (Mathf.Abs(Mathf.DeltaAngle(next, sweep)) < 0.5f) _resuming = false;
            return;
        }

        _time += Time.fixedDeltaTime * rotationSpeed * Mathf.Deg2Rad;
        Rb.MoveRotation(SweepAngle(_baseAngle, _time, maxAngle));
    }

    /// <summary>Ángulo del barrido: base ± maxAngle siguiendo una senoide.</summary>
    public static float SweepAngle(float baseAngle, float time, float maxAngle) =>
        baseAngle + Mathf.Sin(time) * maxAngle;

    void LookAt(Vector2 point)
    {
        _lookTarget = point;
        _hasLookTarget = true;
    }

    protected override void OnVisionAlerted(Vector2 position) => LookAt(position);
    protected override void OnNoiseAlerted(Vector2 position) => LookAt(position);
    protected override void OnReturnToNormal()
    {
        _hasLookTarget = false;
        _resuming = true;
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !_hasLookTarget) return;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, _lookTarget);
    }
}
