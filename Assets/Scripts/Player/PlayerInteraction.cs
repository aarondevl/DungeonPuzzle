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
    PlayerMovement _movement;
    ThrowAim _aim;
    InteractionSensor _sensor;
    Collider2D _ownCollider;
    Camera _cam;

    /// <summary>Objeto que se accionaría al pulsar E ahora mismo (lo consume el letrero de ayuda).</summary>
    public Component CurrentTarget { get; private set; }

    /// <summary>Sensor de proximidad, para que el letrero pueda avisar de placas y otros objetos cercanos.</summary>
    public InteractionSensor Sensor => _sensor;

    void Awake()
    {
        _inventory = GetComponent<PlayerInventory>();
        _movement = GetComponent<PlayerMovement>();
        // Se añade en caliente si el prefab aún no lo trae: RequireComponent solo
        // actúa al añadir el script, no sobre prefabs ya guardados.
        _sensor = GetComponent<InteractionSensor>();
        if (_sensor == null) _sensor = gameObject.AddComponent<InteractionSensor>();
        _ownCollider = SolidCollider();
        _cam = Camera.main;
        _aim = GetComponent<ThrowAim>();
        if (_aim == null) _aim = gameObject.AddComponent<ThrowAim>();
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
        UpdateAim();
        if (kb.eKey.wasPressedThisFrame) TryInteract();
        if (kb.fKey.wasPressedThisFrame) TryThrow();
    }

    void OnDisable() { if (_aim != null) _aim.Hide(); }

    /// <summary>Con una piedra en mano se dibuja hacia dónde caerá.</summary>
    void UpdateAim()
    {
        if (_aim == null) return;
        var stone = _inventory.HeldItem as Stone;
        if (stone == null) { _aim.Hide(); return; }
        Vector2 origin = transform.position;
        Vector2 target = ThrowAim.ClampTarget(origin, AimPoint(origin), stone.MaxRange);
        _aim.Show(origin, target);
    }

    Component FindTarget()
    {
        // Los objetos recogibles tienen prioridad sobre los accionables. Un
        // consumible (la llave) se puede recoger aunque la mano esté ocupada.
        var pickup = _sensor.Closest<PickupItem>(CollisionLayers.ItemsMask);
        if (pickup != null && PlayerInventory.CanPickUp(pickup.ConsumedOnPickup, _inventory.HeldItem != null))
            return pickup;
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
        Vector2 origin = transform.position;                       // punto de lanzamiento
        Vector2 target = AimPoint(origin);
        var stone = _inventory.TakeItem() as Stone;
        target = ThrowAim.ClampTarget(origin, target, stone.MaxRange);
        if (_aim != null) _aim.Hide();
        stone.Throw(origin, target, _ownCollider);
    }

    /// <summary>
    /// Punto del mundo al que se apunta. Con ratón: la posición del cursor pasada de
    /// coordenadas de PANTALLA (píxeles) a coordenadas de MUNDO. Sin ratón: un punto
    /// a una unidad por delante del héroe en su dirección de mirada.
    /// </summary>
    Vector2 AimPoint(Vector2 origin)
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam != null && Mouse.current != null)
            return _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Vector2 facing = _movement != null ? _movement.Facing : Vector2.up;
        return origin + facing;
    }
}
