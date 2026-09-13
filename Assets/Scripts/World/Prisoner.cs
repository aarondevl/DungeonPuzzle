using UnityEngine;

/// <summary>
/// Otro preso del calabozo. Encadenado, es un objeto usable (E  LIBERAR). Al
/// liberarlo echa a correr por su ruta y pasa a la capa Player: los guardias que
/// lo ven lo persiguen A ÉL y, si lo alcanzan, se lo llevan. Es el señuelo que
/// despeja un pasillo vigilado durante unos segundos.
///
/// Mientras corre es un cuerpo cinemático que sigue sus puntos en bucle, igual
/// que una patrulla (paso = dirección · rapidez · Δt).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Prisoner : MonoBehaviour, IInteractable
{
    [SerializeField] Transform[] route;
    [SerializeField] float runSpeed = 3.3f;
    [SerializeField] float pointRadius = 0.2f;
    [Tooltip("Tinte mientras está encadenado.")]
    [SerializeField] Color chainedTint = new Color(0.6f, 0.65f, 0.8f, 1f);

    Rigidbody2D _rb;
    SpriteRenderer[] _renderers;
    int _index;
    bool _freed;
    bool _caught;

    public bool IsFreed => _freed;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0f;
        _rb.freezeRotation = true;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.useFullKinematicContacts = true;
        _renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in _renderers) r.color = chainedTint;
        gameObject.layer = CollisionLayers.Interactable;          // usable con E mientras está encadenado
    }

    /// <summary>E sobre el preso: rompe las cadenas y sale corriendo.</summary>
    public void Interact(PlayerInventory inventory)
    {
        if (_freed) return;
        _freed = true;
        SfxLibrary.Play("SFX/lever");
        Vfx.Spark(transform.position);
        Vfx.FloatingText(transform.position + Vector3.up * 0.6f, "¡CORRE!", new Color(0.7f, 1f, 0.7f, 1f), 1.2f, 5f);
        foreach (var r in _renderers) r.color = Color.white;
        inventory.GetComponent<PlayerFeedback>()?.Punch(1.1f, 0.15f);
        gameObject.layer = CollisionLayers.Player;                // ahora los guardias lo ven y lo persiguen
    }

    void FixedUpdate()
    {
        if (!_freed || _caught || route == null || route.Length == 0) return;
        Vector2 target = route[_index].position;
        _rb.MovePosition(VectorMath.StepTowards(_rb.position, target, runSpeed, Time.fixedDeltaTime));
        if (VectorMath.Distance(_rb.position, target) <= pointRadius)
            _index = (_index + 1) % route.Length;
    }

    /// <summary>Un guardia lo ha alcanzado: se lo llevan y desaparece.</summary>
    public void OnCaught()
    {
        if (_caught) return;
        _caught = true;
        SfxLibrary.Play("SFX/detected", 0.3f);
        Vfx.Alert(transform.position);
        Vfx.FloatingText(transform.position + Vector3.up * 0.6f, "¡ATRAPADO!", new Color(1f, 0.5f, 0.5f, 1f), 1.2f, 5f);
        Destroy(gameObject, 0.05f);
    }

    void OnDrawGizmosSelected()
    {
        if (route == null) return;
        Gizmos.color = new Color(0.6f, 0.9f, 1f, 1f);
        for (int i = 0; i < route.Length; i++)
        {
            if (route[i] == null) continue;
            Gizmos.DrawWireSphere(route[i].position, pointRadius);
            var next = route[(i + 1) % route.Length];
            if (next != null) Gizmos.DrawLine(route[i].position, next.position);
        }
    }
}
