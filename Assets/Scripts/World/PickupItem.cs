using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public Sprite icon;
    public virtual void OnPickedUp(PlayerInventory inventory) { }
    public virtual void OnDropped() { }
}
