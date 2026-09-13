using UnityEngine;

/// <summary>
/// Guardia fijo que barre con el cono. Es DINÁMICO respecto a la sala: mide con
/// raycasts hacia dónde hay espacio abierto y barre solo esa zona, en vez de
/// pasarse la mitad del tiempo mirando una pared (un guardia pegado a un muro que
/// barra ±45° alrededor de "arriba" nunca vigila el pasillo que tiene debajo).
///
/// Al oír un ruido o ver fugazmente al héroe gira el cono hacia ese punto (ángulo
/// del vector guardia → punto) y, al calmarse, vuelve al barrido girando, no
/// saltando.
/// </summary>
public class GuardStatic : GuardBase
{
    [SerializeField] float rotationSpeed = 30f;
    [Tooltip("Medio ángulo del barrido cuando NO se adapta a los muros (o si todo está abierto).")]
    [SerializeField] float maxAngle = 45f;
    [Tooltip("Grados por segundo al girar hacia un ruido o al volver al barrido.")]
    [SerializeField] float turnSpeed = 180f;

    [Header("Barrido adaptado a los muros")]
    [Tooltip("Mide con raycasts dónde hay espacio y barre solo el arco abierto.")]
    [SerializeField] bool adaptToWalls = true;
    [Tooltip("Fracción mínima del alcance del cono que debe estar libre para considerar abierta una dirección.")]
    [SerializeField, Range(0.2f, 1f)] float minOpenFraction = 0.55f;
    [Tooltip("Tope del medio ángulo del barrido adaptado.")]
    [SerializeField] float adaptiveMaxHalfAngle = 110f;
    [Tooltip("Cada cuántos segundos se vuelve a medir la sala.")]
    [SerializeField] float rescanSeconds = 1f;

    public const int ScanRays = 72;                   // uno cada 5°

    float _baseAngle;
    float _centerAngle;
    float _halfRange;
    float _time;
    float _scanTimer;
    float[] _open;
    bool _hasLookTarget;
    Vector2 _lookTarget;
    bool _resuming;

    /// <summary>Centro y medio ángulo del barrido en uso (para HUD de depuración y gizmos).</summary>
    public float SweepCenter => _centerAngle;
    public float SweepHalfRange => _halfRange;

    protected override void Awake()
    {
        base.Awake();
        _baseAngle = transform.eulerAngles.z;
        _centerAngle = _baseAngle;
        _halfRange = maxAngle;
        if (adaptToWalls) Scan();
    }

    void FixedUpdate()
    {
        float maxTurn = turnSpeed * Time.fixedDeltaTime;

        if (adaptToWalls)
        {
            _scanTimer -= Time.fixedDeltaTime;
            if (_scanTimer <= 0f) { Scan(); _scanTimer = rescanSeconds; }
        }

        if (State == GuardState.Alerted)
        {
            if (!_hasLookTarget) return;
            Vector2 toTarget = _lookTarget - Rb.position;               // vector hacia el ruido
            if (toTarget.sqrMagnitude < 1e-6f) return;
            float target = VectorMath.DirectionToAngle(toTarget);
            Rb.MoveRotation(Mathf.MoveTowardsAngle(Rb.rotation, target, maxTurn));
            return;
        }

        float sweep = SweepAngle(_centerAngle, _time, _halfRange);
        if (_resuming)
        {
            // Volver al barrido girando, no saltando.
            float next = Mathf.MoveTowardsAngle(Rb.rotation, sweep, maxTurn);
            Rb.MoveRotation(next);
            if (Mathf.Abs(Mathf.DeltaAngle(next, sweep)) < 0.5f) _resuming = false;
            return;
        }

        _time += Time.fixedDeltaTime * rotationSpeed * Mathf.Deg2Rad;
        Rb.MoveRotation(SweepAngle(_centerAngle, _time, _halfRange));
    }

    /// <summary>Ángulo del barrido: centro ± halfRange siguiendo una senoide.</summary>
    public static float SweepAngle(float centerAngle, float time, float halfRange) =>
        centerAngle + Mathf.Sin(time) * halfRange;

    // ---------- lectura de la sala ----------

