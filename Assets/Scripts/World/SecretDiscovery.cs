using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class SecretDiscovery : MonoBehaviour
{
    [SerializeField] string secretId = "room_04_passage";
    [SerializeField] GameObject revealEffect;
    bool _used;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_used || !other.CompareTag("Player")) return;
        _used = true;
        bool firstDiscovery = !GameProgress.IsSecretDiscovered(secretId);
        GameProgress.DiscoverSecret(secretId);
        if (firstDiscovery)
        {
            SfxLibrary.Play("SFX/key_pickup", 0.45f);
            if (revealEffect != null) revealEffect.SetActive(true);
            Vfx.Spark(transform.position);
        }
    }
}
