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
    [SerializeField, Range(0f, 1f)] float coneAlpha = 0.85f;

    const float CornerEpsilonDeg = 0.5f;

    MeshFilter _mf;
    MeshRenderer _mr;
    Mesh _mesh;
    static readonly List<float> _angleBuffer = new List<float>(256);
    static readonly Collider2D[] _wallBuffer = new Collider2D[16];

    public bool IsSeeingPlayer { get; private set; }
    public event System.Action OnPlayerDetected;

    void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();
        _mesh = new Mesh { name = "VisionConeMesh" };
        _mf.mesh = _mesh;
        _mr.material = normalMaterial;
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

        int wallCount = Physics2D.OverlapCircleNonAlloc(transform.position, distance, _wallBuffer, wallLayer);
        Vector2 origin = transform.position;
        float forwardWorldDeg = Mathf.Atan2(transform.up.x, transform.up.y) * Mathf.Rad2Deg;

        for (int w = 0; w < wallCount; w++)
        {
            var b = _wallBuffer[w].bounds;
            Vector2[] corners =
            {
                new Vector2(b.min.x, b.min.y),
                new Vector2(b.min.x, b.max.y),
                new Vector2(b.max.x, b.min.y),
                new Vector2(b.max.x, b.max.y)
            };
            for (int c = 0; c < 4; c++)
            {
                Vector2 toCorner = corners[c] - origin;
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
            Vector2 localDir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
            Vector2 worldDir = transform.TransformDirection(localDir);
            RaycastHit2D hit = Physics2D.Raycast(origin, worldDir, distance, wallLayer);
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
        Collider2D hit = Physics2D.OverlapCircle(transform.position, distance, playerLayer);
        if (hit != null)
        {
            Vector2 toPlayer = hit.transform.position - transform.position;
            float angleTo = Vector2.Angle(transform.up, toPlayer);
            if (angleTo < angle / 2f)
            {
                RaycastHit2D los = Physics2D.Raycast(transform.position, toPlayer.normalized, distance, wallLayer);
                if (!los) sees = true;
            }
        }
        IsSeeingPlayer = sees;
        if (sees) OnPlayerDetected?.Invoke();
    }

    public void SetAlerted(bool alerted) =>
        _mr.material = alerted ? alertMaterial : normalMaterial;
}
