using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ThrownStone : MonoBehaviour
{
    [SerializeField] float speed = 8f;

    Rigidbody2D _rb;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void Launch(Vector2 direction) =>
        _rb.linearVelocity = direction.normalized * speed;

    void OnCollisionEnter2D(Collision2D col)
    {
        GetComponent<NoiseSource>().TriggerNoise();
    }
}
