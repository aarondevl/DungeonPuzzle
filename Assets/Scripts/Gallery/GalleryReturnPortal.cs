using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public sealed class GalleryReturnPortal : MonoBehaviour
{
    [SerializeField] Transform destination;
    [SerializeField, Min(0f)] float cooldownSeconds = 0.35f;
    float _lastUse = float.NegativeInfinity;

    public void Configure(Transform target, float cooldown)
    {
        destination = target;
        cooldownSeconds = Mathf.Max(0f, cooldown);
    }

    public bool TryTeleport(Collider2D playerCollider, float now)
    {
        if (!GalleryPlayerContact.IsPhysicalPlayer(playerCollider) || destination == null ||
            now - _lastUse < cooldownSeconds) return false;

        _lastUse = now;
        var body = playerCollider.attachedRigidbody;
        if (body != null)
        {
            body.position = destination.position;
            body.linearVelocity = Vector2.zero;
        }
        else
        {
            playerCollider.transform.position = destination.position;
        }
        return true;
    }

    void Awake() => GetComponent<Collider2D>().isTrigger = true;
    void OnTriggerEnter2D(Collider2D other) => TryTeleport(other, Time.unscaledTime);
}
