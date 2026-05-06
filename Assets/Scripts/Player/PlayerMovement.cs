using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float speed = 4f;
    [SerializeField] float acceleration = 30f;

    Rigidbody2D _rb;
    Animator _animator;
    Vector2 _velocity;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponentInChildren<Animator>();
    }

    void FixedUpdate()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        float h = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f)
                - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
        float v = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f)
                - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
        Vector2 target = new Vector2(h, v).normalized * speed;
        _velocity = StepVelocity(_velocity, target, acceleration, Time.fixedDeltaTime);
        _rb.MovePosition(_rb.position + _velocity * Time.fixedDeltaTime);

        if (_animator != null) _animator.SetFloat("Speed", _velocity.magnitude);
    }

    public static Vector2 StepVelocity(Vector2 current, Vector2 target, float acceleration, float deltaTime)
    {
        float maxDelta = acceleration * deltaTime;
        return Vector2.MoveTowards(current, target, maxDelta);
    }
}
