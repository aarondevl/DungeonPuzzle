using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public PickupItem HeldItem { get; private set; }

    public bool TryPickUp(PickupItem item)
    {
        if (HeldItem != null) return false;
        HeldItem = item;
        item.OnPickedUp(this);
        item.gameObject.SetActive(false);
        return true;
    }

    public PickupItem TakeItem()
    {
        var item = HeldItem;
        HeldItem = null;
        return item;
    }

    public bool HasItem<T>() where T : PickupItem => HeldItem is T;
}