    /// <summary>
    /// Lanza un rayo en cada dirección hasta el alcance del cono y anota qué fracción
    /// queda libre de muros. Con eso elige el arco abierto que contiene la orientación
    /// original del guardia (o el mayor) y ajusta el barrido a él, descontando medio
    /// cono para que ni el borde del cono se pierda en una pared.
    /// </summary>
    void Scan()
    {
        float range = VisionCone != null ? VisionCone.distance : 5f;
        float coneAngle = VisionCone != null ? VisionCone.angle : 50f;
        if (_open == null) _open = new float[ScanRays];

        float step = 360f / ScanRays;
        Vector2 origin = Rb != null ? Rb.position : (Vector2)transform.position;
        for (int i = 0; i < ScanRays; i++)
        {
            Vector2 dir = VectorMath.AngleToDirection(i * step);
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, range, CollisionLayers.WallsMask);
            _open[i] = hit ? hit.distance / range : 1f;
        }

        OpenArc arc = ComputeOpenArc(_open, step, _baseAngle, minOpenFraction);
        float center, half;
        if (arc.FullCircle || arc.HalfWidth <= 0f)
        {
            center = _baseAngle;
            half = maxAngle;
        }
        else
        {
            center = arc.Center;
            half = Mathf.Clamp(arc.HalfWidth - coneAngle * 0.5f, 0f, adaptiveMaxHalfAngle);
        }

        bool changed = Mathf.Abs(Mathf.DeltaAngle(center, _centerAngle)) > 2f || Mathf.Abs(half - _halfRange) > 2f;
        _centerAngle = center;
        _halfRange = half;
        if (changed && Application.isPlaying) _resuming = true;
    }

    public struct OpenArc
    {
        public float Center;      // ángulo central del arco (convención del guardia)
        public float HalfWidth;   // medio ángulo del arco
        public bool FullCircle;   // todo abierto: no hace falta adaptar
    }

    /// <summary>
    /// Elige el arco abierto a partir de las medidas de <see cref="Scan"/>. Función pura,
    /// cubierta por tests: <paramref name="openness"/>[i] es la fracción libre en la
    /// dirección i·stepDeg. Se prefiere el arco que contiene <paramref name="preferredAngle"/>;
    /// si ninguno lo contiene, el más ancho.
    /// </summary>
    public static OpenArc ComputeOpenArc(float[] openness, float stepDeg, float preferredAngle, float minOpen)
    {
        int n = openness.Length;
        var isOpen = new bool[n];
        int openCount = 0;
        for (int i = 0; i < n; i++) { isOpen[i] = openness[i] >= minOpen; if (isOpen[i]) openCount++; }

        if (openCount == n) return new OpenArc { Center = preferredAngle, HalfWidth = 180f, FullCircle = true };
        if (openCount == 0) return new OpenArc { Center = preferredAngle, HalfWidth = 0f };

        // Empezar el recorrido circular justo después de una dirección cerrada, para
        // que ningún arco quede partido entre el final y el principio del array.
        int start = 0;
        while (isOpen[start]) start = (start + 1) % n;

        int preferred = Mathf.RoundToInt(Mathf.Repeat(preferredAngle, 360f) / stepDeg) % n;
        int bestStart = -1, bestLen = 0;
        bool bestHasPreferred = false;

        int i0 = start;
        for (int k = 0; k < n; )
        {
            int idx = (i0 + k) % n;
            if (!isOpen[idx]) { k++; continue; }
            int runStart = idx, len = 0;
            bool hasPreferred = false;
            while (k < n && isOpen[(i0 + k) % n])
            {
                if ((i0 + k) % n == preferred) hasPreferred = true;
                len++; k++;
            }
            bool better = hasPreferred && !bestHasPreferred || (hasPreferred == bestHasPreferred && len > bestLen);
            if (better) { bestStart = runStart; bestLen = len; bestHasPreferred = hasPreferred; }
        }

        float halfWidth = bestLen * stepDeg * 0.5f;
        float center = bestStart * stepDeg + (bestLen - 1) * stepDeg * 0.5f;
        return new OpenArc { Center = Mathf.Repeat(center, 360f), HalfWidth = halfWidth };
    }

    // ---------- alerta ----------

    void LookAt(Vector2 point)
    {
        _lookTarget = point;
        _hasLookTarget = true;
    }

    protected override void OnVisionAlerted(Vector2 position) => LookAt(position);
    protected override void OnNoiseAlerted(Vector2 position) => LookAt(position);
    protected override void OnReturnToNormal()
    {
        _hasLookTarget = false;
        _resuming = true;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 o = transform.position;
        float range = VisionCone != null ? VisionCone.distance : 5f;
        // Límites del barrido en uso (amarillo).
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(o, o + (Vector3)VectorMath.AngleToDirection(_centerAngle - _halfRange) * range);
        Gizmos.DrawLine(o, o + (Vector3)VectorMath.AngleToDirection(_centerAngle + _halfRange) * range);
        if (Application.isPlaying && _hasLookTarget)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(o, _lookTarget);
        }
    }
}
