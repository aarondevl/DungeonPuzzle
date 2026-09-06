using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Traduce las teclas de acción (E = interactuar, F = lanzar) sobre lo que el
/// <see cref="InteractionSensor"/> tiene registrado en rango. Ya no hace consultas
/// espaciales propias: la detección la resuelve el motor de física por triggers.
/// </summary>
[RequireComponent(typeof(PlayerInventory))]
public class PlayerInteraction : MonoBehaviour
{
    PlayerInventory _inventory;
    InteractionSensor _sensor;
    Collider2D _ownCollider;
    Camera _cam;

    /// <summary>Objeto que se accionaría al pulsar E ahora mismo (lo consume el HUD).</summary>
    public Component CurrentTarget { get; private set; }

    void Awake()
    {
        _inventory = GetComponent<PlayerInventory>();
        // Se añade en caliente si el prefab aún no lo trae: RequireComponent solo
        // actúa al añadir el script, no sobre prefabs ya guardados.
        _sensor = GetComponent<InteractionSensor>();
        if (_sensor == null) _sensor = gameObject.AddComponent<InteractionSensor>();
        _ownCollider = SolidCollider();
        _cam = Camera.main;
    }

    /// <summary>
    /// El cuerpo sólido del héroe, no el trigger del sensor: es el que hay que
    /// ignorar para que la piedra recién lanzada no lo empuje.
    /// </summary>
    Collider2D SolidCollider()
    {
        foreach (var c in GetComponents<Collider2D>())
            if (!c.isTrigger) return c;
        return GetComponent<Collider2D>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        CurrentTarget = FindTarget();
        if (kb.eKey.wasPressedThisFrame) TryInteract();
        if (kb.fKey.wasPressedThisFrame) TryThrow();
    }

    Component FindTarget()
    {
        // Los objetos recogibles tienen prioridad sobre los accionables.
        var pickup = _sensor.Closest<PickupItem>(CollisionLayers.ItemsMask);
        if (pickup != null && _inventory.HeldItem == null) return pickup;
        return _sensor.Closest<IInteractable>(CollisionLayers.InteractableMask) as Component;
    }

    void TryInteract()
    {
        // Se acciona exactamente lo que FindTarget ya eligió, de modo que lo que
        // el HUD anuncia y lo que ocurre al pulsar E no puedan discrepar.
        switch (CurrentTarget)
        {
            case PickupItem pickup: _inventory.TryPickUp(pickup); break;
            case IInteractable interactable: interactable.Interact(_inventory); break;
        }
    }

    void TryThrow()
    {
        if (!_inventory.HasItem<Stone>()) return;
        if (_cam == null) _cam = Camera.main;
        if (_cam == null || Mouse.current == null) return;
        var stone = _inventory.TakeItem() as Stone;
        Vector2 mouseWorld = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        stone.Throw(transform.position, mouseWorld, _ownCollider);
    }
}
