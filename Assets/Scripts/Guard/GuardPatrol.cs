using UnityEngine;

/// <summary>
/// Guardia que recorre una lista de puntos (waypoints). Al oír un ruido va a
/// investigar el punto; al ver al héroe lo persigue (eso lo hace <see cref="GuardBase"/>)
/// y, si lo pierde, retoma su ruta por el waypoint más cercano.
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
    [Tooltip("Distancia a la que se detiene al investigar un ruido.")]
    [SerializeField] float investigateRadius = 0.4f;

    int _index;
    Vector2 _alertTarget;
    bool _hasAlertTarget;

    /// <summary>Punto que está investigando (solo válido mientras está alertado).</summary>
    public Vector2 AlertTarget => _alertTarget;

    protected override void OnFixedUpdate()
    {
        switch (State)
        {
            case GuardState.Alerted: Investigate(); break;
            case GuardState.Returning: ReturnToRoute(); break;
            default: Patrol(); break;
        }
    }

    void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        Vector2 target = waypoints[_index].position;
        Vector2 toTarget = target - Rb.position;                 // vector hasta el waypoint

        MoveToward(target, moveSpeed);
        FaceDirection(toTarget, turnSpeed);

        if (toTarget.magnitude < waypointRadius)
            _index = (_index + 1) % waypoints.Length;
    }

    void Investigate()
    {
        if (!_hasAlertTarget) return;
        Vector2 toTarget = _alertTarget - Rb.position;
        // Ya está encima del punto: se queda mirando hacia él sin vibrar alrededor.
        if (toTarget.magnitude <= investigateRadius) return;
        MoveToward(_alertTarget, moveSpeed);
        FaceDirection(toTarget, turnSpeed);
    }

    /// <summary>Tras una persecución, camina hasta el waypoint más cercano y retoma la ruta.</summary>
    void ReturnToRoute()
    {
        if (waypoints == null || waypoints.Length == 0) { FinishReturn(); return; }
        Vector2 target = waypoints[_index].position;
        MoveToward(target, moveSpeed);
        FaceDirection(target - Rb.position, turnSpeed);
        if (Arrived(target, waypointRadius * 3f)) FinishReturn();
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

    protected override void OnStartReturning()
    {
        // Elige el waypoint más cercano para no cruzar media sala de vuelta.
        if (waypoints == null || waypoints.Length == 0) { FinishReturn(); return; }
        float best = float.MaxValue;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;
            float d = VectorMath.Distance(Rb.position, waypoints[i].position);
            if (d < best) { best = d; _index = i; }
        }
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
