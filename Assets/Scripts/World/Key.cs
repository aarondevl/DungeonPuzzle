using UnityEngine;

public class Key : PickupItem
{
    [SerializeField] Door linkedDoor;
    [SerializeField] AudioClip pickupSfx;
    [SerializeField] float pickupSfxVolume = 1f;

    public override void OnPickedUp(PlayerInventory inventory)
    {
        // Feedback visual: se lanza ANTES de que PlayerInventory desactive la llave.
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) PickupPop.Spawn(sr);
        Vfx.Spark(transform.position);

        // Feedback sonoro (si hay un clip asignado).
        if (pickupSfx != null && AudioMaster.Instance != null)
            AudioMaster.Instance.PlaySFX(pickupSfx, pickupSfxVolume);

        linkedDoor.Open();
    }
}
