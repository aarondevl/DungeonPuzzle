using UnityEngine;

/// <summary>
/// Llave de una puerta. Al recogerla no abre la puerta "por arte de magia": una copia
/// de la llave vuela en arco hasta la puerta y es al llegar cuando esta se abre, con
/// chispa y una sacudida corta de cámara. Así el jugador ve la relación llave → puerta.
/// </summary>
public class Key : PickupItem
{
    [SerializeField] Door linkedDoor;
    [SerializeField] AudioClip pickupSfx;
    [SerializeField] float pickupSfxVolume = 1f;
    [Tooltip("Segundos que tarda la llave en volar hasta su puerta.")]
    [SerializeField] float flightSeconds = 0.6f;

    /// <summary>La llave abre su puerta al recogerla; no tiene sentido conservarla.</summary>
    public override bool ConsumedOnPickup => true;

    public override void OnPickedUp(PlayerInventory inventory)
    {
        // Feedback visual: se lanza ANTES de que PlayerInventory desactive la llave.
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) PickupPop.Spawn(sr);
        Vfx.Spark(transform.position);
        inventory.GetComponent<PlayerFeedback>()?.Punch();

        // Feedback sonoro (si hay un clip asignado).
        if (pickupSfx != null && AudioMaster.Instance != null)
            AudioMaster.Instance.PlaySFX(pickupSfx, pickupSfxVolume);

        if (linkedDoor == null) return;
        Door door = linkedDoor;
        var flyer = FlyingSprite.Launch(sr, door.transform.position, flightSeconds, () =>
        {
            if (door == null) return;
            CameraShake.Kick(0.18f);
            door.Open();
        });
        if (flyer == null) door.Open();
    }
}
