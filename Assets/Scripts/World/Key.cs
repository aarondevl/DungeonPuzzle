using UnityEngine;

public class Key : PickupItem
{
    [SerializeField] Door linkedDoor;

    public override void OnPickedUp(PlayerInventory inventory)
    {
        linkedDoor.Open();
    }
}
