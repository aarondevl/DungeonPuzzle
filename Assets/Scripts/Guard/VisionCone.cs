using System.Collections.Generic;
using UnityEngine;

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
    Vector3[] _polyLocal;       // perimeter vertices in local space (excluding origin)
    int _polyCount;

    static readonly List<float> _angleBuffer = new List<float>(256);
    static readonly Collider2D[] _wallBuffer = new Collider2D[16];

    public bool IsSeeingPlayer { get; private set; }
    public event System.Action OnPlayerDetected;

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
        _polyLocal = new Vector3[256];
    }

    void LateUpdate()
    {
        BuildMesh();
        CheckDetection();
    }

    void BuildMesh()
    {
        float halfAngle = angle / 2f;
        _angleBuffer.Clear();

        float angleStep = angle / rayCount;
        for (int i = 0; i <= rayCount; i++)
            _angleBuffer.Add(-halfAngle + angleStep * i);

        Vector2 origin = transform.position;
        int wallCount = Physics2D.OverlapCircleNonAlloc(origin, distance, _wallBuffer, _wallMask);
        float forwardWorldDeg = Mathf.Atan2(transform.up.x, transform.up.y) * Mathf.Rad2Deg;

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
        if (_polyLocal.Length < n) _polyLocal = new Vector3[n];
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
            Vector2 localDir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
            Vector2 worldDir = transform.TransformDirection(localDir);
            RaycastHit2D hit = Physics2D.Raycast(origin, worldDir, distance, _wallMask);
            Vector3 point = hit ? transform.InverseTransformPoint(hit.point)
                                : (Vector3)(localDir * distance);
            vertices[i + 1] = point;
            colors[i + 1] = uniform;
            _polyLocal[i] = point;
        }
        _polyCount = n;

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

    bool PointInsideCone(Vector3 localPoint)
    {
        // Fan polygon: origin + perimeter[0..n-1]. Inside if any triangle (origin, p[i], p[i+1]) contains the point.
        Vector2 p = localPoint;
        for (int i = 0; i < _polyCount - 1; i++)
        {
            if (PointInTriangle(p, Vector2.zero, _polyLocal[i], _polyLocal[i + 1])) return true;
        }
        return false;
    }

    static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);
        bool hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        bool hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);
        return !(hasNeg && hasPos);
    }

    static float Sign(Vector2 p1, Vector2 p2, Vector2 p3) =>
        (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);

    void CheckDetection()
    {
        bool sees = false;
        Collider2D hit = Physics2D.OverlapCircle(transform.position, distance, _playerMask);
        if (hit != null && _polyCount >= 2)
        {
            Vector3 localPlayer = transform.InverseTransformPoint(hit.transform.position);
            sees = PointInsideCone(localPlayer);
        }
        IsSeeingPlayer = sees;
        if (sees) OnPlayerDetected?.Invoke();
    }

    public void SetAlerted(bool alerted) =>
        _mr.material = alerted ? alertMaterial : normalMaterial;
}
