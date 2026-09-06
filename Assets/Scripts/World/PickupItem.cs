using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public Sprite icon;

    /// <summary>
    /// true si el objeto surte efecto al recogerlo y NO ocupa el inventario.
    /// La llave abre su puerta en el momento de la recogida: quedarse con ella en
    /// la mano bloqueaba el único hueco del inventario para siempre.
    /// </summary>
    public virtual bool ConsumedOnPickup => false;

    public virtual void OnPickedUp(PlayerInventory inventory) { }
    public virtual void OnDropped() { }
}
