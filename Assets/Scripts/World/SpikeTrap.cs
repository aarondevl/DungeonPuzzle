using UnityEngine;

/// <summary>
/// Trampa de pinchos: obstáculo con ciclo animado propio (oculto → asomando → clavado
/// → retirándose) que hiere al héroe solo durante la fase peligrosa.
///
/// Es el complemento del guardia: el guardia castiga que te VEAN, la trampa castiga
/// DÓNDE pisas, así que obliga a leer el ritmo de la sala en vez de solo las líneas
/// de visión.
///
/// Sobre las colisiones: el collider es un trigger que se HABILITA y DESHABILITA con
/// la fase. Apagar el collider (en vez de comprobar un bool dentro del callback) hace
/// que el motor deje de reportar el contacto por completo, y evita el fallo clásico
/// de "entré cuando estaba desarmada y nunca me mata": al rearmarse, un
/// <c>Collider2D.Overlap</c> comprueba quién está encima en ese instante, porque un
/// cuerpo que ya estaba dentro no vuelve a emitir <c>OnTriggerEnter2D</c>.
/// </summary>
[RequireComponent(typeof(Collider2D), typeof(SpriteRenderer))]
public class SpikeTrap : MonoBehaviour
{
    public enum Phase { Hidden, Rising, Extended, Falling }

    [Header("Ritmo (segundos)")]
    [SerializeField] float hiddenSeconds = 1.6f;
    [SerializeField] float risingSeconds = 0.25f;
    [SerializeField] float extendedSeconds = 0.9f;
    [SerializeField] float fallingSeconds = 0.25f;
    [Tooltip("Desfase inicial: permite alternar trampas vecinas sin duplicar prefabs.")]
    [SerializeField] float startOffset;

    [Header("Animación")]
    [SerializeField] Sprite hiddenSprite;
    [SerializeField] Sprite risingSprite;
    [SerializeField] Sprite extendedSprite;

    [Header("Daño")]
    [SerializeField] LayerMask victimLayers;

    SpriteRenderer _sr;
    Collider2D _col;
    Phase _phase = Phase.Hidden;
    float _timer;
    // Un mismo cuerpo puede reportarse dos veces en el mismo asomo (el jugador
    // lleva collider sólido + trigger del sensor). Se cobra una sola vez por ciclo.
    bool _punishedThisCycle;
    bool _started;

    public Phase CurrentPhase => _phase;
    public bool IsDeadly => _phase == Phase.Extended;

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
        gameObject.layer = CollisionLayers.Hazard;
        _timer = startOffset;
        // La fase inicial se aplica SIN castigar: si el desfase hace que la trampa
        // nazca clavada, no debe matar a nadie antes del primer frame de juego.
        Apply(PhaseAt(_timer, hiddenSeconds, risingSeconds, extendedSeconds, fallingSeconds));
        _started = true;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        Phase next = PhaseAt(_timer, hiddenSeconds, risingSeconds, extendedSeconds, fallingSeconds);
        if (next != _phase) Apply(next);
    }

    void Apply(Phase phase)
    {
        bool wasDeadly = _phase == Phase.Extended;
        _phase = phase;
        _col.enabled = phase == Phase.Extended;
        if (phase != Phase.Extended) _punishedThisCycle = false;

        Sprite s = phase switch
        {
            Phase.Extended => extendedSprite,
            Phase.Rising or Phase.Falling => risingSprite,
            _ => hiddenSprite
        };
        if (s != null && _sr != null) _sr.sprite = s;

        if (_started && !wasDeadly && phase == Phase.Extended)
        {
            SfxLibrary.Play("SFX/stone_land", 0.8f);
            PunishOverlapping();
        }
    }

    /// <summary>
    /// Al asomar, castiga a quien YA estaba encima: los cuerpos que no se mueven no
    /// generan un nuevo evento de entrada al reactivar el collider.
    /// </summary>
    void PunishOverlapping()
    {
        int mask = CollisionLayers.Resolve(victimLayers, CollisionLayers.PlayerMask);
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true, layerMask = mask };
        var hits = new Collider2D[4];
        int count = _col.Overlap(filter, hits);
        for (int i = 0; i < count; i++) Punish(hits[i]);
    }

    void OnTriggerEnter2D(Collider2D other) => Punish(other);

    void Punish(Collider2D victim)
    {
        if (!IsDeadly || _punishedThisCycle || victim == null) return;
        int mask = CollisionLayers.Resolve(victimLayers, CollisionLayers.PlayerMask);
        if (!CollisionLayers.Contains(mask, victim.gameObject.layer)) return;
        _punishedThisCycle = true;

        Vfx.Spark(victim.transform.position);
        if (GameManager.Instance != null) GameManager.Instance.PlayerDetected();
    }

    /// <summary>
    /// Fase del ciclo en el segundo <paramref name="time"/>. Función pura: es la
    /// pieza que cubren los tests EditMode, sin necesidad de instanciar la escena.
    /// </summary>
    public static Phase PhaseAt(float time, float hidden, float rising, float extended, float falling)
    {
        float period = Mathf.Max(0.01f, hidden + rising + extended + falling);
        float t = Mathf.Repeat(time, period);
        if (t < hidden) return Phase.Hidden;
        if (t < hidden + rising) return Phase.Rising;
        if (t < hidden + rising + extended) return Phase.Extended;
        return Phase.Falling;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = IsDeadly ? new Color(1f, 0.2f, 0.2f, 0.6f) : new Color(1f, 0.6f, 0.2f, 0.3f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.6f);
    }
}
