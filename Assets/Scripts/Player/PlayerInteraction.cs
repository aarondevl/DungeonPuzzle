using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInventory))]
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] float interactRadius = 1f;
    [SerializeField] LayerMask interactLayer;
    [SerializeField] LayerMask itemLayer;

    PlayerInventory _inventory;

    void Awake() => _inventory = GetComponent<PlayerInventory>();

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.eKey.wasPressedThisFrame) TryInteract();
        if (kb.fKey.wasPressedThisFrame) TryThrow();
    }

    void TryInteract()
    {
        Collider2D item = Physics2D.OverlapCircle(transform.position, interactRadius, itemLayer);
        if (item != null)
        {
            var pickup = item.GetComponent<PickupItem>();
            if (pickup != null) { _inventory.TryPickUp(pickup); return; }
        }

        Collider2D interactable = Physics2D.OverlapCircle(transform.position, interactRadius, interactLayer);
        if (interactable != null)
            interactable.GetComponent<IInteractable>()?.Interact(_inventory);
    }

    void TryThrow()
    {
        if (!_inventory.HasItem<Stone>()) return;
        var stone = _inventory.TakeItem() as Stone;
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        stone.Throw(transform.position, mouseWorld);
    }
}
