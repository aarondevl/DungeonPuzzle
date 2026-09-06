using UnityEngine;

/// <summary>
/// Inventario de una sola ranura. Distingue objetos que se GUARDAN (la piedra, que
/// hay que llevar hasta el punto de lanzamiento) de los que se CONSUMEN al recogerlos
/// (la llave, que abre su puerta en el acto).
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    public PickupItem HeldItem { get; private set; }

    public bool TryPickUp(PickupItem item)
    {
        if (item == null) return false;

        // Consumible: surte efecto y desaparece, sin ocupar la ranura. Antes la llave
        // se quedaba en la mano y dejaba al jugador sin poder recoger ni lanzar nada
        // durante el resto de la sala (Room_05 tiene llave y piedras).
        if (item.ConsumedOnPickup)
        {
            item.OnPickedUp(this);
            item.gameObject.SetActive(false);
            return true;
        }

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

    /// <summary>Regla de admisión, aislada del motor para poder testearla.</summary>
    public static bool CanPickUp(bool consumable, bool handIsFull) => consumable || !handIsFull;
}
