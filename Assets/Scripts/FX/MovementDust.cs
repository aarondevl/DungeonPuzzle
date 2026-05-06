using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MovementDust : MonoBehaviour
{
    [SerializeField] ParticleSystem dust;
    [SerializeField] float minSpeed = 0.6f;

    Rigidbody2D _rb;

    void Awake() { _rb = GetComponent<Rigidbody2D>(); }

    void Update()
    {
        if (dust == null) return;
        bool moving = _rb.linearVelocity.sqrMagnitude > minSpeed * minSpeed;
        var em = dust.emission;
        em.enabled = moving;
    }
}
