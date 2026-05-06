using UnityEngine;

public class GuardPatrol : GuardBase
{
    [SerializeField] Transform[] waypoints;
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float turnSpeed = 360f;

    int _index;
    Vector2 _alertTarget;

    void FixedUpdate()
    {
        if (State == GuardState.Alerted)
        {
            MoveToward(_alertTarget);
            return;
        }
        Patrol();
    }

    void Patrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        Vector2 target = waypoints[_index].position;
        MoveToward(target);
        FaceDirection(target - Rb.position);

        if (Vector2.Distance(Rb.position, target) < 0.1f)
            _index = (_index + 1) % waypoints.Length;
    }

    void MoveToward(Vector2 target)
    {
        Vector2 next = Vector2.MoveTowards(Rb.position, target, moveSpeed * Time.fixedDeltaTime);
        Rb.MovePosition(next);
    }

    void FaceDirection(Vector2 dir)
    {
        if (dir == Vector2.zero) return;
        float target = -Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        float current = Rb.rotation;
        float next = Mathf.MoveTowardsAngle(current, target, turnSpeed * Time.fixedDeltaTime);
        Rb.MoveRotation(next);
    }

    protected override void OnNoiseAlerted(Vector2 position) => _alertTarget = position;
    protected override void OnReturnToNormal() => _alertTarget = Vector2.zero;
}
