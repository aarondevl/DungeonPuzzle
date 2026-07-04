using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ThrownStone : MonoBehaviour
{
    [SerializeField] float speed = 8f;
    [SerializeField] LayerMask noiseTriggerLayers;

    Rigidbody2D _rb;

    void Awake() => _rb = GetComponent<Rigidbody2D>();

    public void Launch(Vector2 direction) =>
        _rb.linearVelocity = direction.normalized * speed;

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!ShouldTriggerNoise(noiseTriggerLayers.value, col.gameObject.layer)) return;
        SfxLibrary.Play("SFX/stone_land");
        Vfx.Spark(transform.position);
        GetComponent<NoiseSource>().TriggerNoise();
    }

    public static bool ShouldTriggerNoise(int mask, int layer) =>
        (mask & (1 << layer)) != 0;
}
