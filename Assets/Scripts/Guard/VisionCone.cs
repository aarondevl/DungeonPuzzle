using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionCone : MonoBehaviour
{
    [SerializeField] public float angle = 60f;
    [SerializeField] public float distance = 5f;
    [SerializeField] int rayCount = 30;
    [SerializeField] LayerMask wallLayer;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] Material normalMaterial;
    [SerializeField] Material alertMaterial;

    MeshFilter _mf;
    MeshRenderer _mr;
    Mesh _mesh;

    public event System.Action OnPlayerDetected;

    void Awake()
    {
        _mf = GetComponent<MeshFilter>();
        _mr = GetComponent<MeshRenderer>();
        _mesh = new Mesh();
        _mf.mesh = _mesh;
        _mr.material = normalMaterial;
    }

    void Update()
    {
        BuildMesh();
        CheckDetection();
    }

    void BuildMesh()
    {
        float halfAngle = angle / 2f;
        float angleStep = angle / rayCount;

        Vector3[] vertices = new Vector3[rayCount + 2];
        int[] triangles = new int[rayCount * 3];

        vertices[0] = Vector3.zero;

        for (int i = 0; i <= rayCount; i++)
        {
            float currentAngle = -halfAngle + angleStep * i;
            // Local direction (no world rotation baked in)
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector2 localDir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
            // Convert to world dir for raycast
            Vector2 worldDir = transform.TransformDirection(localDir);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, worldDir, distance, wallLayer);
            // Store vertex in local space so the mesh transform applies correctly
            Vector3 point = hit ? transform.InverseTransformPoint(hit.point)
                                : (Vector3)(localDir * distance);
            vertices[i + 1] = point;
        }

        for (int i = 0; i < rayCount; i++)
        {
            triangles[i * 3 + 0] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
    }

    public bool IsSeeingPlayer { get; private set; }

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

    // Returns local-space direction; use transform.TransformDirection to get world-space
    Vector2 DirFromAngle(float angleDeg)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
    }
}
