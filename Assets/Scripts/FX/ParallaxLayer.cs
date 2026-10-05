using UnityEngine;

/// <summary>
/// Capa de parallax 2D. Desplaza este objeto una fracción del desplazamiento de una
/// referencia (la cámara, o el héroe cuando la cámara de la sala es fija) para
/// simular profundidad:
///   factor 0      → pegado al mundo (el plano del suelo),
///   0 &lt; factor &lt; 1 → fondo, cuanto más cerca de 1 más lejano,
///   factor 1      → pegado a la referencia (cielo infinito),
///   factor &lt; 0    → primer plano: está entre el observador y el suelo y en pantalla
///                   parece moverse más deprisa y en sentido contrario.
///
/// Toda la matemática es estática y pura (<see cref="Evaluate"/>, <see cref="Wrap"/>,
/// <see cref="FactorFromDepth"/>) para poder probarla en EditMode sin escena.
/// </summary>
[DisallowMultipleComponent]
public class ParallaxLayer : MonoBehaviour
{
    public enum ReferenceMode { MainCamera, Player, Custom }

    [Header("Referencia")]
    [Tooltip("Qué transform se observa. Con cámara fija usa Player: la 'cámara virtual' sigue al héroe.")]
    [SerializeField] ReferenceMode referenceMode = ReferenceMode.MainCamera;
    [SerializeField] Transform customReference;

    [Header("Profundidad")]
    [Tooltip("Fracción del desplazamiento de la referencia que copia la capa, por eje.")]
    [SerializeField] Vector2 factor = new(0.3f, 0.3f);

    [Header("Deriva propia (niebla, polvo)")]
    [Tooltip("Unidades por segundo que la capa se mueve sola, independiente de la referencia.")]
    [SerializeField] Vector2 drift = Vector2.zero;

    [Header("Repetición infinita")]
    [Tooltip("Tamaño de un mosaico en unidades. 0 = sin envolver en ese eje.")]
    [SerializeField] Vector2 wrapSize = Vector2.zero;

    Vector3 _origin;
    Vector2 _referenceStart;
    Transform _reference;
    bool _ready;

    public Vector2 Factor => factor;
    public Vector2 Drift => drift;
    public Vector2 WrapSize => wrapSize;
    public ReferenceMode Mode => referenceMode;

    void Start() => Bind();

    void LateUpdate()
    {
        if (!_ready && !Bind()) return;

        Vector2 refDelta = (Vector2)_reference.position - _referenceStart;
        Vector2 driftOffset = drift * Time.time;
        Vector2 target = Evaluate(_origin, refDelta, factor, driftOffset);
        Vector2 wrapped = new(Wrap(target.x - _origin.x, wrapSize.x) + _origin.x,
                              Wrap(target.y - _origin.y, wrapSize.y) + _origin.y);
        transform.position = new Vector3(wrapped.x, wrapped.y, _origin.z);
    }

    bool Bind()
    {
        _reference = ResolveReference();
        if (_reference == null) return false;
        _origin = transform.position;
        _referenceStart = _reference.position;
        _ready = true;
        return true;
    }

    Transform ResolveReference()
    {
        switch (referenceMode)
        {
            case ReferenceMode.Player:
                var player = GameObject.FindGameObjectWithTag("Player");
                return player != null ? player.transform : null;
            case ReferenceMode.Custom:
                return customReference;
            default:
                return Camera.main != null ? Camera.main.transform : null;
        }
    }

    /// <summary>Configura la capa por código (la usa el builder del editor).</summary>
    public void Configure(ReferenceMode mode, Vector2 depthFactor, Vector2 selfDrift, Vector2 tileSize)
    {
        referenceMode = mode;
        factor = depthFactor;
        drift = selfDrift;
        wrapSize = tileSize;
    }

    // ------------------------------------------------------------- matemática

    /// <summary>
    /// posición = origen + Δreferencia ⊙ factor + deriva.
    /// ⊙ es producto componente a componente: cada eje puede tener su propia profundidad.
    /// </summary>
    public static Vector2 Evaluate(Vector2 origin, Vector2 referenceDelta, Vector2 factor, Vector2 driftOffset)
    {
        return origin + Vector2.Scale(referenceDelta, factor) + driftOffset;
    }

    /// <summary>
    /// Envuelve un desplazamiento al intervalo [-period/2, period/2) para que una
    /// capa en mosaico pueda avanzar indefinidamente sin alejarse de su origen.
    /// Con period ≤ 0 devuelve el valor sin cambios.
    /// </summary>
    public static float Wrap(float offset, float period)
    {
        if (period <= 0f) return offset;
        float half = period * 0.5f;
        return Mathf.Repeat(offset + half, period) - half;
    }

    /// <summary>
    /// Factor de parallax a partir de una profundidad aparente, con el observador a
    /// distancia 1 del plano del suelo (profundidad 0): factor = d / (1 + d).
    /// d = 1 → 0.5, d → ∞ → 1. Profundidades negativas (entre el observador y el
    /// suelo, -1 &lt; d &lt; 0) dan factores negativos: primer plano.
    /// </summary>
    public static float FactorFromDepth(float depth)
    {
        return depth / (1f + depth);
    }

    void OnDrawGizmosSelected()
    {
        if (!_ready) return;
        // Naranja: vector desde el origen de la capa hasta su posición actual.
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawLine(_origin, transform.position);
        Gizmos.DrawWireSphere(_origin, 0.1f);
    }
}
