using UnityEngine;

public class GuardStatic : GuardBase
{
    [SerializeField] float rotationSpeed = 30f;
    [SerializeField] float maxAngle = 45f;

    float _baseAngle;
    float _time;

    protected override void Awake()
    {
        base.Awake();
        _baseAngle = transform.eulerAngles.z;
    }

    void FixedUpdate()
    {
        if (State == GuardState.Alerted) return;
        _time += Time.fixedDeltaTime * rotationSpeed * Mathf.Deg2Rad;
        float offset = Mathf.Sin(_time) * maxAngle;
        Rb.MoveRotation(_baseAngle + offset);
    }
}
