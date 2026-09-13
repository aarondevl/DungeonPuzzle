using UnityEngine;

/// <summary>
/// Guardia que recorre una lista de puntos (waypoints) y, al alertarse, va a
/// investigar el punto donde vio al héroe u oyó el ruido.
///
/// Todo el movimiento se expresa con vectores:
///   dirección = normalizar(destino - posición)
///   paso      = dirección · rapidez · Δt
///   rotación  = ángulo del vector dirección (VectorMath.DirectionToAngle)
/// </summary>
public class GuardPatrol : GuardBase
{
    [SerializeField] Transform[] waypoints;
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float turnSpeed = 360f;
    [Tooltip("Distancia a la que se considera alcanzado un waypoint.")]
    [SerializeField] float waypointRadius = 0.1f;
    [Tooltip("Distancia a la que se detiene al investigar un ruido o una posición vista.")]
    [SerializeField] float investigateRadius = 0.4f;

    int _index;
    Vector2 _alertTarget;
    bool _hasAlertTarget;

    /// <summary>Punto que está investigando (solo válido mientras <see cref="GuardBase.IsAlerted"/>).</summary>
    public Vector2 AlertTarget => _alertTarget;

    void FixedUpdate()
    {
        if (State == GuardState.Alerted)
        {
            Investigate();
            return;
        }
        Patrol();
    }

    void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        Vector2 target = waypoints[_index].position;
        Vector2 toTarget = target - Rb.position;                 // vector hasta el waypoint

        MoveToward(target);
        FaceDirection(toTarget);

        if (toTarget.magnitude < waypointRadius)
            _index = (_index + 1) % waypoints.Length;
    }

    void Investigate()
    {
        if (!_hasAlertTarget) return;
        Vector2 toTarget = _alertTarget - Rb.position;
        // Ya está encima del punto: se queda mirando hacia él sin vibrar alrededor.
        if (toTarget.magnitude <= investigateRadius) return;
        MoveToward(_alertTarget);
        FaceDirection(toTarget);
    }

    void MoveToward(Vector2 target)
    {
        Vector2 next = VectorMath.StepTowards(Rb.position, target, moveSpeed, Time.fixedDeltaTime);
        Rb.MovePosition(next);
    }

    void FaceDirection(Vector2 dir)
    {
        if (dir == Vector2.zero) return;
        float target = VectorMath.DirectionToAngle(dir);
        float current = Rb.rotation;
        float maxTurn = turnSpeed * Time.fixedDeltaTime;
        if (Mathf.Abs(Mathf.DeltaAngle(current, target)) <= maxTurn)
            Rb.rotation = target;
        else
            Rb.MoveRotation(Mathf.MoveTowardsAngle(current, target, maxTurn));
    }

    void SetAlertTarget(Vector2 position)
    {
        _alertTarget = position;
        _hasAlertTarget = true;
    }

    protected override void OnVisionAlerted(Vector2 position) => SetAlertTarget(position);
    protected override void OnNoiseAlerted(Vector2 position) => SetAlertTarget(position);
    protected override void OnReturnToNormal()
    {
        _alertTarget = Vector2.zero;
        _hasAlertTarget = false;
    }

    void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            Gizmos.DrawWireSphere(waypoints[i].position, waypointRadius);
            var next = waypoints[(i + 1) % waypoints.Length];
            if (next != null) Gizmos.DrawLine(waypoints[i].position, next.position);
        }
        if (Application.isPlaying && _hasAlertTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _alertTarget);
            Gizmos.DrawWireSphere(_alertTarget, investigateRadius);
        }
    }
}
