using UnityEngine;

public class GuardPatrol : GuardBase
{
    [SerializeField] Transform[] waypoints;
    [SerializeField] float moveSpeed = 2f;

    int _index;
    Vector2 _alertTarget;

    void Update()
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
        MoveToward(waypoints[_index].position);
        FaceDirection((Vector2)waypoints[_index].position - (Vector2)transform.position);

        if (Vector2.Distance(transform.position, waypoints[_index].position) < 0.1f)
            _index = (_index + 1) % waypoints.Length;
    }

    void MoveToward(Vector2 target)
    {
        transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
    }

    void FaceDirection(Vector2 dir)
    {
        if (dir == Vector2.zero) return;
        float angle = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, -angle);
    }

    protected override void OnNoiseAlerted(Vector2 position)
    {
        _alertTarget = position;
    }

    protected override void OnReturnToNormal()
    {
        _alertTarget = Vector2.zero;
    }
}
