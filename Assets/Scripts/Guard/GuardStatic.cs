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

    void Update()
    {
        if (State == GuardState.Alerted) return;
        _time += Time.deltaTime * rotationSpeed * Mathf.Deg2Rad;
        float offset = Mathf.Sin(_time) * maxAngle;
        transform.rotation = Quaternion.Euler(0, 0, _baseAngle + offset);
    }
}
