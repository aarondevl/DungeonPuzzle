using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Placa de presión: objeto interactivo accionado ÚNICAMENTE por colisión, sin tecla.
/// Mientras haya peso encima mantiene abierta la puerta enlazada; al quedarse vacía,
/// la cierra.
///
/// Es el caso que obliga a distinguir entrada de PERMANENCIA: <c>OnTriggerEnter2D</c>
/// y <c>OnTriggerExit2D</c> no bastan por sí solos porque pueden solaparse varios
/// cuerpos y el segundo Exit apagaría la placa aunque el primero siga encima. Ocurre
/// de verdad: el héroe aporta DOS colliders (el sólido y el trigger del sensor), y un
/// guardia puede pisarla a la vez. Por eso se lleva un CONJUNTO de ocupantes en vez
/// de un bool: la placa se suelta solo cuando el conjunto queda vacío.
///
/// Solo cuentan cuerpos que puedan REPOSAR encima. Una piedra en vuelo no sirve: un
/// trigger no frena a un cuerpo dinámico, así que la sobrevuela y solo produciría un
/// Enter y un Exit en el mismo instante.
///
/// El conjunto se limpia de referencias muertas en cada consulta, porque un objeto
/// destruido o desactivado (una piedra recogida) no siempre emite su Exit.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PressurePlate : MonoBehaviour
{
    [SerializeField] Door linkedDoor;
    [Tooltip("Qué capas cuentan como 'peso' sobre la placa.")]
    [SerializeField] LayerMask acceptedLayers;
    [Tooltip("Si está marcada, una vez pisada permanece accionada para siempre.")]
    [SerializeField] bool latching;

    [Header("Visual")]
    [SerializeField] SpriteRenderer plateRenderer;
    [SerializeField] Sprite releasedSprite;
    [SerializeField] Sprite pressedSprite;

    readonly List<Collider2D> _occupants = new();
    bool _pressed;
    bool _latched;

    public bool IsPressed => _pressed;
    public int OccupantCount => _occupants.Count;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (plateRenderer == null) plateRenderer = GetComponent<SpriteRenderer>();
        Refresh();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        int mask = CollisionLayers.Resolve(acceptedLayers, CollisionLayers.PlayerMask | CollisionLayers.GuardMask);
        if (!CollisionLayers.Contains(mask, other.gameObject.layer)) return;
        if (_occupants.Contains(other)) return;
        _occupants.Add(other);
        Refresh();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (_occupants.Remove(other)) Refresh();
    }

    void Update()
    {
        // Barrido barato: descarta ocupantes destruidos o desactivados que no emitieron Exit.
        if (Prune(_occupants)) Refresh();
    }

    /// <summary>Elimina referencias nulas o inactivas. Devuelve true si cambió la lista.</summary>
    static bool Prune(List<Collider2D> occupants)
    {
        bool changed = false;
        for (int i = occupants.Count - 1; i >= 0; i--)
        {
            Collider2D c = occupants[i];
            if (c != null && c.gameObject.activeInHierarchy && c.enabled) continue;
            occupants.RemoveAt(i);
            changed = true;
        }
        return changed;
    }

    void Refresh()
    {
        bool pressed = ShouldBePressed(_occupants.Count, latching, _latched);
        if (pressed) _latched = true;
        if (pressed == _pressed) return;
        _pressed = pressed;

        SfxLibrary.Play("SFX/lever", pressed ? 1f : 0.7f);
        Vfx.Spark(transform.position);
        if (linkedDoor != null) linkedDoor.SetOpen(pressed);

        Sprite s = pressed ? pressedSprite : releasedSprite;
        if (plateRenderer != null && s != null) plateRenderer.sprite = s;
    }

    /// <summary>Regla de accionamiento, aislada del motor para poder testearla.</summary>
    public static bool ShouldBePressed(int occupantCount, bool latching, bool alreadyLatched) =>
        occupantCount > 0 || (latching && alreadyLatched);

    void OnDrawGizmos()
    {
        Gizmos.color = _pressed ? new Color(0.4f, 1f, 0.4f, 0.5f) : new Color(1f, 0.8f, 0.2f, 0.4f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
}
