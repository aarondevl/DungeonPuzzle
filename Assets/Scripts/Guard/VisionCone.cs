using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cono de visión del guardia. Dos responsabilidades separadas:
///
///   * DIBUJO: una malla en abanico generada cada frame y recortada con raycasts
///     contra los muros, para que el jugador vea exactamente qué zona es peligrosa.
///   * DETECCIÓN: puramente vectorial. Un objetivo es visto si el vector que va del
///     guardia a él (1) mide menos que el alcance, (2) forma con la dirección de
///     mirada un ángulo menor que la mitad de la apertura y (3) no lo corta ningún
///     muro (raycast de línea de visión).
///
/// Los objetivos son todo lo que está en la capa Player: el héroe y los prisioneros
/// liberados que hacen de señuelo. Si ve a los dos, prefiere al señuelo: para eso
/// se le libera.
///
/// La dirección de mirada es <c>transform.up</c>: el cono es hijo del guardia y
/// hereda su rotación, así que 0° = arriba, -90° = derecha (ver VectorMath).
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionCone : MonoBehaviour
{
    [SerializeField] public float angle = 60f;
    [SerializeField] public float distance = 5f;
    [SerializeField] int rayCount = 60;
    [SerializeField] LayerMask wallLayer;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] Material normalMaterial;
    [SerializeField] Material alertMaterial;
    [SerializeField, Range(0f, 1f)] float coneAlpha = 0.55f;

    const float CornerEpsilonDeg = 0.01f;

    MeshFilter _mf;
    MeshRenderer _mr;
    Mesh _mesh;
    bool _enabled = true;

    static readonly List<float> _angleBuffer = new List<float>(256);
    static readonly Collider2D[] _wallBuffer = new Collider2D[16];
    static readonly Collider2D[] _targetBuffer = new Collider2D[8];

    public bool IsSeeingPlayer { get; private set; }
    public Vector2 LastSeenPlayerPosition { get; private set; }
    /// <summary>Collider del último objetivo visto (héroe o señuelo).</summary>
    public Collider2D LastSeenCollider { get; private set; }
    public event System.Action OnPlayerDetected;

    /// <summary>Vector unitario de la dirección de mirada, en coordenadas del mundo.</summary>
    public Vector2 Forward => transform.up;

    /// <summary>Punto del mundo desde el que mira el guardia.</summary>
    public Vector2 Origin => transform.position;

    /// <summary>Distancia al objetivo en el último chequeo (infinito si no hay ninguno en alcance).</summary>
    public float DistanceToPlayer { get; private set; } = float.PositiveInfinity;

    /// <summary>Ángulo entre la mirada y el objetivo en el último chequeo (grados).</summary>
    public float AngleToPlayer { get; private set; } = float.PositiveInfinity;

    // Máscaras efectivas: si el prefab dejó el campo vacío se usa la capa canónica,
    // en vez de fallar en silencio (un wallLayer a 0 hacía el cono atravesar muros).
    int _wallMask;
    int _playerMask;

    void Awake()
    {
        _wallMask = CollisionLayers.Resolve(wallLayer, CollisionLayers.WallsMask);
        _playerMask = CollisionLayers.Resolve(playerLayer, CollisionLayers.PlayerMask);
        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();
        _mesh = new Mesh { name = "VisionConeMesh" };
        _mf.mesh = _mesh;
        _mr.material = normalMaterial;
    }

    void LateUpdate()
    {
        if (!_enabled) { IsSeeingPlayer = false; return; }
        BuildMesh();
        CheckDetection();
    }

    /// <summary>Apaga el cono (guardia aturdido): no ve ni se dibuja.</summary>
    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        _mr.enabled = enabled;
        if (!enabled) IsSeeingPlayer = false;
    }

    void BuildMesh()
    {
        float halfAngle = angle / 2f;
        _angleBuffer.Clear();

        float angleStep = angle / rayCount;
        for (int i = 0; i <= rayCount; i++)
            _angleBuffer.Add(-halfAngle + angleStep * i);

        Vector2 origin = Origin;
        int wallCount = Physics2D.OverlapCircleNonAlloc(origin, distance, _wallBuffer, _wallMask);
        float forwardWorldDeg = Mathf.Atan2(Forward.x, Forward.y) * Mathf.Rad2Deg;

        // Rayos extra hacia cada esquina de muro visible para que el borde del
        // recorte sea nítido en lugar de escalonado.
        for (int w = 0; w < wallCount; w++)
        {
            var b = _wallBuffer[w].bounds;
            for (int c = 0; c < 4; c++)
            {
                Vector2 corner = new Vector2(c < 2 ? b.min.x : b.max.x, (c & 1) == 0 ? b.min.y : b.max.y);
                Vector2 toCorner = corner - origin;
                if (toCorner.sqrMagnitude > distance * distance) continue;
                float worldDeg = Mathf.Atan2(toCorner.x, toCorner.y) * Mathf.Rad2Deg;
                float localDeg = Mathf.DeltaAngle(forwardWorldDeg, worldDeg);
                if (Mathf.Abs(localDeg) > halfAngle) continue;
                _angleBuffer.Add(Mathf.Clamp(localDeg - CornerEpsilonDeg, -halfAngle, halfAngle));
                _angleBuffer.Add(Mathf.Clamp(localDeg + CornerEpsilonDeg, -halfAngle, halfAngle));
            }
        }

        _angleBuffer.Sort();

        int n = _angleBuffer.Count;
        var vertices = new Vector3[n + 1];
        var triangles = new int[(n - 1) * 3];
        var colors = new Color[n + 1];
        Color uniform = new Color(1, 1, 1, coneAlpha);

        vertices[0] = Vector3.zero;
        colors[0] = uniform;

        for (int i = 0; i < n; i++)
        {
            float a = _angleBuffer[i];
            float rad = a * Mathf.Deg2Rad;
            // Dirección local del rayo: (sin, cos) porque 0° es "arriba" (+Y local).
            Vector2 localDir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
            Vector2 worldDir = transform.TransformDirection(localDir);
            RaycastHit2D hit = Physics2D.Raycast(origin, worldDir, distance, _wallMask);
            // El vértice se guarda en coordenadas LOCALES del cono (la malla rota con él).
            Vector3 point = hit ? transform.InverseTransformPoint(hit.point)
                                : (Vector3)(localDir * distance);
            vertices[i + 1] = point;
            colors[i + 1] = uniform;
        }

        for (int i = 0; i < n - 1; i++)
        {
            triangles[i * 3 + 0] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.colors = colors;
        _mesh.RecalculateNormals();
    }

    void CheckDetection()
    {
        bool sees = false;
        DistanceToPlayer = float.PositiveInfinity;
        AngleToPlayer = float.PositiveInfinity;
        Collider2D best = null;
        bool bestIsDecoy = false;

        int count = Physics2D.OverlapCircleNonAlloc(Origin, distance, _targetBuffer, _playerMask);
        for (int i = 0; i < count; i++)
        {
            var col = _targetBuffer[i];
            if (col == null || col.isTrigger) continue;               // el sensor del héroe no cuenta
            Vector2 point = col.bounds.center;
            Vector2 toTarget = point - Origin;                          // vector guardia → objetivo
            float dist = toTarget.magnitude;
            float ang = VectorMath.AngleBetween(Forward, toTarget);
            if (dist < DistanceToPlayer) { DistanceToPlayer = dist; AngleToPlayer = ang; }

            if (!VectorMath.IsInsideCone(Origin, Forward, point, angle * 0.5f, distance)) continue;
            if (!HasLineOfSight(Origin, point)) continue;

            bool isDecoy = col.GetComponentInParent<Prisoner>() != null;
            if (best == null || (isDecoy && !bestIsDecoy)) { best = col; bestIsDecoy = isDecoy; }
        }

        if (best != null)
        {
            sees = true;
            LastSeenCollider = best;
            LastSeenPlayerPosition = best.bounds.center;
        }

        IsSeeingPlayer = sees;
        if (sees) OnPlayerDetected?.Invoke();
    }

    /// <summary>Un muro entre los dos puntos bloquea la visión aunque el ángulo cuadre.</summary>
    bool HasLineOfSight(Vector2 from, Vector2 to)
    {
        Vector2 dir = VectorMath.Direction(from, to);
        float len = VectorMath.Distance(from, to);
        return !Physics2D.Raycast(from, dir, len, _wallMask);
    }

    public void SetAlerted(bool alerted) =>
        _mr.material = alerted ? alertMaterial : normalMaterial;

    void OnDrawGizmosSelected()
    {
        Vector3 o = transform.position;
        Vector2 fwd = Application.isPlaying ? Forward : (Vector2)transform.up;
        float half = angle * 0.5f;
        Vector3 left = VectorMath.AngleToDirection(VectorMath.DirectionToAngle(fwd) + half) * distance;
        Vector3 right = VectorMath.AngleToDirection(VectorMath.DirectionToAngle(fwd) - half) * distance;
        Gizmos.color = Color.white;
        Gizmos.DrawLine(o, o + left);
        Gizmos.DrawLine(o, o + right);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(o, o + (Vector3)fwd * distance);

        if (Application.isPlaying && !float.IsInfinity(DistanceToPlayer))
        {
            Gizmos.color = IsSeeingPlayer ? Color.red : Color.gray;
            Gizmos.DrawLine(o, LastSeenPlayerPosition);
        }
    }
}
